using UnityEngine;
using System.Collections;

public class MovmientoNPC : MonoBehaviour
{
    public Transform[] waypoints;
    public float speed = 1f;
    public float reachDistance = 0.2f;
    public float waitTime = 1f;
    public float lookAngle = 45f;
    public float lookDuration = 0.5f;

    public Transform player; // Asigna el jugador en el inspector
    public float visionDistance = 10f;
    public float visionAngle = 60f;
    public LayerMask obstacleMask; // Asigna las capas de obstáculos

    private int currentWaypoint = 0;
    private int direction = 1;
    private bool isWaiting = false;
    private bool playerInSight = false;

    void Update()
    {
        // Verifica si el jugador está en el campo de visión y sin obstáculos
        playerInSight = IsPlayerInSight();

        if (playerInSight)
        {
            // Mira al jugador y no se mueve
            Vector3 dirToPlayer = (player.position - transform.position).normalized;
            if (dirToPlayer != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(dirToPlayer);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
            }
            return;
        }

        if (waypoints.Length == 0 || isWaiting) return;

        Transform target = waypoints[currentWaypoint];
        Vector3 directionToTarget = (target.position - transform.position).normalized;

        // Rotar suavemente hacia el siguiente waypoint
        if (directionToTarget != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(directionToTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }

        transform.position += directionToTarget * speed * Time.deltaTime;

        if (Vector3.Distance(transform.position, target.position) < reachDistance)
        {
            StartCoroutine(WaitAndLook());
        }
    }

    bool IsPlayerInSight()
    {
        if (player == null) return false;

        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Comprueba ángulo de visión
        float angle = Vector3.Angle(transform.forward, dirToPlayer);
        if (angle < visionAngle * 0.5f && distanceToPlayer < visionDistance)
        {
            // Raycast para comprobar obstáculos
            if (!Physics.Raycast(transform.position + Vector3.up * 0.5f, dirToPlayer, distanceToPlayer, obstacleMask))
            {
                return true;
            }
        }
        return false;
    }

    IEnumerator WaitAndLook()
    {
        isWaiting = true;
        yield return new WaitForSeconds(waitTime);

        Quaternion originalRotation = transform.rotation;
        Quaternion leftRotation = originalRotation * Quaternion.Euler(0, -lookAngle, 0);
        yield return RotateTo(leftRotation, lookDuration);

        Quaternion rightRotation = originalRotation * Quaternion.Euler(0, lookAngle, 0);
        yield return RotateTo(rightRotation, lookDuration * 2);

        yield return RotateTo(originalRotation, lookDuration);

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