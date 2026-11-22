using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// NOVA DRONE - Progreso del jugador en Cloud Firestore via API REST (sin SDK).
/// Coleccion "jugadores" -> campos: monedas, nivel1Desbloqueado, nivel2Desbloqueado.
/// Se crea solo al iniciar el juego (no hace falta ponerlo en ninguna escena)
/// y sobrevive entre escenas. Tambien guarda una copia local (PlayerPrefs).
/// </summary>
public class FirebaseManager : MonoBehaviour
{
    // =================== CONFIGURA AQUI ===================
    static readonly string PROJECT_ID = "calavera-memorama";
    static readonly string API_KEY    = "TU_WEB_API_KEY";
    const string COLECCION = "jugadores";
    // ======================================================

    // ---------- Singleton con auto-creacion ----------
    static FirebaseManager _instance;
    public static FirebaseManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("FirebaseManager");
                _instance = go.AddComponent<FirebaseManager>();
            }
            return _instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        _ = Instance;
    }

    // ---------- Estado del jugador ----------
    public int monedas { get; private set; }
    public bool nivel1Desbloqueado { get; private set; } = true;
    public bool nivel2Desbloqueado { get; private set; }

    // ---------- Estado de conexion ----------
    public string estadoConexion { get; private set; } = "Firebase: conectando...";
    public bool conectado { get; private set; }

    public event Action OnDatosActualizados;

    string playerId;

    // =====================================================
    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        playerId = PlayerPrefs.GetString("nd_playerId", "");
        if (string.IsNullOrEmpty(playerId))
        {
            playerId = Guid.NewGuid().ToString("N");
            PlayerPrefs.SetString("nd_playerId", playerId);
        }

        CargarLocal();
    }

    void Start()
    {
        if (_instance != this) return;
        StartCoroutine(CargarProgreso());
    }

    // =================== API PUBLICA ===================
    public void AgregarMonedas(int cantidad)
    {
        monedas += cantidad;
        GuardarLocal();
        OnDatosActualizados?.Invoke();
    }

    public void DesbloquearNivel2()
    {
        nivel2Desbloqueado = true;
        GuardarLocal();
        OnDatosActualizados?.Invoke();
    }

    /// <summary>Guarda en Firestore. El callback se ejecuta al terminar (exito o error).</summary>
    public void GuardarProgreso(Action alTerminar = null)
    {
        StartCoroutine(GuardarRoutine(alTerminar));
    }

    public void ReiniciarProgreso()
    {
        monedas = 0;
        nivel1Desbloqueado = true;
        nivel2Desbloqueado = false;
        GuardarLocal();
        OnDatosActualizados?.Invoke();
        GuardarProgreso();
    }

    // =================== FIRESTORE ===================
    bool Configurado()
    {
        return PROJECT_ID != "TU_PROJECT_ID" && API_KEY != "TU_WEB_API_KEY"
               && !string.IsNullOrEmpty(PROJECT_ID) && !string.IsNullOrEmpty(API_KEY);
    }

    string DocUrl(string extra = "")
    {
        return "https://firestore.googleapis.com/v1/projects/" + PROJECT_ID +
               "/databases/(default)/documents/" + COLECCION + "/" + playerId +
               "?key=" + API_KEY + extra;
    }

    IEnumerator CargarProgreso()
    {
        if (!Configurado())
        {
            SetEstado("Firebase: no configurado (modo local)", false);
            yield break;
        }

        SetEstado("Firebase: conectando...", false);

        using (UnityWebRequest req = UnityWebRequest.Get(DocUrl()))
        {
            req.timeout = 8;
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                FsDoc doc = JsonUtility.FromJson<FsDoc>(req.downloadHandler.text);
                AplicarDoc(doc);
                SetEstado("Firebase: conectado", true);
            }
            else if (req.responseCode == 404)
            {
                // Servidor alcanzado, pero el jugador aun no existe -> se crea.
                SetEstado("Firebase: conectado (jugador nuevo)", true);
                yield return GuardarRoutine(null);
            }
            else
            {
                SetEstado("Firebase: sin conexion (modo local)", false);
                Debug.LogWarning("[Firebase] Error al cargar: " + req.responseCode + " " + req.error);
            }
        }
    }

    IEnumerator GuardarRoutine(Action alTerminar)
    {
        GuardarLocal();

        if (!Configurado())
        {
            if (alTerminar != null) alTerminar();
            yield break;
        }

        FsDoc doc = new FsDoc
        {
            fields = new FsFields
            {
                monedas = new FsInt { integerValue = monedas.ToString() },
                nivel1Desbloqueado = new FsBool { booleanValue = nivel1Desbloqueado },
                nivel2Desbloqueado = new FsBool { booleanValue = nivel2Desbloqueado }
            }
        };

        string json = JsonUtility.ToJson(doc);
        string mascara = "&updateMask.fieldPaths=monedas" +
                         "&updateMask.fieldPaths=nivel1Desbloqueado" +
                         "&updateMask.fieldPaths=nivel2Desbloqueado";

        // PATCH = crea el documento si no existe, o actualiza solo esos campos.
        using (UnityWebRequest req = new UnityWebRequest(DocUrl(mascara), "PATCH"))
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 5;

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                SetEstado("Firebase: conectado (guardado)", true);
            }
            else
            {
                SetEstado("Firebase: sin conexion (guardado local)", false);
                Debug.LogWarning("[Firebase] Error al guardar: " + req.responseCode + " " + req.error +
                                 "\n" + req.downloadHandler.text);
            }
        }

        if (alTerminar != null) alTerminar();
    }

    void AplicarDoc(FsDoc doc)
    {
        if (doc == null || doc.fields == null) return;

        int m;
        if (doc.fields.monedas != null && int.TryParse(doc.fields.monedas.integerValue, out m))
            monedas = m;
        if (doc.fields.nivel1Desbloqueado != null)
            nivel1Desbloqueado = doc.fields.nivel1Desbloqueado.booleanValue;
        if (doc.fields.nivel2Desbloqueado != null)
            nivel2Desbloqueado = doc.fields.nivel2Desbloqueado.booleanValue;

        GuardarLocal();
        OnDatosActualizados?.Invoke();
    }

    void SetEstado(string texto, bool ok)
    {
        estadoConexion = texto;
        conectado = ok;
        OnDatosActualizados?.Invoke();
    }

    // =================== COPIA LOCAL ===================
    void GuardarLocal()
    {
        PlayerPrefs.SetInt("nd_monedas", monedas);
        PlayerPrefs.SetInt("nd_n1", nivel1Desbloqueado ? 1 : 0);
        PlayerPrefs.SetInt("nd_n2", nivel2Desbloqueado ? 1 : 0);
        PlayerPrefs.Save();
    }

    void CargarLocal()
    {
        monedas = PlayerPrefs.GetInt("nd_monedas", 0);
        nivel1Desbloqueado = PlayerPrefs.GetInt("nd_n1", 1) == 1;
        nivel2Desbloqueado = PlayerPrefs.GetInt("nd_n2", 0) == 1;
    }
}

// ============ Clases para el JSON de Firestore ============
[Serializable] class FsDoc { public FsFields fields; }
[Serializable] class FsFields
{
    public FsInt monedas;
    public FsBool nivel1Desbloqueado;
    public FsBool nivel2Desbloqueado;
}
[Serializable] class FsInt { public string integerValue; }
[Serializable] class FsBool { public bool booleanValue; }
