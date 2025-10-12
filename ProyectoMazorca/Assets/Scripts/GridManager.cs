using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Grid Settings")]
    public Vector2 gridWorldSize = new Vector2(20f, 20f);

    [Tooltip("Tamaño de medio nodo (usa el slider o escribe con cuidado)")]
    [Range(0.1f, 5f)]
    public float nodeRadius = 0.5f;

    [Tooltip("Permitir diagonales en la búsqueda")]
    public bool allowDiagonals = true;

    [Header("Obstacle Detection")]
    public LayerMask obstacleMask;
    public float raycastHeight = 5f;
    public bool useBoxCheck = true;

    [Header("Debug")]
    public bool drawGizmos = true;

    Node[,] grid;
    float nodeDiameter;
    int gridSizeX, gridSizeY;

    const int MAX_NODES = 100000;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);

        RebuildGrid();
    }

    void OnValidate()
    {
        nodeRadius = Mathf.Max(0.1f, nodeRadius);
        if (!Application.isPlaying)
        {
            RebuildGrid();
        }
    }

    public void RebuildGrid()
    {
        nodeDiameter = nodeRadius * 2f;
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);

        int totalNodes = gridSizeX * gridSizeY;
        if (totalNodes > MAX_NODES)
        {
            Debug.LogWarning($"⚠️ Grid demasiado grande ({totalNodes} nodos). Reduce gridWorldSize o aumenta nodeRadius.");
            grid = null;
            return;
        }

        CreateGrid();
    }

    void CreateGrid()
    {
        grid = new Node[gridSizeX, gridSizeY];
        Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x / 2f - Vector3.forward * gridWorldSize.y / 2f;

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.forward * (y * nodeDiameter + nodeRadius);

                // Si quieres la Y exacta (terrains/desnivel), raycast desde arriba:
                RaycastHit hit;
                Vector3 sampleOrigin = worldPoint + Vector3.up * raycastHeight;
                if (Physics.Raycast(sampleOrigin, Vector3.down, out hit, raycastHeight * 2f))
                {
                    worldPoint.y = hit.point.y;
                }

                bool walkable;
                if (useBoxCheck)
                {
                    Vector3 halfExtents = Vector3.one * (nodeRadius * 0.9f);
                    Collider[] hits = Physics.OverlapBox(worldPoint, halfExtents, Quaternion.identity, obstacleMask);
                    walkable = hits.Length == 0;
                }
                else
                {
                    float checkRadius = nodeRadius * 0.9f;
                    Collider[] hits = Physics.OverlapSphere(worldPoint, checkRadius, obstacleMask);
                    walkable = hits.Length == 0;
                }

                grid[x, y] = new Node(walkable, worldPoint, x, y);
            }
        }
    }

    public Node NodeFromWorldPoint(Vector3 worldPosition)
    {
        float percentX = (worldPosition.x - (transform.position.x - gridWorldSize.x / 2f)) / gridWorldSize.x;
        float percentY = (worldPosition.z - (transform.position.z - gridWorldSize.y / 2f)) / gridWorldSize.y;
        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);
        int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);
        return grid[x, y];
    }

    public List<Node> GetNeighbours(Node node)
    {
        List<Node> neighbours = new List<Node>();

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                if (!allowDiagonals && Mathf.Abs(dx) + Mathf.Abs(dy) > 1) continue;

                int checkX = node.gridX + dx;
                int checkY = node.gridY + dy;

                if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
                    neighbours.Add(grid[checkX, checkY]);
            }
        }

        return neighbours;
    }

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos || grid == null) return;

        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 1f, gridWorldSize.y));

        float drawSize = nodeDiameter - 0.05f;
        foreach (Node n in grid)
        {
            Gizmos.color = n.walkable ? Color.white : Color.red;
            Gizmos.DrawCube(n.worldPosition + Vector3.up * 0.01f, new Vector3(drawSize, 0.02f, drawSize));
        }
    }
}
