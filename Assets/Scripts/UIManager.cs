using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// NOVA DRONE - UI para MenuPrincipal, HUD de niveles y Resultados.
/// Deja vacios los campos que no uses en cada escena.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("Textos (TextMeshPro)")]
    public TMP_Text textoMonedas;
    public TMP_Text textoEstadoFirebase;
    public TMP_Text textoNivel2Bloqueado;     // opcional: "BLOQUEADO"

    [Header("Botones")]
    public Button botonNivel2;

    [Header("Nombres de escena")]
    public string escenaMenu = "MenuPrincipal";
    public string escenaNivel1 = "Nivel1";
    public string escenaNivel2 = "Nivel2";

    FirebaseManager fb;

    void Start()
    {
        fb = FirebaseManager.Instance;
        fb.OnDatosActualizados += Refrescar;
        Refrescar();
    }

    void OnDestroy()
    {
        if (fb != null) fb.OnDatosActualizados -= Refrescar;
    }

    void Refrescar()
    {
        if (fb == null) return;

        if (textoMonedas != null)
            textoMonedas.text = "Monedas: " + fb.monedas;

        if (textoEstadoFirebase != null)
        {
            string color = fb.conectado ? "#4CFF4C" : "#FF5555";
            textoEstadoFirebase.text = "<color=" + color + ">" + fb.estadoConexion + "</color>";
        }

        if (botonNivel2 != null)
            botonNivel2.interactable = fb.nivel2Desbloqueado;

        if (textoNivel2Bloqueado != null)
            textoNivel2Bloqueado.gameObject.SetActive(!fb.nivel2Desbloqueado);
    }

    // ============ Botones (OnClick) ============
    public void Jugar() { SceneManager.LoadScene(escenaNivel1); }

    public void IrNivel1() { SceneManager.LoadScene(escenaNivel1); }

    public void IrNivel2()
    {
        if (fb != null && fb.nivel2Desbloqueado)
            SceneManager.LoadScene(escenaNivel2);
    }

    public void ReiniciarProgreso()
    {
        if (fb != null) fb.ReiniciarProgreso();
    }

    public void VolverMenu() { SceneManager.LoadScene(escenaMenu); }

    public void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
