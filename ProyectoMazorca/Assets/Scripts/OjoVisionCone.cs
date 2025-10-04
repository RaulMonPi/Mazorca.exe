using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class OjoVisionCone : MonoBehaviour
{
    public float detectionRange = 5f;
    public float detectionAngle = 45f;
    public int segments = 40;
    public Color coneColor = new Color(1f, 1f, 0f, 0.3f);
    public float heightOffset = 0.5f; // Offset vertical desde el suelo

    private Mesh mesh;
    private Material mat;

    void Awake()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = coneColor;
        GetComponent<MeshRenderer>().material = mat;
    }

    void LateUpdate()
    {
        DrawCone();
    }

    void DrawCone()
    {
        mesh.Clear();

        Vector3[] vertices = new Vector3[segments + 2];
        int[] triangles = new int[segments * 3];

        // El vértice de origen está a la mitad de la altura del ojo
        vertices[0] = new Vector3(0, heightOffset, 0);
        float halfAngle = detectionAngle / 2f;

        for (int i = 0; i <= segments; i++)
        {
            float angle = -halfAngle + detectionAngle * ((float)i / segments);
            float rad = Mathf.Deg2Rad * angle;
            // Todos los vértices del borde también tienen el mismo offset en Y
            vertices[i + 1] = new Vector3(Mathf.Sin(rad), heightOffset, Mathf.Cos(rad)) * detectionRange;
            vertices[i + 1].y = heightOffset;
            vertices[i + 1] = vertices[i + 1].normalized * detectionRange;
            vertices[i + 1].y = heightOffset;
        }

        for (int i = 0; i < segments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }
}