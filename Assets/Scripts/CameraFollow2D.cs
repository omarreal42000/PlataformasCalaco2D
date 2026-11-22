using UnityEngine;

/// <summary>
/// NOVA DRONE - Camara 2D con seguimiento suavizado y limites del nivel.
/// Los limites (Min/Max X e Y) se dibujan en amarillo en la ventana Scene.
/// La camara se limita para que su VISTA nunca salga del rectangulo (no se ve el vacio).
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraFollow2D : MonoBehaviour
{
    [Header("Seguimiento")]
    public Transform target;
    public float smoothTime = 0.15f;
    public Vector2 offset = new Vector2(0f, 1f);

    [Header("Camara")]
    public float orthographicSize = 6f;

    [Header("Limites del nivel (rectangulo amarillo)")]
    public float minX = -10f;
    public float maxX = 60f;
    public float minY = -2f;
    public float maxY = 10f;

    Camera cam;
    Vector3 velocidad;

    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = orthographicSize;
    }

    void Start()
    {
        if (target == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) target = p.transform;
        }

        if (target != null)
            transform.position = Limitar(target.position + (Vector3)offset);   // arranque sin "salto"
    }

    void LateUpdate()
    {
        if (target == null) return;

        cam.orthographicSize = orthographicSize;   // permite ajustarlo en vivo

        Vector3 meta = Limitar(target.position + (Vector3)offset);
        transform.position = Vector3.SmoothDamp(transform.position, meta, ref velocidad, smoothTime);
    }

    Vector3 Limitar(Vector3 deseada)
    {
        float mitadAlto = cam.orthographicSize;
        float mitadAncho = mitadAlto * cam.aspect;

        float x = (maxX - minX) <= mitadAncho * 2f
            ? (minX + maxX) * 0.5f
            : Mathf.Clamp(deseada.x, minX + mitadAncho, maxX - mitadAncho);

        float y = (maxY - minY) <= mitadAlto * 2f
            ? (minY + maxY) * 0.5f
            : Mathf.Clamp(deseada.y, minY + mitadAlto, maxY - mitadAlto);

        return new Vector3(x, y, transform.position.z);
    }

    void OnValidate()
    {
        Camera c = GetComponent<Camera>();
        if (c != null) c.orthographicSize = orthographicSize;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Vector3 centro = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);
        Vector3 tamano = new Vector3(maxX - minX, maxY - minY, 0f);
        Gizmos.DrawWireCube(centro, tamano);
    }
}
