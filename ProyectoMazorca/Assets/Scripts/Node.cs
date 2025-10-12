// EN Node.cs

using UnityEngine;
using System;

// Modificar: de 'IComparable<Node>' a 'IHeapItem<Node>'
public class Node : IHeapItem<Node>
{
    public bool walkable;
    public Vector3 worldPosition;
    public int gridX;
    public int gridY;

    // A* fields
    public int gCost; // Coste del camino desde el inicio
    public int hCost; // Coste heurístico hasta el final
    public Node parent;

    public int fCost => gCost + hCost;

    // **CAMPO PARA EL HEAP:**
    private int _heapIndex;
    public int HeapIndex 
    {
        get { return _heapIndex; }
        set { _heapIndex = value; }
    }
    // ----------------------

    public Node(bool walkable, Vector3 worldPosition, int gridX, int gridY)
    {
        this.walkable = walkable;
        this.worldPosition = worldPosition;
        this.gridX = gridX;
        this.gridY = gridY;
    }

    // CompareTo para ordenar: menor fCost => mayor prioridad (el heap usa '>' para subir)
    public int CompareTo(Node other)
    {
        int compare = fCost.CompareTo(other.fCost);
        if (compare == 0)
        {
            compare = hCost.CompareTo(other.hCost);
        }
        // Invertimos el resultado para que el Heap priorice el menor F-Cost.
        return -compare; 
    }
}

