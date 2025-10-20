using UnityEngine;

public class PlayerVida : MonoBehaviour
{
    public int vida = 3;
    public float tiempoInvulnerable = 1.0f; // segundos de invulnerabilidad tras recibir daño

    private float tiempoUltimoDanio = -Mathf.Infinity;
    public Animator animator;

    //Efecto sangre
    public GameObject damageMarkPrefab;
    public float alturaSobreSuelo;

    private void Start()
    {
        
    }

    public void RecibirDanio(int cantidad)
    {
        if (Time.time - tiempoUltimoDanio < tiempoInvulnerable)
            return; // Aún es invulnerable

        vida -= cantidad;
        tiempoUltimoDanio = Time.time;
        Debug.Log("Jugador ha recibido daño. Vida restante: " + vida);

        // Activar la animación de daño
        if (animator != null)
        {
            animator.SetBool("damage", true);
            StartCoroutine(ResetDamageBool());
        }

        Vector3 markPos = new Vector3(transform.position.x, transform.position.y - 1f + alturaSobreSuelo, transform.position.z);
        Quaternion markRot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f); // rotación aleatoria en Y para variar
        Instantiate(damageMarkPrefab, markPos, markRot);

        if (vida <= 0)
        {
            // Lógica de muerte
        }
    }

    private System.Collections.IEnumerator ResetDamageBool()
    {
        yield return new WaitForSeconds(0.1f); // pequeño retraso para que el Animator detecte el cambio
        animator.SetBool("damage", false);
    }
}
