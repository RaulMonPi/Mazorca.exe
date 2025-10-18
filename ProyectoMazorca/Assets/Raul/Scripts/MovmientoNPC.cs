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
    public float nodeReachDistance = 0.15f;
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
    private bool isLockedOnAlertPath = false;
    private bool isAlertFromGroup = false;

    private bool yaInvestigado = false;

    void Start()
    {
        pathfinder = GetComponent<Pathfinding>();
        if (pathfinder == null) pathfinder = gameObject.AddComponent<Pathfinding>();
        nextPathUpdateTime = Time.time;

        // Construye el árbol de decisión
        decisionTreeRoot =
            new DecisionConditionNode(
                npc => npc.isLockedOnAlertPath,
                new DecisionActionNode(npc => npc.AlertUpdate()), // Solo hace alerta hasta terminar el path
                new DecisionConditionNode(
                    npc => npc.IsPlayerInSight(),
                    new DecisionActionNode(npc => npc.ChasingUpdate()),
                    new DecisionConditionNode(
                        npc => npc.HasHeardSound(),
                        new DecisionActionNode(npc => npc.AlertUpdate()),
                        new DecisionActionNode(npc => npc.PatrollingUpdate())
                    )
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
            // SOLO inicia la rotación si no es alerta de grupo ya investigada
            else if (!isAlertRotating && !(isAlertFromGroup && yaInvestigado))
            {
                StartCoroutine(AlertRotateAndCheck());
            }
            return;
        }

        if (Time.time >= nextPathUpdateTime)
        {
            //Debug.Log("Tiempo actualizar el path");
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
        // --- SOLO crea el waypoint si la alerta NO viene del grupo ---
        if (!isAlertFromGroup)
        {
            GameObject noiseWaypoint = new GameObject("NoiseWaypoint");
            noiseWaypoint.transform.position = transform.position;
            var waypointsList = new List<Transform>(waypoints);
            waypointsList.Insert(currentWaypoint, noiseWaypoint.transform);
            waypoints = waypointsList.ToArray();
            // Ajusta el índice para que el NPC vaya al nuevo waypoint
            // (el nuevo waypoint está en currentWaypoint, así que no hay que sumar nada)
            // ------------------------------------------------------
        }

        heardSound = false;
        isAlertRotating = false;
        yaInvestigado = true;

        // Si es alerta de grupo y ya ha investigado, termina aquí
        if (isAlertFromGroup && yaInvestigado)
            yield break;

        // Si no, sigue con la patrulla
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
            Debug.Log("No se pudo encontrar un camino.");
            isFollowingPath = false;
            currentPath = null;
        }
    }

    void FollowPathUpdate()
    {
        if (IsPlayerInSight())
        {
            // Ignora el path y mueve en recto al jugador
            Vector3 dirToPlayer = (player.position - transform.position).normalized;
            if (dirToPlayer != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(dirToPlayer);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
            }
            transform.position += dirToPlayer * chaseSpeed * Time.deltaTime;
            // No actualices el path ni el índice
            return;
        }

        Node node = GridManager.Instance.NodeFromWorldPoint(transform.position);
        if (!node.walkable)
        {
            Debug.LogWarning($"{name}: NPC está en nodo no caminable, forzando salida.");
            // Busca el nodo caminable más cercano y mueve al NPC ahí
            Vector3 safePos = GetNearestWalkablePosition(transform.position);
            transform.position = safePos;

            isFollowingPath = false;
            currentPath = null;

            // Pide un path hacia el destino original (alertPosition si estaba en alerta, waypoint si patrullando)
            if (heardSound || isLockedOnAlertPath)
            {
                if (pathfinder != null)
                {
                    nextPathUpdateTime = Time.time + pathUpdateRate;
                    pathfinder.StartFindPath(transform.position, alertPosition, OnPathFound);
                }
            }
            else if (waypoints.Length > 0 && pathfinder != null)
            {
                nextPathUpdateTime = Time.time + pathUpdateRate;
                pathfinder.StartFindPath(transform.position, waypoints[currentWaypoint].position, OnPathFound);
            }
            return;
        }
        if (!isFollowingPath || currentPath == null) return;

        if (currentPathIndex < 0 || currentPathIndex >= currentPath.Length)
        {
            isFollowingPath = false;

            // Si estaba investigando (alerta), vuelve a patrullar
            if (heardSound)
            {
                heardSound = false;
                isAlertRotating = false;
                isLockedOnAlertPath = false;

                if (savedWaypointIndex != -1)
                {
                    currentWaypoint = savedWaypointIndex;
                    savedWaypointIndex = -1;
                }

                // --- PARCHE: asegura que el índice es válido ---
                if (waypoints.Length > 0 && pathfinder != null)
                {
                    currentWaypoint = Mathf.Clamp(currentWaypoint, 0, waypoints.Length - 1);
                    nextPathUpdateTime = Time.time + pathUpdateRate;
                    pathfinder.StartFindPath(transform.position, waypoints[currentWaypoint].position, OnPathFound);
                }
            }
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

        if (Vector3.Distance(transform.position, targetWaypoint) < nodeReachDistance)
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
            savedWaypointIndex = currentWaypoint;
            isAlertFromGroup = false; // <--- NUEVO
        }
    }

    public void ReceiveAlert(Vector3 position)
    {
        alertPosition = GetNearestWalkablePosition(position);
        heardSound = true;
        savedWaypointIndex = currentWaypoint;
        Debug.Log("alerta de sonido");
        isAlertRotating = false;
        isWaiting = false;
        isFollowingPath = false;
        hasAlertedGroup = false;
        isLockedOnAlertPath = true;
        isAlertFromGroup = true;
        yaInvestigado = false; // <--- aquí
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

    Vector3 GetNearestWalkablePosition(Vector3 fromPosition)
    {
        Node startNode = GridManager.Instance.NodeFromWorldPoint(fromPosition);
        if (startNode != null && startNode.walkable)
            return startNode.worldPosition;

        // Search in concentric rings in world space and use NodeFromWorldPoint to avoid accessing the internal grid array.
        float maxSearchRadius = 5f; // max world units to search
        float step = 0.5f; // sampling step in world units
        Node nearest = null;
        float minDist = float.MaxValue;

        for (float radius = step; radius <= maxSearchRadius; radius += step)
        {
            int samples = Mathf.Max(8, Mathf.CeilToInt(2f * Mathf.PI * radius / step));
            for (int i = 0; i < samples; i++)
            {
                float angle = (i / (float)samples) * Mathf.PI * 2f;
                Vector3 samplePos = fromPosition + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Node checkNode = GridManager.Instance.NodeFromWorldPoint(samplePos);
                if (checkNode != null && checkNode.walkable)
                {
                    float dist = (checkNode.worldPosition - fromPosition).sqrMagnitude;
                    if (dist < minDist)
                    {
                        minDist = dist;
                        nearest = checkNode;
                    }
                }
            }

            // if we found at least one walkable node at this radius, we can stop expanding further
            if (nearest != null)
                break;
        }

        return nearest != null ? nearest.worldPosition : fromPosition;
    }
}
