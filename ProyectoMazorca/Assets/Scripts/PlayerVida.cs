using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerVida : MonoBehaviour
{
    [Header("Vidas")]
    public int vida = 3;
    public float tiempoInvulnerable = 1.0f; // segundos de invulnerabilidad tras recibir daño
    private float tiempoUltimoDanio = -Mathf.Infinity;

    [Header("HUD")]
    public GameObject[] corazones;      // Imágenes o modelos de corazones
    public GameObject gameOverImage;    // Imagen de Game Over (desactivada al inicio)
    

    [Header("Configuración")]
    public float tiempoParaVolver = 2f; // Segundos antes de volver al menú

    private bool estaMuerto = false;
    private bool nivelCompletado = false;

    // === Lógica de recibir daño ===
    public void RecibirDanio(int cantidad)
    {
        if (estaMuerto || nivelCompletado) return;

        if (Time.time - tiempoUltimoDanio < tiempoInvulnerable)
            return; // Aún es invulnerable

        vida -= cantidad;
        tiempoUltimoDanio = Time.time;
        Debug.Log("Jugador ha recibido daño. Vida restante: " + vida);

        ActualizarHUD();

        if (vida <= 0)
            StartCoroutine(GameOver());
    }

    // === Actualizar corazones del HUD ===
    void ActualizarHUD()
    {
        if (corazones == null || corazones.Length == 0) return;

        for (int i = 0; i < corazones.Length; i++)
            corazones[i].SetActive(i < vida);
    }
  

    // === GAME OVER ===
    IEnumerator GameOver()
    {
        estaMuerto = true;
        Debug.Log("Jugador ha muerto. Mostrando pantalla de Game Over...");

        if (gameOverImage != null)
            gameOverImage.SetActive(true);

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = Vector3.zero;

        yield return new WaitForSeconds(tiempoParaVolver);

        int indexActual = SceneManager.GetActiveScene().buildIndex;
        int indexAnterior = indexActual - 1;

        if (indexAnterior >= 0)
            SceneManager.LoadScene(indexAnterior);
        else
            Debug.LogWarning("No hay escena anterior en el Build Settings.");
    }
}
