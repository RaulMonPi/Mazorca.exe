using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class Meta : MonoBehaviour
{
    public GameObject gameWinImage;     // Imagen de Game Win (desactivada al inicio)
    private void OnCollisionEnter(Collision other)
    {

        Debug.Log("¡El player ha tocado el trigger!");
        StartCoroutine(GameOver());


    }
    IEnumerator GameOver()
    {
        Debug.Log("Nivel Completado. Mostrando pantalla de Game Win...");

        
        if (gameWinImage != null)
            gameWinImage.SetActive(true);

        // Esperar unos segundos antes de volver al menú principal
        yield return new WaitForSeconds(3f);

        // Volver al menú principal (asumiendo que el menú está en la escena 0)
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }
}
