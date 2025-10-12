using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MovmientoNPC : MonoBehaviour
{
    public Transform[] waypoints;
    public float patrolSpeed = 1f;      
    public float chaseSpeed = 2f;       
    public float alertSpeed = 1.2f;     
    public float reachDistance = 0.2f; // <-- RECOMENDACIÓN: Prueba con 0.3f o 0.4f si se sigue atascando en paredes.
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

        if (estadoActual == EstadoNPC.Chasing || estadoActual == EstadoNPC.Alert)
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

            if (estadoActual == EstadoNPC.Alert)
            {
                isAlertRotating = false;
                StopAllCoroutines(); 
            }
        }
    }

    void PatrollingUpdate()
    {
        // <-- CORRECCIÓN: Verifica si hay waypoints antes de intentar acceder a ellos.
        if (waypoints.Length == 0) return; 
        if (isWaiting) return;

        Transform target = waypoints[currentWaypoint];
        Vector3 directionToTarget = (target.position - transform.position).normalized;

        if (directionToTarget != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(directionToTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }

        transform.position += directionToTarget * patrolSpeed * Time.deltaTime;

        if (Vector3.Distance(transform.position, target.position) < reachDistance)
        {
            StartCoroutine(WaitAndLook());
        }
    }

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
        if (pathSuccessful)
        {
            currentPath = newPath;
            currentPathIndex = 0;
            isFollowingPath = true;
        }
        else
        {
            // <-- CORRECCIÓN: Si el path falla, aseguramos que la ruta se anule.
            isFollowingPath = false;
            currentPath = null;
        }
    }

    void FollowPathUpdate()
    {
        if (!isFollowingPath || currentPath == null) return;
        
        // <-- CORRECCIÓN: Salir si el índice ya está fuera de rango para prevenir el error.
        if (currentPathIndex >= currentPath.Length)
        {
            isFollowingPath = false;
            return; 
        }

        Vector3 targetWaypoint = currentPath[currentPathIndex];
        targetWaypoint.y = transform.position.y;

        Vector3 directionToTarget = (targetWaypoint - transform.position).normalized;
        float speed = (estadoActual == EstadoNPC.Chasing) ? chaseSpeed : alertSpeed;

        if (estadoActual != EstadoNPC.Chasing && directionToTarget != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(directionToTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }

        transform.position += directionToTarget * speed * Time.deltaTime;

        if (Vector3.Distance(transform.position, targetWaypoint) < reachDistance)
        {
            currentPathIndex++;
            // Verificamos de nuevo si terminamos la ruta
            if (currentPathIndex >= currentPath.Length)
            {
                isFollowingPath = false;
            }
        }
    }

    // Método que gira al NPC al llegar a la posición de alerta
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

        // <-- CORRECCIÓN: Lógica de límites robusta para evitar IndexOutOfRangeException
        if (waypoints.Length > 0)
        {
            currentWaypoint += direction;
            if (currentWaypoint >= waypoints.Length)
            {
                direction = -1;
                currentWaypoint = waypoints.Length - 2;
                if (currentWaypoint < 0) currentWaypoint = 0; // Seguridad
            }
            else if (currentWaypoint < 0)
            {
                direction = 1;
                currentWaypoint = 1;
                if (currentWaypoint >= waypoints.Length) currentWaypoint = 0; // Seguridad
            }
        }
        else
        {
            currentWaypoint = 0; // Si no hay waypoints, nos quedamos en 0
        }

        CambiarEstado(EstadoNPC.Patrolling);
        isAlertRotating = false;
    }


    private void OnDrawGizmosSelected()
    {
        if (currentPath != null)
        {
            Gizmos.color = Color.blue;
            for (int i = currentPathIndex; i < currentPath.Length; i++)
            {
                Gizmos.DrawSphere(currentPath[i] + Vector3.up * 0.1f, 0.2f);
                if (i > currentPathIndex)
                {
                    Gizmos.DrawLine(currentPath[i - 1], currentPath[i]);
                }
            }
        }
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

        // <-- CORRECCIÓN: Lógica de límites robusta para Patrolling
        if (waypoints.Length > 0)
        {
            currentWaypoint += direction;
            if (currentWaypoint >= waypoints.Length)
            {
                direction = -1;
                currentWaypoint = waypoints.Length - 2;
                if (currentWaypoint < 0) currentWaypoint = 0; // Seguridad
            }
            else if (currentWaypoint < 0)
            {
                direction = 1;
                currentWaypoint = 1;
                if (currentWaypoint >= waypoints.Length) currentWaypoint = 0; // Seguridad
            }
        }
        else
        {
            currentWaypoint = 0;
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

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Sound"))
        {
            alertPosition = other.transform.position;
            if (estadoActual != EstadoNPC.Chasing) 
            {
                CambiarEstado(EstadoNPC.Alert);
            }
        }
    }
}