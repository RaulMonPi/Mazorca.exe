using UnityEngine;

public class OjoManager : MonoBehaviour
{
    public enum OjoMode { FullRotation, PingPong }
    public OjoMode mode = OjoMode.FullRotation;

    public float rotationStep = 90f; // Grados por paso
    public float waitTime = 2f;      // Segundos en cada posición
    public float rotationSpeed = 180f; // Velocidad de rotación en grados/segundo

    [Header("Detección")]
    public float detectionRange = 5f;
    public float detectionAngle = 45f; // Ángulo del cono

    private float timer = 0f;
    private float targetAngle = 0f;
    private float currentAngle = 0f;
    private int direction = 1; // 1 o -1

    private void Start()
    {
        currentAngle = transform.eulerAngles.y;
        targetAngle = currentAngle;
    }

    private void Update()
    {
        // Rotación suave hacia el ángulo objetivo
        currentAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, rotationSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0, currentAngle, 0);

        // Cuando llega al ángulo objetivo, espera y calcula el siguiente
        if (Mathf.Approximately(Mathf.DeltaAngle(currentAngle, targetAngle), 0f))
        {
            timer += Time.deltaTime;
            if (timer >= waitTime)
            {
                timer = 0f;
                if (mode == OjoMode.FullRotation)
                {
                    targetAngle += rotationStep;
                    if (targetAngle >= 360f) targetAngle -= 360f;
                }
                else // PingPong
                {
                    targetAngle += rotationStep * direction;
                    direction *= -1;
                }
            }
        }
        else
        {
            timer = 0f; // Reset timer if still rotating
        }
    }

    // Dibuja el cono de visión en el editor (pintado entero de amarillo)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f); // Amarillo translúcido
        Vector3 origin = transform.position;
        Vector3 forward = Quaternion.Euler(0, Application.isPlaying ? currentAngle : transform.eulerAngles.y, 0) * Vector3.forward;

        int segments = 40;
        float halfAngle = detectionAngle / 2f;
        Vector3[] conePoints = new Vector3[segments + 2];
        conePoints[0] = origin;

        for (int i = 0; i <= segments; i++)
        {
            float angle = -halfAngle + detectionAngle * ((float)i / segments);
            Vector3 dir = Quaternion.Euler(0, angle, 0) * forward;
            conePoints[i + 1] = origin + dir.normalized * detectionRange;
        }

        // Rellenar el cono con triángulos
        for (int i = 1; i < conePoints.Length - 1; i++)
        {
            Gizmos.DrawLine(conePoints[0], conePoints[i]);
            Gizmos.DrawLine(conePoints[i], conePoints[i + 1]);
            Gizmos.DrawLine(conePoints[i + 1], conePoints[0]);
        }
    }

    // Detección de objetivo (ignora la altura)
    public bool IsInSight(Vector3 targetPosition)
    {
        Vector3 origin = transform.position;
        Vector3 toTarget = targetPosition - origin;
        toTarget.y = 0; // Ignora la altura

        if (toTarget.magnitude > detectionRange)
            return false;

        Vector3 forward = transform.forward;
        forward.y = 0;
        float angleToTarget = Vector3.Angle(forward, toTarget);

        return angleToTarget <= detectionAngle / 2f;
    }
}
