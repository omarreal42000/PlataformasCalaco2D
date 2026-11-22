using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// NOVA DRONE - Movimiento, salto unico, volteo, disparo y animacion del dron.
/// Input: SOLO Input Manager clasico.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 6f;
    public float jumpForce = 13f;
    [Tooltip("Si el jugador cae por debajo de esta Y, se reinicia el nivel.")]
    public float killY = -15f;

    [Header("Deteccion de suelo")]
    public Transform groundCheck;          // hijo "GroundCheck"
    public float groundRadius = 0.2f;
    public LayerMask groundLayer;          // capa "Ground" (Layer 8)

    [Header("Disparo")]
    public Transform puntoDisparo;         // hijo "PuntoDisparo"
    public GameObject bulletPrefab;
    public float fireRate = 0.35f;

    [Header("Animacion")]
    public Animator animator;              // Animator del hijo "Visual"

    Rigidbody2D rb;
    Collider2D col;
    float h;
    float touchMove;
    bool isGrounded;
    bool facingRight = true;
    bool touchShoot;
    bool muerto;
    float nextFireTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (rb.gravityScale <= 0f) rb.gravityScale = 3f;   // gravedad activa
        rb.freezeRotation = true;

        // Respaldo: si no asignaste el material fisico, se crea uno sin friccion.
        if (col != null && col.sharedMaterial == null)
        {
            PhysicsMaterial2D mat = new PhysicsMaterial2D("SinFriccion");
            mat.friction = 0f;
            mat.bounciness = 0f;
            col.sharedMaterial = mat;
        }
    }

    void Update()
    {
        // ---- Movimiento horizontal (A/D o flechas) ----
        float teclado = Input.GetAxisRaw("Horizontal");
        h = Mathf.Abs(teclado) > 0.01f ? teclado : touchMove;

        // ---- Suelo ----
        isGrounded = groundCheck != null &&
                     Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);

        // ---- Salto unico ----
        if (Input.GetKeyDown(KeyCode.Space)) TryJump();

        // ---- Volteo ----
        if (h > 0.01f && !facingRight) Flip();
        else if (h < -0.01f && facingRight) Flip();

        // ---- Disparo (J, Z o clic izquierdo) ----
        bool quiereDisparar = Input.GetKey(KeyCode.J) || Input.GetKey(KeyCode.Z) || touchShoot;
#if UNITY_STANDALONE || UNITY_EDITOR
        quiereDisparar |= Input.GetMouseButton(0);
#endif
        if (quiereDisparar) TryShoot();

        // ---- Animator ----
        if (animator != null)
            animator.SetBool("isWalking", Mathf.Abs(h) > 0.01f);

        // ---- Caida al vacio ----
        if (transform.position.y < killY) Morir();
    }

    void FixedUpdate()
    {
        rb.velocity = new Vector2(h * moveSpeed, rb.velocity.y);
    }

    void TryJump()
    {
        // Solo si esta en el suelo Y no esta subiendo -> evita doble salto.
        if (isGrounded && rb.velocity.y <= 0.1f)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            isGrounded = false;
        }
    }

    void TryShoot()
    {
        if (Time.time < nextFireTime) return;
        if (bulletPrefab == null || puntoDisparo == null) return;

        nextFireTime = Time.time + fireRate;

        GameObject go = Instantiate(bulletPrefab, puntoDisparo.position, Quaternion.identity);
        BulletController bala = go.GetComponent<BulletController>();
        if (bala != null)
            bala.Initialize(facingRight ? Vector2.right : Vector2.left, gameObject);

        if (animator != null) animator.SetTrigger("shootTrigger");
    }

    void Flip()
    {
        facingRight = !facingRight;
        Vector3 s = transform.localScale;
        s.x = Mathf.Abs(s.x) * (facingRight ? 1f : -1f);
        transform.localScale = s;              // tambien gira PuntoDisparo (es hijo)
    }

    public void Morir()
    {
        if (muerto) return;
        muerto = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ===== API para botones tactiles (Android) =====
    public void SetMoveInput(float v) { touchMove = Mathf.Clamp(v, -1f, 1f); }
    public void JumpButton() { TryJump(); }
    public void ShootDown() { touchShoot = true; }
    public void ShootUp() { touchShoot = false; }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
    }
}
