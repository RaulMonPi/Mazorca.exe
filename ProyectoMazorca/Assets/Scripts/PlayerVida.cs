using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerVida : MonoBehaviour
{
    public int vida = 3;
    public float tiempoInvulnerable = 1.0f; // segundos de invulnerabilidad tras recibir daño
    private float tiempoUltimoDanio = -Mathf.Infinity;

    [Header("HUD de Vidas")]
    public GameObject[] corazones; // Vincula aquí los corazones del HUD en el inspector
    public GameObject gameOverImage; // Imagen de Game Over (debe estar desactivada al inicio)

    public float tiempoParaVolver = 2f; // segundos antes de volver al menú

    private bool estaMuerto = false;

    public void RecibirDanio(int cantidad)
    {
        if (estaMuerto) return;

        if (Time.time - tiempoUltimoDanio < tiempoInvulnerable)
            return; // Aún es invulnerable

        vida -= cantidad;
        tiempoUltimoDanio = Time.time;
        Debug.Log("Jugador ha recibido daño. Vida restante: " + vida);

        ActualizarHUD();

        if (vida <= 0)
        {
            StartCoroutine(GameOver());
        }
    }

    void ActualizarHUD()
    {
        if (corazones == null || corazones.Length == 0) return;

        for (int i = 0; i < corazones.Length; i++)
        {
            corazones[i].SetActive(i < vida);
        }
    }

    IEnumerator GameOver()
    {
        estaMuerto = true;

        Debug.Log("Jugador ha muerto. Mostrando pantalla de Game Over...");

        if (gameOverImage != null)
            gameOverImage.SetActive(true);

        // Pausar el movimiento del jugador si lo deseas (opcional)
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // Esperar 2 segundos en tiempo real
        yield return new WaitForSeconds(tiempoParaVolver);

        // Volver a la escena anterior (buildIndex - 1)
        int indexActual = SceneManager.GetActiveScene().buildIndex;
        int indexAnterior = indexActual - 1;

        if (indexAnterior >= 0)
        {
            SceneManager.LoadScene(indexAnterior);
        }
        else
        {
            Debug.LogWarning("No hay escena anterior en el Build Settings.");
        }
    }
}
