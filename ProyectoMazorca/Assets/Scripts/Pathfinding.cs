using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Pathfinding : MonoBehaviour
{
    // Delegado para el callback que se ejecutará al terminar la búsqueda
    public delegate void PathCallback(Vector3[] path, bool success);

    // Inicia la búsqueda de camino en una Coroutine
    public void StartFindPath(Vector3 startWorld, Vector3 targetWorld, PathCallback callback)
    {
        StartCoroutine(FindPath(startWorld, targetWorld, callback));
    }

    IEnumerator FindPath(Vector3 startWorld, Vector3 targetWorld, PathCallback callback)
    {
        Vector3[] waypoints = new Vector3[0];
        bool pathSuccess = false;

        Node startNode = GridManager.Instance.NodeFromWorldPoint(startWorld);
        Node targetNode = GridManager.Instance.NodeFromWorldPoint(targetWorld);

        // Añade esto:
        if (startNode == targetNode)
        {
            callback(new Vector3[] { startNode.worldPosition }, true);
            yield break;
        }

        if (startNode.walkable && targetNode.walkable)
        {
            Heap<Node> openSet = new Heap<Node>(GridManager.Instance.gridSizeX * GridManager.Instance.gridSizeY);
            HashSet<Node> closedSet = new HashSet<Node>();
            openSet.Add(startNode);

            while (openSet.Count > 0)
            {
                Node currentNode = openSet.RemoveFirst();
                closedSet.Add(currentNode);

                if (currentNode == targetNode)
                {
                    pathSuccess = true;
                    break;
                }

                foreach (Node neighbour in GridManager.Instance.GetNeighbours(currentNode))
                {
                    if (!neighbour.walkable || closedSet.Contains(neighbour))
                        continue;

                    int newMovementCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour);

                    if (newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
                    {
                        neighbour.gCost = newMovementCostToNeighbour;
                        neighbour.hCost = GetDistance(neighbour, targetNode);
                        neighbour.parent = currentNode;

                        if (!openSet.Contains(neighbour))
                            openSet.Add(neighbour);
                        else
                            openSet.UpdateItem(neighbour);
                    }
                }
                yield return null;
            }
        }

        if (pathSuccess)
        {
            waypoints = RetracePath(startNode, targetNode);
            if (waypoints.Length == 0)
            {
                pathSuccess = false;
            }
        }

        callback(waypoints, pathSuccess);
    }

    int GetDistance(Node nodeA, Node nodeB)
    {
        int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int dstY = Mathf.Abs(nodeA.gridY - nodeB.gridY);

        if (GridManager.Instance.allowDiagonals)
        {
            if (dstX > dstY)
                return 14 * dstY + 10 * (dstX - dstY);
            return 14 * dstX + 10 * (dstY - dstX);
        }
        else
        {
            return 10 * (dstX + dstY);
        }
    }

    Vector3[] RetracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;

        while (currentNode != null && currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }

        if (path.Count == 0)
        {
            Debug.LogWarning("RetracePath: no se pudo reconstruir el camino (lista vacía).");
            return new Vector3[0];
        }

        Vector3[] waypoints = SimplifyPath(path);
        System.Array.Reverse(waypoints);
        return waypoints;
    }

    Vector3[] SimplifyPath(List<Node> path)
    {
        if (path == null || path.Count == 0)
            return new Vector3[0];

        List<Vector3> waypoints = new List<Vector3>();
        Vector2 directionOld = Vector2.zero;

        for (int i = 1; i < path.Count; i++)
        {
            Vector2 directionNew = new Vector2(path[i - 1].gridX - path[i].gridX, path[i - 1].gridY - path[i].gridY);

            if (directionNew != directionOld)
            {
                waypoints.Add(path[i - 1].worldPosition);
            }
            directionOld = directionNew;
        }

        waypoints.Add(path[path.Count - 1].worldPosition);
        return waypoints.ToArray();
    }
}
