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

        // 1. Obtener Nodos de inicio y fin
        Node startNode = GridManager.Instance.NodeFromWorldPoint(startWorld);
        Node targetNode = GridManager.Instance.NodeFromWorldPoint(targetWorld);

        if (startNode.walkable && targetNode.walkable)
        {
            // Inicializar estructuras de A*
            Heap<Node> openSet = new Heap<Node>(GridManager.Instance.gridSizeX * GridManager.Instance.gridSizeY);
            HashSet<Node> closedSet = new HashSet<Node>();
            openSet.Add(startNode);

            while (openSet.Count > 0)
            {
                // Obtener el nodo con menor F cost (más prometedor)
                Node currentNode = openSet.RemoveFirst();
                closedSet.Add(currentNode);

                // **ÉXITO:** Se ha llegado al destino
                if (currentNode == targetNode)
                {
                    pathSuccess = true;
                    break;
                }

                // Evaluar Vecinos
                foreach (Node neighbour in GridManager.Instance.GetNeighbours(currentNode))
                {
                    if (!neighbour.walkable || closedSet.Contains(neighbour))
                    {
                        continue;
                    }

                    // Calcular nuevo G Cost
                    int newMovementCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour);
                    
                    // Si encontramos un camino mejor o si el nodo es nuevo
                    if (newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
                    {
                        neighbour.gCost = newMovementCostToNeighbour;
                        neighbour.hCost = GetDistance(neighbour, targetNode);
                        neighbour.parent = currentNode; // Establecer el padre

                        if (!openSet.Contains(neighbour))
                        {
                            openSet.Add(neighbour);
                        }
                        else
                        {
                            openSet.UpdateItem(neighbour);
                        }
                    }
                }
                // Cedemos el control para evitar que la búsqueda bloquee la aplicación (frame rate)
                yield return null; 
            }
        }
        
        // Si se encontró un camino, lo reconstruimos
        if (pathSuccess)
        {
            waypoints = RetracePath(startNode, targetNode);
        }

        // Llamar al método del enemigo con el resultado
        callback(waypoints, pathSuccess);
    }

    // Calcula la distancia heurística (Manhattan o Diagonal)
    int GetDistance(Node nodeA, Node nodeB)
    {
        int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int dstY = Mathf.Abs(nodeA.gridY - nodeB.gridY);

        if (GridManager.Instance.allowDiagonals)
        {
            // Costes para diagonal (14, 10)
            if (dstX > dstY)
                return 14 * dstY + 10 * (dstX - dstY);
            return 14 * dstX + 10 * (dstY - dstX);
        }
        else
        {
            // Costes para Manhattan (10)
            return 10 * (dstX + dstY);
        }
    }

    // Reconstruye el camino desde el nodo final hasta el nodo inicial
    Vector3[] RetracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }
        
        // Simplificar y convertir a array de Vector3
        Vector3[] waypoints = SimplifyPath(path);
        System.Array.Reverse(waypoints); 
        return waypoints;
    }

    // Opcional: Simplifica el camino eliminando nodos intermedios que no cambian de dirección
    Vector3[] SimplifyPath(List<Node> path)
    {
        List<Vector3> waypoints = new List<Vector3>();
        // La dirección se basa en las coordenadas de la cuadrícula
        Vector2 directionOld = Vector2.zero; 

        for (int i = 1; i < path.Count; i++)
        {
            Vector2 directionNew = new Vector2(path[i-1].gridX - path[i].gridX, path[i-1].gridY - path[i].gridY);
            
            // Si la dirección ha cambiado, el nodo anterior es un 'corner' (esquina)
            if (directionNew != directionOld)
            {
                waypoints.Add(path[i-1].worldPosition);
            }
            directionOld = directionNew;
        }
        // Añadir el punto final del camino
        waypoints.Add(path[path.Count - 1].worldPosition); 
        return waypoints.ToArray();
    }
}