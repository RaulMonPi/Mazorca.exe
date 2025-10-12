using UnityEngine;
using System.Collections.Generic;

public class PathfindingAStar : MonoBehaviour
{
    public static PathfindingAStar Instance { get; private set; }

    [Header("Pathfinding Settings")]
    public LayerMask obstacleMask; // usado para smoothing (Physics.Linecast)
    public bool allowDiagonals = true;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);
    }

    // Encuentra ruta de startWorld a targetWorld. Si smooth==true, hace line-of-sight shortcut
    public List<Vector3> FindPath(Vector3 startWorld, Vector3 targetWorld, bool smooth = true)
    {
        GridManager grid = GridManager.Instance;
        if (grid == null || grid.Grid == null) return null;

        Node startNode = grid.NodeFromWorldPoint(startWorld);
        Node targetNode = grid.NodeFromWorldPoint(targetWorld);

        // Si el nodo objetivo está bloqueado, busca nodo caminable cercano
        if (!targetNode.walkable)
        {
            Node alt = grid.GetClosestWalkableNode(targetWorld, 5);
            if (alt == null) return null;
            targetNode = alt;
        }

        int gridX = grid.Grid.GetLength(0);
        int gridY = grid.Grid.GetLength(1);

        // heap: máximo tamaño = total de nodos
        Heap<Node> openSet = new Heap<Node>(gridX * gridY);
        HashSet<Node> closedSet = new HashSet<Node>();

        // inicializar
        startNode.gCost = 0;
        startNode.hCost = GetHeuristic(startNode, targetNode);
        startNode.parent = null;

        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            Node current = openSet.RemoveFirst();
            closedSet.Add(current);

            if (current == targetNode)
            {
                List<Vector3> rawPath = RetracePath(startNode, targetNode);
                if (smooth) return SmoothPath(rawPath, grid);
                return rawPath;
            }

            foreach (Node neighbour in grid.GetNeighbours(current))
            {
                if (!neighbour.walkable || closedSet.Contains(neighbour)) continue;

                int tentativeG = current.gCost + GetDistance(current, neighbour);
                if (tentativeG < neighbour.gCost || neighbour.parent == null)
                {
                    neighbour.gCost = tentativeG;
                    neighbour.hCost = GetHeuristic(neighbour, targetNode);
                    neighbour.parent = current;

                    // Si no está en el openSet lo añadimos
                    // Aquí no tenemos Contains eficiente: añadiré sin comprobación
                    openSet.Add(neighbour);
                }
            }
        }

        // no path
        return null;
    }

    List<Vector3> RetracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
        Node current = endNode;
        while (current != startNode)
        {
            path.Add(current);
            current = current.parent;
            if (current == null) break;
        }
        path.Reverse();

        List<Vector3> worldPath = new List<Vector3>();
        foreach (var n in path) worldPath.Add(n.worldPosition);
        return worldPath;
    }

    int GetDistance(Node a, Node b)
    {
        int dx = Mathf.Abs(a.gridX - b.gridX);
        int dy = Mathf.Abs(a.gridY - b.gridY);
        if (dx > dy) return 14 * dy + 10 * (dx - dy);
        return 14 * dx + 10 * (dy - dx);
    }

    int GetHeuristic(Node a, Node b)
    {
        return GetDistance(a, b);
    }

    // Smoothing: intenta saltar nodos intermedios si hay line of sight
    List<Vector3> SmoothPath(List<Vector3> rawPath, GridManager grid)
    {
        if (rawPath == null || rawPath.Count == 0) return rawPath;
        List<Vector3> smoothed = new List<Vector3>();
        int currentIndex = 0;
        smoothed.Add(rawPath[0]);

        while (currentIndex < rawPath.Count - 1)
        {
            int nextIndex = rawPath.Count - 1; // intentar llegar hasta el final
            bool found = false;
            // iterar hacia atrás para encontrar el más lejano que sea visible
            for (int i = rawPath.Count - 1; i > currentIndex; i--)
            {
                Vector3 from = smoothed[smoothed.Count - 1] + Vector3.up * 0.1f;
                Vector3 to = rawPath[i] + Vector3.up * 0.1f;
                // si no hay obstáculo entre from y to, lo dejamos
                if (!Physics.Linecast(from, to, grid.obstacleMask))
                {
                    nextIndex = i;
                    found = true;
                    break;
                }
            }
            if (!found) nextIndex = currentIndex + 1; // fallback a siguiente nodo
            smoothed.Add(rawPath[nextIndex]);
            currentIndex = nextIndex;
        }

        // eliminar duplicados contiguos si existen
        for (int i = smoothed.Count - 1; i > 0; i--)
            if (Vector3.Distance(smoothed[i], smoothed[i - 1]) < 0.001f)
                smoothed.RemoveAt(i);

        return smoothed;
    }
}
