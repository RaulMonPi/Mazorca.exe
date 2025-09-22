using UnityEngine;

public class FloorManager : MonoBehaviour
{
    public GameObject floorPrefab;   // tu prefab de suelo
    public int width = 10;           // ancho en tiles
    public int height = 10;          // alto en tiles
    public float tileSize = 1f;      // tamaño del prefab (si mide 1x1, dejar en 1)
    public Vector3 startPosition = Vector3.zero; // posición inicial desde el editor

    void Start()
    {
        GenerateFloor();
    }

    void GenerateFloor()
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector3 pos = startPosition + new Vector3(x * tileSize, 0, z * tileSize);
                Instantiate(floorPrefab, pos, Quaternion.identity, transform);
            }
        }
    }
}
