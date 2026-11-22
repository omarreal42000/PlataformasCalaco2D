using UnityEngine;

/// <summary>
/// NOVA DRONE - Bala: linea recta a velocidad constante, se autodestruye a los 2 s,
/// ignora al jugador y a los triggers, y crea EfectoImpacto al chocar.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class BulletController : MonoBehaviour
{
    public float speed = 14f;
    public float lifeTime = 2f;
    public int damage = 1;
    public GameObject efectoImpactoPrefab;   // prefab "EfectoImpacto"

    Rigidbody2D rb;
    bool usada;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.drag = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    void Start()
    {
        Destroy(gameObject, lifeTime);                       // autodestruccion
        if (rb.velocity == Vector2.zero)                     // por si no se llamo Initialize
            rb.velocity = Vector2.right * speed;
    }

    /// <summary>Llamado por el jugador justo despues de instanciar la bala.</summary>
    public void Initialize(Vector2 direction, GameObject owner)
    {
        rb.velocity = direction.normalized * speed;

        // Ignorar colisiones con el jugador (y todos sus hijos).
        if (owner != null)
        {
            Collider2D[] mios = GetComponents<Collider2D>();
            Collider2D[] delOwner = owner.GetComponentsInChildren<Collider2D>();
            foreach (Collider2D a in mios)
                foreach (Collider2D b in delOwner)
                    Physics2D.IgnoreCollision(a, b);
        }
    }

    void OnCollisionEnter2D(Collision2D c)
    {
        if (usada) return;
        if (c.collider.isTrigger) return;                    // ignora triggers
        usada = true;

        Vector2 punto = c.contactCount > 0 ? c.GetContact(0).point : (Vector2)transform.position;

        if (efectoImpactoPrefab != null)
        {
            GameObject fx = Instantiate(efectoImpactoPrefab, punto, Quaternion.identity);
            Destroy(fx, 2f);
        }

        EnemyHealth enemigo = c.collider.GetComponentInParent<EnemyHealth>();
        if (enemigo != null) enemigo.TakeDamage(damage);

        Destroy(gameObject);
    }
}
