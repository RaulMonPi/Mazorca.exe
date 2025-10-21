using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuInicial : MonoBehaviour
{

    public void Jugar()
    {
        SceneManager.LoadScene("EscenaPauloFinal");
    }
    public void Salir(){
        Application.Quit();
    }
}
