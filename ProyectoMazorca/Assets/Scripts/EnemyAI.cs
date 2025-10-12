using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterController))]
public class EnemyAI : MonoBehaviour
{
    public Transform target;
    public float hearingRadius = 8f;
    public float recalcInterval = 0.2f; // mínimo intervalo entre recalculos
    public float recalcDistanceThreshold = 1.0f; // recalcula solo si objetivo se mueve > esto
    public float speed = 3f;
    public float nextWaypointThreshold = 0.2f;
    public bool useSmoothing = true;

    private List<Vector3> path;
    private int pathIndex = 0;
    private Vector3 lastTargetPosition;
    private Coroutine followRoutine;

    void Start()
    {
        if (target != null) lastTargetPosition = target.position;
    }

    void Update()
    {
        if (target == null) return;

        float distToTarget = Vector3.Distance(transform.position, target.position);
        if (distToTarget <= hearingRadius)
        {
            // check if need to request a path
            if (path == null || Vector3.Distance(lastTargetPosition, target.position) > recalcDistanceThreshold)
            {
                lastTargetPosition = target.position;
                RequestPath();
            }
        }
        else
        {
            // fuera de radio: stop following
            if (followRoutine != null) { StopCoroutine(followRoutine); followRoutine = null; }
            path = null;
        }
    }

    void RequestPath()
    {
        // pedimos la ruta y comenzamos a seguirla
        path = PathfindingAStar.Instance.FindPath(transform.position, target.position, useSmoothing);
        pathIndex = 0;
        if (followRoutine != null) StopCoroutine(followRoutine);
        if (path != null && path.Count > 0) followRoutine = StartCoroutine(FollowPath());
    }

    IEnumerator FollowPath()
    {
        CharacterController cc = GetComponent<CharacterController>();
        while (pathIndex < path.Count)
        {
            Vector3 waypoint = path[pathIndex];
            Vector3 waypointFlat = new Vector3(waypoint.x, transform.position.y, waypoint.z);

            while (Vector3.Distance(transform.position, waypointFlat) > nextWaypointThreshold)
            {
                Vector3 dir = (waypointFlat - transform.position).normalized;
                // mover con CharacterController si existe
                if (cc != null) cc.Move(dir * speed * Time.deltaTime);
                else transform.position += dir * speed * Time.deltaTime;

                transform.forward = Vector3.Lerp(transform.forward, dir, Time.deltaTime * 10f);
                yield return null;
            }

            pathIndex++;
            yield return null;
        }

        followRoutine = null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, hearingRadius);
        if (path != null)
        {
            Gizmos.color = Color.cyan;
            foreach (Vector3 p in path) Gizmos.DrawSphere(p, 0.08f);
        }
    }
}

