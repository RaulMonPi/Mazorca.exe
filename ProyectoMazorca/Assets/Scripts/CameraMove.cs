using UnityEngine;

public class CameraMove : MonoBehaviour
{
    public Transform target; // el Player
    public Vector3 offset;   // distancia desde el Player

    void LateUpdate()
    {
        if (target != null)
        {
            // La cámara sigue la posición del jugador pero NO rota con él
            transform.position = target.position + offset;
        }
    }
}
