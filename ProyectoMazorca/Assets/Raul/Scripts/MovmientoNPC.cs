using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MovmientoNPC : MonoBehaviour
{
    public Transform[] waypoints;
    public float patrolSpeed = 1f;
    public float chaseSpeed = 2f;
    public float alertSpeed = 1.2f;
    public float reachDistance = 1.5f;
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

    private Vector3 alertPosition;
    private bool isAlertRotating = false;

    private DecisionTreeNode decisionTreeRoot;
    private bool heardSound = false;

    private int savedWaypointIndex = -1;

    public EnemyGroupManager groupManager;

    private bool hasAlertedGroup = false; // Elimina lastAlertTime

    void Start()
    {
        pathfinder = GetComponent<Pathfinding>();
        if (pathfinder == null) pathfinder = gameObject.AddComponent<Pathfinding>();
        nextPathUpdateTime = Time.time;

        // Construye el árbol de decisión
        decisionTreeRoot =
            new DecisionConditionNode(
                npc => npc.IsPlayerInSight(),
                new DecisionActionNode(npc => {
                    //Debug.Log("NPC: Persiguiendo jugador");
                    npc.ChasingUpdate();
                }),
                new DecisionConditionNode(
                    npc => npc.HasHeardSound(),
                    new DecisionActionNode(npc => {
                        //Debug.Log("NPC: Investigando sonido");
                        npc.AlertUpdate();
                    }),
                    new DecisionActionNode(npc => {
                        //Debug.Log("NPC: Patrullando");
                        npc.PatrollingUpdate();
                    })
                )
            );
    }

    void Update()
    {
        decisionTreeRoot.Evaluate(this);
        FollowPathUpdate();
    }

    // ---------------- PATROL ----------------
    void PatrollingUpdate()
    {
        if (waypoints.Length == 0 || isWaiting) return;

        Transform target = waypoints[currentWaypoint];

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

        // ALERTA AL GRUPO solo una vez
        if (groupManager != null && !hasAlertedGroup)
        {
            groupManager.AlertGroup(player.position, this);
            hasAlertedGroup = true;
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
        // Si ya estoy en el destino, no busco ruta
        if (distance <= reachDistance)
        {
            isFollowingPath = false;

            if (IsPlayerInSight())
            {
                // El árbol de decisión se encargará de cambiar a persecución
            }
            else if (!isAlertRotating)
            {
                StartCoroutine(AlertRotateAndCheck());
            }
            return;
        }

        if (Time.time >= nextPathUpdateTime)
        {
            nextPathUpdateTime = Time.time + pathUpdateRate;
            pathfinder.StartFindPath(transform.position, alertPosition, OnPathFound);
        }
    }

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
                isAlertRotating = false;
                yield break;
            }

            yield return null;
        }

        // Al terminar la rotación, vuelve a patrullar desde el waypoint guardado
        if (savedWaypointIndex != -1)
        {
            currentWaypoint = savedWaypointIndex;
            savedWaypointIndex = -1;
        }

        // --- NUEVO: crea e inserta un waypoint justo antes del destino actual ---
        GameObject noiseWaypoint = new GameObject("NoiseWaypoint");
        noiseWaypoint.transform.position = transform.position;
        var waypointsList = new List<Transform>(waypoints);
        waypointsList.Insert(currentWaypoint, noiseWaypoint.transform);
        waypoints = waypointsList.ToArray();
        // Ajusta el índice para que el NPC vaya al nuevo waypoint
        // (el nuevo waypoint está en currentWaypoint, así que no hay que sumar nada)
        // ------------------------------------------------------

        heardSound = false;
        isAlertRotating = false;

        // Pide un nuevo path hacia el waypoint actual para retomar la patrulla
        if (waypoints.Length > 0)
        {
            pathfinder.StartFindPath(transform.position, waypoints[currentWaypoint].position, OnPathFound);
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
        if (!isFollowingPath || currentPath == null)
        {
            // --- NUEVO BLOQUE: Si estaba investigando (alerta), vuelve a patrullar ---
            if (heardSound && !IsPlayerInSight())
            {
                heardSound = false;
                isAlertRotating = false;

                // Restaura el waypoint guardado si existe
                if (savedWaypointIndex != -1)
                {
                    currentWaypoint = savedWaypointIndex;
                    savedWaypointIndex = -1;
                }

                // Pide el path hacia el waypoint actual para retomar la patrulla
                if (waypoints.Length > 0 && pathfinder != null)
                {
                    nextPathUpdateTime = Time.time + pathUpdateRate;
                    pathfinder.StartFindPath(transform.position, waypoints[currentWaypoint].position, OnPathFound);
                }
            }
            // ------------------------------------------------------------------------
            return;
        }

        if (currentPathIndex < 0 || currentPathIndex >= currentPath.Length)
        {
            isFollowingPath = false;

            // --- BLOQUE DE ALERTA YA EXISTENTE ---
            if (heardSound)
            {
                heardSound = false;
                isAlertRotating = false;

                if (savedWaypointIndex != -1)
                {
                    currentWaypoint = savedWaypointIndex;
                    savedWaypointIndex = -1;
                }

                if (waypoints.Length > 0 && pathfinder != null)
                {
                    nextPathUpdateTime = Time.time + pathUpdateRate;
                    pathfinder.StartFindPath(transform.position, waypoints[currentWaypoint].position, OnPathFound);
                }
            }
            // -------------------------------------
            return;
        }

        Vector3 targetWaypoint = currentPath[currentPathIndex];
        targetWaypoint.y = transform.position.y;

        float speed = patrolSpeed;
        if (IsPlayerInSight())
            speed = chaseSpeed;
        else if (heardSound)
            speed = alertSpeed;

        Vector3 direction = (targetWaypoint - transform.position).normalized;

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

        // ---- pide el path inmediatamente y orienta al objetivo ----
        if (waypoints.Length > 0)
        {
            Vector3 targetPos = waypoints[currentWaypoint].position;
            Vector3 flatDir = targetPos - transform.position;
            flatDir.y = 0f;
            if (flatDir.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(flatDir.normalized);

            if (pathfinder != null)
            {
                nextPathUpdateTime = Time.time + pathUpdateRate;
                pathfinder.StartFindPath(transform.position, targetPos, OnPathFound);
            }
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
            heardSound = true;
            savedWaypointIndex = currentWaypoint; // Guarda el waypoint actual
        }
    }

    public void ReceiveAlert(Vector3 position)
    {
        alertPosition = position;
        heardSound = true;
        savedWaypointIndex = currentWaypoint;
        Debug.Log("alerta de sonido");
        isAlertRotating = false;
        isWaiting = false;
        isFollowingPath = false;
        hasAlertedGroup = false; // Permite que este NPC pueda alertar si ve al jugador después
        nextPathUpdateTime = Time.time + pathUpdateRate;
        pathfinder.StartFindPath(transform.position, alertPosition, OnPathFound);
    }

    public bool HasHeardSound()
    {
        return heardSound;
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
