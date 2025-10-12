using UnityEngine;

public class Node {
    public bool walkable;
    public Vector3 worldPosition;
    public int gridX;
    public int gridY;

    // A* campos (se usan más tarde)
    public int gCost;
    public int hCost;
    public int F => gCost + hCost;
    public Node parent;

    public Node(bool walkable, Vector3 worldPosition, int gridX, int gridY) {
        this.walkable = walkable;
        this.worldPosition = worldPosition;
        this.gridX = gridX;
        this.gridY = gridY;
    }
}

