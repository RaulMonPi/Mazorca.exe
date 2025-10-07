using UnityEngine;

[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(LineRenderer))]
public class NoiseArea : MonoBehaviour
{
    [Header("Radios de detección")]
    public float walkRadius = 3f;   // Radio cuando camina
    public float runRadius = 6f;    // Radio cuando corre

    [Header("Visualización")]
    public int segments = 50;       // Cuántos puntos forman el círculo

    private CapsuleCollider col;
    private PlayerMove player;
    private LineRenderer line;

    void Start()
    {
        // Configurar el collider
        col = GetComponent<CapsuleCollider>();
        col.isTrigger = true;
        col.center = new Vector3(0, 1, 0);
        col.height = 2f;

        // Buscar script PlayerMove
        player = GetComponentInParent<PlayerMove>();
        if (player == null)
            Debug.LogError("NoiseArea no encontró el script PlayerMove en el padre.");

        LineConfigurator();
    }

    void LineConfigurator()
    {
        // Configurar LineRenderer (opcional para Game View)
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = segments + 1;
        line.startWidth = 0.05f;
        line.endWidth = 0.05f;
        line.material = new Material(Shader.Find("Sprites/Default"));
    }

    void Update()
    {
        if (player == null) return;

        // Cambia radio según el estado (andar/correr)
        col.radius = player.IsRunning() ? runRadius : walkRadius;

        // Actualiza color del LineRenderer para coincidir con el Gizmo
        Color color = player.IsRunning() ? Color.red : Color.green;
        line.startColor = color;
        line.endColor = color;

        // Actualiza visualización en Game View
        UpdateCircle(col.radius);
    }

    private void UpdateCircle(float radius)
    {
        float angle = 0f;
        for (int i = 0; i <= segments; i++)
        {
            float x = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;
            float z = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;
            line.SetPosition(i, new Vector3(x, 0, z) + col.center);
            angle += 360f / segments;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            Debug.Log("🔊 Un enemigo ha escuchado al jugador!");
        }
    }

    // 🔹 Gizmo SIEMPRE visible, cambia color dinámicamente
    private void OnDrawGizmos()
    {
        if (col == null)
            col = GetComponent<CapsuleCollider>();

        // Si hay referencia al jugador, usamos su estado para elegir color
        Color gizmoColor = Color.green; // Default (si no hay player)
        if (player != null)
            gizmoColor = player.IsRunning() ? Color.red : Color.green;

        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position + col.center, col.radius);
    }
}
