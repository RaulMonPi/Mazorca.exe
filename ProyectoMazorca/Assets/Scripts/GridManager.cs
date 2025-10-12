using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Grid Settings")]
    public Vector2 gridWorldSize = new Vector2(20f, 20f); // en unidades del mundo (X,Z)
    public float nodeRadius = 0.5f; // medio del lado del nodo aproximado
    public bool allowDiagonals = true;

    [Header("Obstacle Detection")]
    public LayerMask obstacleMask; // asigna la layer de paredes/objetos
    [Tooltip("Altura para muestrear suelo/terreno con raycast (si aplica)")]
    public float raycastHeight = 5f;
    [Tooltip("Si true, usa CheckBox; si false, usa CheckSphere")]
    public bool useBoxCheck = true;

    Node[,] grid;
    float nodeDiameter;
    int gridSizeX, gridSizeY;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(this);

        InitializeGrid();
    }

    private void OnValidate()
    {
        // Esto asegura que el editor actualiza la grid cuando cambias parametros en tiempo de edición
        if (nodeRadius <= 0.01f) nodeRadius = 0.01f;
        InitializeGrid();
    }

    void InitializeGrid()
    {
        nodeDiameter = nodeRadius * 2f;
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);

        CreateGrid();
    }

    void CreateGrid()
    {
        grid = new Node[gridSizeX, gridSizeY];

        // esquina inferior izquierda en XZ (asumiendo que transform.position es el centro de la grid)
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
                bool walkable = !IsBlocked(worldPoint);
                grid[x, y] = new Node(walkable, worldPoint, x, y);
            }
        }
    }

    bool IsBlocked(Vector3 worldPoint)
    {
        if (useBoxCheck)
        {
            // CheckBox con tamaño del nodo (ligeramente reducido para evitar falsos positivos)
            Vector3 halfExtents = Vector3.one * (nodeRadius * 0.9f); // usa X = Y = Z, pero se ignora Y si el collider es vertical
            // Usamos Physics.OverlapBox en lugar de CheckBox (CheckBox no existe, se hace con Overlap)
            Collider[] hits = Physics.OverlapBox(worldPoint, halfExtents, Quaternion.identity, obstacleMask);
            return hits.Length > 0;
        }
        else
        {
            float checkRadius = nodeRadius * 0.9f;
            Collider[] hits = Physics.OverlapSphere(worldPoint, checkRadius, obstacleMask);
            return hits.Length > 0;
        }
    }

    // Exposición pública de la grid (útil para A*)
    public Node[,] Grid => grid;

    // Convierte una posición del mundo a su nodo correspondiente
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

    // Devuelve vecinos; puedes cambiar allowDiagonals para permitir o no diagonales
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
                {
                    neighbours.Add(grid[checkX, checkY]);
                }
            }
        }

        return neighbours;
    }

    // Opcional: si la posición objetivo cae en un nodo bloqueado, busca el nodo caminable más cercano.
    public Node GetClosestWalkableNode(Vector3 worldPos, int searchRadius = 3)
    {
        Node start = NodeFromWorldPoint(worldPos);
        if (start.walkable) return start;

        for (int r = 1; r <= searchRadius; r++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    int x = start.gridX + dx;
                    int y = start.gridY + dy;
                    if (x >= 0 && x < gridSizeX && y >= 0 && y < gridSizeY)
                    {
                        if (grid[x, y].walkable) return grid[x, y];
                    }
                }
            }
        }
        return null; // no se encontró en el radio
    }

    // Debug: dibuja la grid en editor con Gizmos
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 1f, gridWorldSize.y));

        if (grid == null) return;

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                Node n = grid[x, y];
                Gizmos.color = n.walkable ? Color.white : Color.red;
                float drawSize = (nodeDiameter - 0.05f);
                Gizmos.DrawCube(n.worldPosition + Vector3.up * 0.01f, new Vector3(drawSize, 0.02f, drawSize));
            }
        }
    }
}
