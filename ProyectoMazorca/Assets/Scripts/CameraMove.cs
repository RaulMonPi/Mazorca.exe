using UnityEngine;

public class CameraMove : MonoBehaviour
{
    public Transform target; // el Player
    public Vector3 offset;   // distancia desde el Player
    public float rotationX = 30f; // rotación en X
    public float rotationY = 0f;  // rotación en Y
    public float rotationZ = 0f;  // rotación en Z

    void LateUpdate()
    {
        if (target != null)
        {
            // La camara sigue la posicion del jugador pero NO rota con el
            transform.position = target.position + offset;
            transform.rotation = Quaternion.Euler(rotationX, rotationY, rotationZ);
        }
    }
}
