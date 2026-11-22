using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// NOVA DRONE - Portal de salto (Trigger). Desbloquea el siguiente nivel,
/// guarda el progreso en Firebase y cambia de escena.
/// Nivel 1: nivel = 1, escenaSiguiente = "Nivel2"
/// Nivel 2: nivel = 2, escenaSiguiente = "Resultados"
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LevelGoal : MonoBehaviour
{
    [Tooltip("Numero del nivel en el que esta este portal (1 o 2).")]
    public int nivel = 1;
    public string escenaSiguiente = "Nivel2";

    bool activado;

    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (activado || !other.CompareTag("Player")) return;
        activado = true;

        FirebaseManager fb = FirebaseManager.Instance;

        if (nivel == 1) fb.DesbloquearNivel2();

        // Guarda en Firestore y, al terminar (con exito o error), cambia de escena.
        fb.GuardarProgreso(() => SceneManager.LoadScene(escenaSiguiente));
    }
}
