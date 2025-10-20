using UnityEngine;
using System.Collections;

public class MageTeleport : MonoBehaviour
{
    [Header("Puntos de teletransporte (asigna en el Inspector)")]
    public Transform[] teleportPoints;

    [Header("Configuración de tiempo")]
    public float visibleTime = 4f;
    public float fadeDuration = 0.5f;

    [Header("Rotación")]
    public float rotationSpeed = 20f; // grados por segundo

    private Renderer[] renderers;
    private bool isVisible = false;
    private int currentIndex = -1;

    void Start()
    {
        renderers = GetComponentsInChildren<Renderer>();

        if (teleportPoints.Length == 0)
        {
            Debug.LogError("⚠️ No hay puntos de teletransporte asignados al mago.");
            return;
        }

        StartCoroutine(TeleportLoop());
    }

    void Update()
    {
        if (isVisible)
        {
            // Rotar sobre el eje Y global sin alterar la rotación X (-90°)
            transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);

            // Mantener la X bloqueada en -90° (para que no se incline)
            Vector3 euler = transform.eulerAngles;
            euler.x = 270f; // -90° en X
            transform.eulerAngles = euler;
        }
    }

    IEnumerator TeleportLoop()
    {
        while (true)
        {
            int newIndex;
            do
            {
                newIndex = Random.Range(0, teleportPoints.Length);
            } while (newIndex == currentIndex && teleportPoints.Length > 1);

            currentIndex = newIndex;
            transform.position = teleportPoints[currentIndex].position;

            yield return StartCoroutine(Fade(true));

            yield return new WaitForSeconds(visibleTime);

            yield return StartCoroutine(Fade(false));
        }
    }

    IEnumerator Fade(bool appear)
    {
        float start = appear ? 0f : 1f;
        float end = appear ? 1f : 0f;
        float elapsed = 0f;

        if (appear) isVisible = true;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(start, end, elapsed / fadeDuration);

            foreach (Renderer r in renderers)
            {
                foreach (var mat in r.materials)
                {
                    Color c = mat.color;
                    c.a = alpha;
                    mat.color = c;
                }
            }

            yield return null;
        }

        foreach (Renderer r in renderers)
        {
            foreach (var mat in r.materials)
            {
                Color c = mat.color;
                c.a = end;
                mat.color = c;
            }
        }

        if (!appear) isVisible = false;
    }
}
