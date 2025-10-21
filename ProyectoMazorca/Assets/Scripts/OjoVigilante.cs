using UnityEngine;
using System.Collections;

public class OjoVigilante : MonoBehaviour
{
    public enum RotationMode {
        Degrees360,
        Degrees90
    }

    [Header("Configuración de Rotación")]
    public RotationMode rotationMode = RotationMode.Degrees360; // Modo de rotación (360 o 90)
    public float rotationSpeed = 30f; // Velocidad de rotación en grados por segundo
    public float pauseDuration = 2f; // Duración de la pausa cada 90 grados
    
    // Variables de control de rotación
    private float targetRotationY;
    private float rotationAccumulator = 0f; // Acumulador para saber cuándo giró 90 grados
    private bool isPaused = false;
    private float initialYRotation;

    [Header("Configuración de Visión")]
    public Transform player; // Referencia al Transform del jugador
    public ConoVision visionCone; // Referencia al script ConoVision
    
    // Parámetros de visión duplicados para la detección
    public float visionDistance = 5f;
    public float visionAngle = 60f;
    public LayerMask obstacleMask; // Máscara de capas para paredes/obstáculos
    
    private bool playerDetected = false;

    void Start()
    {
        // ... (Configuración de ConoVision - Mantenemos la misma lógica)
        if (visionCone == null)
        {
            visionCone = GetComponentInChildren<ConoVision>();
            // ... (comprobación de error)
        }
        
        if (visionCone != null)
        {
            visionCone.visionDistance = visionDistance;
            visionCone.visionAngle = visionAngle;
        }

        // Inicializar el punto de partida y el acumulador
        initialYRotation = transform.eulerAngles.y;
        targetRotationY = initialYRotation;

        // Iniciar el coroutine de rotación y pausa
        StartCoroutine(RotateAndPauseCoroutine());
    }

    void Update()
    {
        CheckForPlayer();

        // **AQUÍ SE EJECUTARÍA EL COMPORTAMIENTO AL DETECTAR AL JUGADOR**
        if (playerDetected)
        {
            // Debug.Log("¡Jugador detectado por el Ojo!");
            // Detén el coroutine si quieres detener la rotación al detectar:
            // StopAllCoroutines(); 
        }
    }

    // --- Coroutine de Rotación y Pausa ---
    IEnumerator RotateAndPauseCoroutine()
    {
        while (true) // Bucle infinito para rotar y pausar continuamente
        {
            float degreesToRotate;
            
            if (rotationMode == RotationMode.Degrees360)
            {
                // En modo 360, siempre giramos 90 grados y pausamos
                degreesToRotate = 90f;
            }
            else // RotationMode.Degrees90
            {
                // En modo 90, giramos 90 grados y pausamos, alternando dirección
                float currentRotation = transform.eulerAngles.y;
                // Calculamos hacia qué target de 90 grados desde el inicio debemos ir
                float angleFromStart = currentRotation - initialYRotation;
                angleFromStart = Mathf.Repeat(angleFromStart + 180f, 360f) - 180f; // Normalizar a -180/180

                // Si estamos en el lado derecho o en el centro, ir al izquierdo (-45)
                if (angleFromStart >= 0)
                {
                    targetRotationY = initialYRotation - 45f;
                }
                else // Si estamos en el lado izquierdo o en el centro, ir al derecho (+45)
                {
                    targetRotationY = initialYRotation + 45f;
                }
                
                // La cantidad de grados a rotar es la distancia más corta hacia ese nuevo target
                // OJO: Mathf.DeltaAngle calcula la diferencia angular más corta entre dos ángulos
                degreesToRotate = Mathf.DeltaAngle(currentRotation, targetRotationY);
            }
            
            // Si la rotación es cero (estamos justo en el target), simplemente pausamos
            if (Mathf.Abs(degreesToRotate) < 0.1f && rotationMode == RotationMode.Degrees90)
            {
                // Ya estamos en el límite de 90. Simplemente hacemos la pausa y la próxima rotación se calculará para ir al otro lado.
            }
            else
            {
                 // Gira el ángulo necesario (hasta 90 grados)
                float rotatedAngle = 0f;
                Quaternion startRot = transform.rotation;
                
                // Calcula la rotación final
                Quaternion endRot = startRot * Quaternion.Euler(0, degreesToRotate, 0);

                float timeToRotate = Mathf.Abs(degreesToRotate) / rotationSpeed;
                float elapsedTime = 0f;

                while (elapsedTime < timeToRotate)
                {
                    transform.rotation = Quaternion.Slerp(startRot, endRot, elapsedTime / timeToRotate);
                    elapsedTime += Time.deltaTime;

                    // IMPORTANTE: Chequear detección durante la rotación
                    CheckForPlayer();
                    if (playerDetected)
                    {
                        // Si detecta, puede pausar la rotación o romper el loop.
                        // Por simplicidad, aquí lo dejamos seguir, pero puedes añadir lógica de detención.
                    }

                    yield return null;
                }
                // Asegurar que la rotación es exacta
                transform.rotation = endRot;
            }

            // Pausa de 2 segundos al completar el giro (o al alcanzar el límite de 90 en modo 90)
            isPaused = true;
            yield return new WaitForSeconds(pauseDuration);
            isPaused = false;
        }
    }

    // --- Detección del Jugador ---
    void CheckForPlayer()
    {
        playerDetected = IsPlayerInSight();
    }

    // Función de detección adaptada del script del Slime
    bool IsPlayerInSight()
    {
        if (player == null) return false;

        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // 1. Está el jugador dentro del ángulo y distancia de visión?
        float angle = Vector3.Angle(transform.forward, dirToPlayer);
        if (angle < visionAngle * 0.5f && distanceToPlayer < visionDistance)
        {
            // 2. Hay un obstáculo entre el Ojo y el jugador?
            if (!Physics.Raycast(transform.position + Vector3.up * 0.5f, dirToPlayer, distanceToPlayer, obstacleMask))
            {
                return true; // Jugador detectado!
            }
        }
        return false;
    }
    
    // Opcional: Para dibujar Gizmos.
    // ... (Mantén la función OnDrawGizmosSelected del script anterior si la necesitas)
}