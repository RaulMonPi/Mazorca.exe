using UnityEngine;

[RequireComponent(typeof(CapsuleCollider))]
public class NoiseArea : MonoBehaviour
{
    [Header("Radios de detecci�n")]
    public float walkRadius = 3f;   // Radio cuando camina
    public float runRadius = 6f;    // Radio cuando corre

    private CapsuleCollider col;
    private PlayerMove player;

    void Start()
    {
        col = GetComponent<CapsuleCollider>();
        col.isTrigger = true;
        col.center = new Vector3(0, 1, 0); // Ajusta la altura al personaje
        col.height = 2f;

        player = GetComponentInParent<PlayerMove>();
        if (player == null)
            Debug.LogError("NoiseArea no encontro el script PlayerMovement en el padre.");
    }

    void Update()
    {
        if (player == null) return;

        // Cambia radio seg�n el estado (andar/correr)
        col.radius = player.IsRunning() ? runRadius : walkRadius;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            Debug.Log("Un enemigo ha escuchado al jugador!");
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Solo para debug visual en la escena
        Gizmos.color = Color.yellow;
        if (col != null)
            Gizmos.DrawWireSphere(transform.position + col.center, col.radius);
    }
}

