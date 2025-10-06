using UnityEngine;

public class Enemigo : MonoBehaviour
{
    public int damage = 1;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerVida player = collision.gameObject.GetComponent<PlayerVida>();
            if (player != null)
            {
                //Debug.Log("Enemigo ha colisionado con el jugador, infligiendo daño.");
                player.RecibirDanio(damage);
            }
        }
    }
}