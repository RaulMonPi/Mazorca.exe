using UnityEngine;
using System.Collections;

public class MovmientoNPC : MonoBehaviour
{
    public Transform[] waypoints;
    public float patrolSpeed = 1f;      // Velocidad al patrullar
    public float chaseSpeed = 2f;       // Velocidad al perseguir
    public float alertSpeed = 1.2f;     // Velocidad al ir al sonido
    public float reachDistance = 0.2f;
    public float waitTime = 1f;
    public float lookAngle = 45f;
    public float lookDuration = 0.5f;

    public Transform player; // Asigna el jugador en el inspector
    public float visionDistance = 10f;
    public float visionAngle = 60f;
    public LayerMask obstacleMask; // Asigna las capas de obstáculos

    public float chaseStopDistance = 1.5f; // Distancia mínima para detenerse cerca del jugador
    public float alertRotationTime = 2f;   // Tiempo girando en alerta

    private int currentWaypoint = 0;
    private int direction = 1;
    private bool isWaiting = false;
    private bool playerInSight = false;

    private enum EstadoNPC { Patrolling, Alert, Chasing }
    private EstadoNPC estadoActual = EstadoNPC.Patrolling;

    private Vector3 alertPosition;
    private bool isAlertRotating = false;

    void Update()
    {
        playerInSight = IsPlayerInSight();

        switch (estadoActual)
        {
            case EstadoNPC.Patrolling:
                if (playerInSight)
                {
                    CambiarEstado(EstadoNPC.Chasing);
                }
                else
                {
                    PatrollingUpdate();
                }
                break;
            case EstadoNPC.Alert:
                AlertUpdate();
                break;
            case EstadoNPC.Chasing:
                if (!playerInSight)
                {
                    CambiarEstado(EstadoNPC.Patrolling);
                }
                else
                {
                    ChasingUpdate();
                }
                break;
        }
    }

    void CambiarEstado(EstadoNPC nuevoEstado)
    {
        if (estadoActual != nuevoEstado)
        {
            estadoActual = nuevoEstado;
            Debug.Log("NPC Estado: " + estadoActual);

            if (estadoActual == EstadoNPC.Alert)
            {
                isAlertRotating = false;
                StopAllCoroutines();
            }
        }
    }

    void PatrollingUpdate()
    {
        if (waypoints.Length == 0 || isWaiting) return;

        Transform target = waypoints[currentWaypoint];
        Vector3 directionToTarget = (target.position - transform.position).normalized;

        // Rotar suavemente hacia el siguiente waypoint
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
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Rotar hacia el jugador
        if (dirToPlayer != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(dirToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }

        // Si está lejos, avanza hacia el jugador
        if (distanceToPlayer > chaseStopDistance)
        {
            transform.position += dirToPlayer * chaseSpeed * Time.deltaTime;
        }
        // Si está cerca, se queda quieto mirando al jugador
    }

    void AlertUpdate()
    {
        if (!isAlertRotating)
        {
            // Ir a la posición del sonido
            Vector3 dirToAlert = (alertPosition - transform.position);
            dirToAlert.y = 0;
            float distance = dirToAlert.magnitude;

            if (distance > reachDistance)
            {
                Vector3 moveDir = dirToAlert.normalized;
                Quaternion lookRotation = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
                transform.position += moveDir * alertSpeed * Time.deltaTime;
            }
            else
            {
                // Si ve al jugador al llegar, cambia a persecución directamente
                if (IsPlayerInSight())
                {
                    CambiarEstado(EstadoNPC.Chasing);
                }
                else
                {
                    // Si no ve al jugador, empieza a girar
                    StartCoroutine(AlertRotateAndCheck());
                }
            }
        }
        // Si está girando, la corrutina se encarga de la lógica
    }

    IEnumerator AlertRotateAndCheck()
    {
        isAlertRotating = true;
        float elapsed = 0f;
        float startY = transform.eulerAngles.y;
        float totalRotation = 0f;

        while (elapsed < alertRotationTime)
        {
            // Girar sobre sí mismo
            float rotationStep = 360f * (Time.deltaTime / alertRotationTime);
            transform.Rotate(0, rotationStep, 0);
            totalRotation += rotationStep;
            elapsed += Time.deltaTime;

            // Durante la rotación, si ve al jugador, cambia a persecución
            if (IsPlayerInSight())
            {
                CambiarEstado(EstadoNPC.Chasing);
                yield break;
            }

            yield return null;
        }

        // Si no ha visto al jugador, avanza al siguiente waypoint y vuelve a patrullar
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

        CambiarEstado(EstadoNPC.Patrolling);
        isAlertRotating = false;
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

    // Llama a esto cuando el NPC escuche un sonido (por ejemplo, OnTriggerEnter de un collider del jugador)
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Sound"))
        {
            alertPosition = other.transform.position;
            CambiarEstado(EstadoNPC.Alert);
        }
    }
}