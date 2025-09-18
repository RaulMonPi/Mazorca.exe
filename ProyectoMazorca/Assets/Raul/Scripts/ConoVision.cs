using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ConoVision : MonoBehaviour
{
    public float visionAngle = 60f; // Ángulo del cono en grados
    public float visionDistance = 5f; // Longitud del cono
    public int segments = 30; // Más segmentos = cono más suave
    public Material visionMaterial; // Asigna tu material en el Inspector
    public string wallLayerName = "Paredes"; // Nombre de la capa de paredes

    private Mesh mesh;
    private int wallLayerMask;

    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        if (visionMaterial != null)
            GetComponent<MeshRenderer>().material = visionMaterial;
        else
            Debug.LogWarning("Asigna un material al Cono de Visión.");

        wallLayerMask = LayerMask.GetMask(wallLayerName);
        CreateVisionCone();
    }

    void Update()
    {
        CreateVisionCone();
    }

    void CreateVisionCone()
    {
        mesh.Clear();

        Vector3[] vertices = new Vector3[segments + 2];
        int[] triangles = new int[segments * 3];

        vertices[0] = Vector3.zero; // Centro del cono

        float halfAngle = visionAngle * 0.5f;
        // Compensar la escala global del objeto
        float scaleCompensate = transform.lossyScale.x; // Suponiendo escala uniforme

        for (int i = 0; i <= segments; i++)
        {
            float angle = -halfAngle + (visionAngle * i / segments);
            float rad = Mathf.Deg2Rad * angle;

            Vector3 localDir = new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad));
            Vector3 worldDir = transform.TransformDirection(localDir);

            RaycastHit hit;
            Vector3 vertex;
            // Compensar la distancia de vision por la escala
            float compensatedVisionDistance = visionDistance * scaleCompensate;

            if (Physics.Raycast(transform.position, worldDir, out hit, compensatedVisionDistance, wallLayerMask))
            {
                vertex = transform.InverseTransformPoint(hit.point);
            }
            else
            {
                vertex = localDir * visionDistance; // ¡OJO! Aquí usamos visionDistance sin escalar
            }
            vertices[i + 1] = vertex;
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