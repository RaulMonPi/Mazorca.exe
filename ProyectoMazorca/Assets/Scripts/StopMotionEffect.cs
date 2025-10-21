using UnityEngine;

[RequireComponent(typeof(Animator))]
public class StopMotionEffect : MonoBehaviour
{
    public Animator animator;
    [Range(1, 30)] public int stopMotionFPS = 12;

    private float frameDuration;
    private float accumulatedTime = 0f;

    void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        animator.speed = 0f; // Control manual
        frameDuration = 1f / stopMotionFPS;
    }

    void Update()
    {
        if (animator == null) return;

        accumulatedTime += Time.deltaTime;

        while (accumulatedTime >= frameDuration)
        {
            animator.Update(frameDuration); // Avanza la animación un "frame"
            accumulatedTime -= frameDuration;
        }
    }
}
