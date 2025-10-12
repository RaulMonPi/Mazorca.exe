using UnityEngine;
using System;

public class Node : IComparable<Node>
{
    public bool walkable;
    public Vector3 worldPosition;
    public int gridX;
    public int gridY;

    // A* fields
    public int gCost;
    public int hCost;
    public Node parent;

    // heap index para la implementación del heap
    public int heapIndex;

    public int fCost => gCost + hCost;

    public Node(bool walkable, Vector3 worldPosition, int gridX, int gridY)
    {
        this.walkable = walkable;
        this.worldPosition = worldPosition;
        this.gridX = gridX;
        this.gridY = gridY;
    }

    // CompareTo para ordenar en el heap: menor fCost => mayor prioridad.
    public int CompareTo(Node other)
    {
        int compare = fCost.CompareTo(other.fCost);
        if (compare == 0)
        {
            compare = hCost.CompareTo(other.hCost);
        }
        // invertimos porque el heap que implementaremos usa '>' para subir
        return -compare;
    }
}

