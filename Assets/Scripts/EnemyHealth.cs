using System.Collections;
using UnityEngine;

/// <summary>
/// NOVA DRONE - Salud del enemigo (asteroide / torreta). 3 disparos = destruido.
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    public int maxHealth = 3;
    [Tooltip("Si esta activo, tocar al enemigo reinicia el nivel.")]
    public bool mataAlJugadorAlTocar = true;
    public GameObject efectoMuertePrefab;    // opcional: puedes reusar EfectoImpacto

    int health;
    SpriteRenderer sr;
    Color colorBase;

    void Awake()
    {
        health = maxHealth;
        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) colorBase = sr.color;
    }

    public void TakeDamage(int damage)
    {
        health -= damage;

        if (health <= 0)
        {
            Die();
            return;
        }

        if (sr != null)
        {
            StopAllCoroutines();
            StartCoroutine(Destello());
        }
    }

    IEnumerator Destello()
    {
        sr.color = Color.white;
        yield return new WaitForSeconds(0.08f);
        sr.color = colorBase;
    }

    void Die()
    {
        if (efectoMuertePrefab != null)
        {
            GameObject fx = Instantiate(efectoMuertePrefab, transform.position, Quaternion.identity);
            Destroy(fx, 2f);
        }
        Destroy(gameObject);
    }

    void OnCollisionEnter2D(Collision2D c)
    {
        if (!mataAlJugadorAlTocar) return;
        if (!c.gameObject.CompareTag("Player")) return;

        PlayerController p = c.gameObject.GetComponent<PlayerController>();
        if (p != null) p.Morir();
    }
}
