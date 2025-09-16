using UnityEngine;
using System.Collections;

public class MovmientoNPC : MonoBehaviour
{
    public Transform[] waypoints;
    public float speed = 1f;
    public float reachDistance = 0.2f;
    public float waitTime = 1f;
    public float lookAngle = 45f; // Grados a rotar a izquierda y derecha
    public float lookDuration = 0.5f; // Tiempo para cada rotación

    private int currentWaypoint = 0;
    private int direction = 1;
    private bool isWaiting = false;

  void Update()
{
    if (waypoints.Length == 0 || isWaiting) return;

    Transform target = waypoints[currentWaypoint];
    Vector3 directionToTarget = (target.position - transform.position).normalized;

    // Rotar suavemente hacia el siguiente waypoint
    if (directionToTarget != Vector3.zero)
    {
        Quaternion lookRotation = Quaternion.LookRotation(directionToTarget);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f); // 5f es la velocidad de giro
    }

    transform.position += directionToTarget * speed * Time.deltaTime;

    if (Vector3.Distance(transform.position, target.position) < reachDistance)
    {
        StartCoroutine(WaitAndLook());
    }
}
    IEnumerator WaitAndLook()
    {
        isWaiting = true;

        // Espera 1 segundo
        yield return new WaitForSeconds(waitTime);

        // Guarda la rotación original
        Quaternion originalRotation = transform.rotation;

        // Mira a la izquierda
        Quaternion leftRotation = originalRotation * Quaternion.Euler(0, -lookAngle, 0);
        yield return RotateTo(leftRotation, lookDuration);

        // Mira a la derecha
        Quaternion rightRotation = originalRotation * Quaternion.Euler(0, lookAngle, 0);
        yield return RotateTo(rightRotation, lookDuration * 2);

        // Vuelve a la rotación original
        yield return RotateTo(originalRotation, lookDuration);

        // Cambia al siguiente waypoint
        currentWaypoint += direction;
        if (currentWaypoint >= waypoints.Length)
        {
            direction = -1;
            currentWaypoint = waypoints.Length - 2;
        }
        else if (currentWaypoint < 0)
        {
            direction = 1;
            currentWaypoint = 1;
        }

        isWaiting = false;
    }

    IEnumerator RotateTo(Quaternion targetRotation, float duration)
    {
        Quaternion startRotation = transform.rotation;
        float time = 0f;
        while (time < duration)
        {
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        transform.rotation = targetRotation;
    }
}