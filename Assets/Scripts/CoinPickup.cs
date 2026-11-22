using UnityEngine;

/// <summary>
/// NOVA DRONE - Nucleo de energia (Trigger). Suma monedas al tocarlo el jugador.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CoinPickup : MonoBehaviour
{
    public int valor = 1;
    public float velocidadGiro = 120f;
    public GameObject efectoPrefab;      // opcional

    bool recogido;

    void Reset()
    {
        // Al agregar el script, el collider se vuelve Trigger automaticamente.
        GetComponent<Collider2D>().isTrigger = true;
    }

    void Update()
    {
        transform.Rotate(0f, 0f, velocidadGiro * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (recogido || !other.CompareTag("Player")) return;
        recogido = true;

        FirebaseManager.Instance.AgregarMonedas(valor);

        if (efectoPrefab != null)
        {
            GameObject fx = Instantiate(efectoPrefab, transform.position, Quaternion.identity);
            Destroy(fx, 2f);
        }

        Destroy(gameObject);
    }
}
