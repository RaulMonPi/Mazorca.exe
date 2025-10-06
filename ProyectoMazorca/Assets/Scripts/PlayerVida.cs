using UnityEngine;

public class PlayerVida : MonoBehaviour
{
    public int vida = 3;
    public float tiempoInvulnerable = 1.0f; // segundos de invulnerabilidad tras recibir daño

    private float tiempoUltimoDanio = -Mathf.Infinity;

    public void RecibirDanio(int cantidad)
    {
        if (Time.time - tiempoUltimoDanio < tiempoInvulnerable)
            return; // Aún es invulnerable

        vida -= cantidad;
        tiempoUltimoDanio = Time.time;
        Debug.Log("Jugador ha recibido daño. Vida restante: " + vida);

        if (vida <= 0)
        {
            // Lógica de muerte
        }
    }
}
