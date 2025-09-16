using UnityEngine;

public class MovmientoNPC : MonoBehaviour
{
    public Transform[] waypoints; // Asigna los waypoints desde el inspector
    public float speed = 2f;
    public float reachDistance = 0.2f;

    private int currentWaypoint = 0;
    private int direction = 1; // 1 para adelante, -1 para atrás

    void Update()
    {
        if (waypoints.Length == 0) return;

        Transform target = waypoints[currentWaypoint];
        Vector3 directionToTarget = (target.position - transform.position).normalized;
        transform.position += directionToTarget * speed * Time.deltaTime;

        if (Vector3.Distance(transform.position, target.position) < reachDistance)
        {
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
        }
    }
}