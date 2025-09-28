using UnityEngine;

public class StopMotionEffect : MonoBehaviour
{
    public Animator animator;
    [Range(1, 30)] public int stopMotionFPS = 12;

    private float frameTimer;
    private float frameDuration;
    private float holdTime;

    void Start()
    {
        frameDuration = 1f / stopMotionFPS;
    }

    void Update()
    {
        if (animator == null) return;

        frameTimer += Time.deltaTime;
        if (frameTimer >= frameDuration)
        {
            frameTimer = 0f;
            holdTime = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        }

        animator.Play(animator.GetCurrentAnimatorStateInfo(0).shortNameHash, 0, holdTime);
        animator.speed = 0f;
    }
}
