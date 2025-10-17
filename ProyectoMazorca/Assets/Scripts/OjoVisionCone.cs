using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class OjoVisionCone : MonoBehaviour
{
    public float detectionRange = 5f;
    public float detectionAngle = 45f;
    public int segments = 40;
    public Color coneColor = new Color(1f, 1f, 0.9f, 0.3f);
    public float heightOffset = 0.5f; // Mitad de la altura del ojo
    public string wallLayerName = "Paredes"; // Nombre de la capa de paredes

    private Mesh mesh;
    private Material mat;
    private int wallLayerMask;

    void Awake()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = coneColor;
        GetComponent<MeshRenderer>().material = mat;

        wallLayerMask = LayerMask.GetMask(wallLayerName);
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

        // Origen del cono en la mitad de la altura del ojo
        Vector3 origin = transform.position + Vector3.up * heightOffset;
        vertices[0] = transform.InverseTransformPoint(origin);

        float halfAngle = detectionAngle * 0.5f;

        for (int i = 0; i <= segments; i++)
        {
            float angle = -halfAngle + detectionAngle * i / segments;
            float rad = Mathf.Deg2Rad * angle;

            Vector3 localDir = new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad));
            Vector3 worldDir = transform.TransformDirection(localDir);

            RaycastHit hit;
            Vector3 vertexWorld;
            if (Physics.Raycast(origin, worldDir, out hit, detectionRange, wallLayerMask))
            {
                vertexWorld = hit.point;
            }
            else
            {
                vertexWorld = origin + worldDir * detectionRange;
            }
            vertices[i + 1] = transform.InverseTransformPoint(vertexWorld);
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