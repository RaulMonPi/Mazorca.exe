using UnityEngine;

public class PlayerVida : MonoBehaviour
{
    public int vida = 3;

    public void RecibirDanio(int cantidad)
    {
        vida -= cantidad;
        Debug.Log("Jugador ha recibido daño. Vida restante: " + vida);
        if (vida <= 0)
        {
            // Lógica de muerte
        }
    }
}
