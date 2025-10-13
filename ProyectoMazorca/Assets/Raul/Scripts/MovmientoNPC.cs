using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MovmientoNPC : MonoBehaviour
{
    public Transform[] waypoints;
    public float patrolSpeed = 1f;
    public float chaseSpeed = 2f;
    public float alertSpeed = 1.2f;
    public float reachDistance = 0.3f;
    public float waitTime = 1f;
    public float lookAngle = 45f;
    public float lookDuration = 0.5f;

    public Transform player;
    public float visionDistance = 10f;
    public float visionAngle = 60f;
    public LayerMask obstacleMask;

    public float chaseStopDistance = 1.5f;
    public float alertRotationTime = 2f;

    [Header("A* Pathfinding")]
    public float pathUpdateRate = 0.5f;

    private Pathfinding pathfinder;
    private Vector3[] currentPath;
    private int currentPathIndex;
    private bool isFollowingPath = false;
    private float nextPathUpdateTime;

    private int currentWaypoint = 0;
    private int direction = 1;
    private bool isWaiting = false;
    private bool playerInSight = false;

    private enum EstadoNPC { Patrolling, Alert, Chasing }
    private EstadoNPC estadoActual = EstadoNPC.Patrolling;

    private Vector3 alertPosition;
    private bool isAlertRotating = false;

    void Start()
    {
        pathfinder = GetComponent<Pathfinding>();
        if (pathfinder == null) pathfinder = gameObject.AddComponent<Pathfinding>();
        nextPathUpdateTime = Time.time;
    }

    void Update()
    {
        playerInSight = IsPlayerInSight();

        switch (estadoActual)
        {
            case EstadoNPC.Patrolling:
                if (playerInSight) CambiarEstado(EstadoNPC.Chasing);
                else PatrollingUpdate();
                break;

            case EstadoNPC.Alert:
                if (playerInSight) CambiarEstado(EstadoNPC.Chasing);
                else AlertUpdate();
                break;

            case EstadoNPC.Chasing:
                if (!playerInSight) CambiarEstado(EstadoNPC.Patrolling);
                else ChasingUpdate();
                break;
        }

        if (estadoActual == EstadoNPC.Chasing || estadoActual == EstadoNPC.Alert || estadoActual == EstadoNPC.Patrolling)
        {
            FollowPathUpdate();
        }
    }

    void CambiarEstado(EstadoNPC nuevoEstado)
    {
        if (estadoActual != nuevoEstado)
        {
            estadoActual = nuevoEstado;
            Debug.Log("NPC Estado: " + estadoActual);
            isFollowingPath = false;
            StopAllCoroutines();

            if (estadoActual == EstadoNPC.Alert)
                isAlertRotating = false;
        }
    }

    // ---------------- PATROL ----------------
    void PatrollingUpdate()
    {
        if (waypoints.Length == 0 || isWaiting) return;

        Transform target = waypoints[currentWaypoint];

        // 🔧 Ahora patrullamos usando pathfinding también
        if (!isFollowingPath && Time.time >= nextPathUpdateTime)
        {
            nextPathUpdateTime = Time.time + pathUpdateRate;
            pathfinder.StartFindPath(transform.position, target.position, OnPathFound);
        }

        if (!isFollowingPath)
        {
            Vector3 directionToTarget = (target.position - transform.position).normalized;
            transform.position += directionToTarget * patrolSpeed * Time.deltaTime;
        }

        if (Vector3.Distance(transform.position, target.position) < reachDistance)
        {
            StartCoroutine(WaitAndLook());
        }
    }

    // ---------------- CHASE ----------------
    void ChasingUpdate()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        if (dirToPlayer != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(dirToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }

        if (distanceToPlayer > chaseStopDistance && Time.time >= nextPathUpdateTime)
        {
            nextPathUpdateTime = Time.time + pathUpdateRate;
            pathfinder.StartFindPath(transform.position, player.position, OnPathFound);
        }
        else if (distanceToPlayer <= chaseStopDistance)
        {
            isFollowingPath = false;
        }
    }

    // ---------------- ALERT ----------------
    void AlertUpdate()
    {
        if (isAlertRotating) return;

        float distance = Vector3.Distance(transform.position, alertPosition);

        if (distance > reachDistance && Time.time >= nextPathUpdateTime)
        {
            nextPathUpdateTime = Time.time + pathUpdateRate;
            pathfinder.StartFindPath(transform.position, alertPosition, OnPathFound);
        }
        else if (distance <= reachDistance)
        {
            isFollowingPath = false;

            if (IsPlayerInSight())
            {
                CambiarEstado(EstadoNPC.Chasing);
            }
            else if (!isAlertRotating)
            {
                StartCoroutine(AlertRotateAndCheck());
            }
        }
    }

    public void OnPathFound(Vector3[] newPath, bool pathSuccessful)
    {
        if (pathSuccessful && newPath.Length > 0)
        {
            currentPath = newPath;
            currentPathIndex = 0;
            isFollowingPath = true;
        }
        else
        {
            isFollowingPath = false;
            currentPath = null;
        }
    }

    void FollowPathUpdate()
    {
        if (!isFollowingPath || currentPath == null) return;

        if (currentPathIndex < 0 || currentPathIndex >= currentPath.Length)
        {
            isFollowingPath = false;
            return;
        }

        Vector3 targetWaypoint = currentPath[currentPathIndex];
        targetWaypoint.y = transform.position.y;

        float speed = (estadoActual == EstadoNPC.Chasing) ? chaseSpeed :
                      (estadoActual == EstadoNPC.Alert) ? alertSpeed : patrolSpeed;

        Vector3 direction = (targetWaypoint - transform.position).normalized;

        // Siempre mirar en la dirección de movimiento (excepto si el vector es cero)
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }

        transform.position += direction * speed * Time.deltaTime;

        if (Vector3.Distance(transform.position, targetWaypoint) < reachDistance)
        {
            currentPathIndex++;
            if (currentPathIndex >= currentPath.Length)
                isFollowingPath = false;
        }
    }

    // ---------------- ALERT ROTATION ----------------
    IEnumerator AlertRotateAndCheck()
    {
        isAlertRotating = true;
        float elapsed = 0f;

        while (elapsed < alertRotationTime)
        {
            float rotationStep = 360f * (Time.deltaTime / alertRotationTime);
            transform.Rotate(0, rotationStep, 0);
            elapsed += Time.deltaTime;

            if (IsPlayerInSight())
            {
                CambiarEstado(EstadoNPC.Chasing);
                yield break;
            }

            yield return null;
        }

        // ✅ Índices protegidos: evita el IndexOutOfRange
        if (waypoints.Length > 1)
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

            currentWaypoint = Mathf.Clamp(currentWaypoint, 0, waypoints.Length - 1);
        }

        CambiarEstado(EstadoNPC.Patrolling);
        isAlertRotating = false;
    }

    // ---------------- WAIT AND LOOK ----------------
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

        // ✅ Protección ante índices fuera de rango
        if (waypoints.Length > 1)
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

            currentWaypoint = Mathf.Clamp(currentWaypoint, 0, waypoints.Length - 1);
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

    bool IsPlayerInSight()
    {
        if (player == null) return false;

        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        float angle = Vector3.Angle(transform.forward, dirToPlayer);
        if (angle < visionAngle * 0.5f && distanceToPlayer < visionDistance)
        {
            if (!Physics.Raycast(transform.position + Vector3.up * 0.5f, dirToPlayer, distanceToPlayer, obstacleMask))
                return true;
        }
        return false;
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Sound"))
        {
            alertPosition = other.transform.position;
            if (estadoActual != EstadoNPC.Chasing)
                CambiarEstado(EstadoNPC.Alert);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (currentPath != null)
        {
            Gizmos.color = Color.cyan;
            for (int i = currentPathIndex; i < currentPath.Length; i++)
            {
                Gizmos.DrawSphere(currentPath[i] + Vector3.up * 0.1f, 0.2f);
                if (i > currentPathIndex)
                    Gizmos.DrawLine(currentPath[i - 1], currentPath[i]);
            }
        }
    }
}
