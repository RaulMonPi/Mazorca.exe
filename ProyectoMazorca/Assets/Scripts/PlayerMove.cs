using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    [Header("Velocidades")]
    public float walkSpeed = 5f;
    public float runSpeed = 9f;
    public float rotationSpeed = 10f;

    public Animator animator;

    private Rigidbody rb;
    private Vector3 moveDirection;


    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        if (rb != null)
            rb.freezeRotation = true;
    }

    void Update()
    {
        // Entrada WASD / Flechas
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        moveDirection = new Vector3(horizontal, 0f, vertical).normalized;

        if (animator != null)
        {
            bool isWalking = moveDirection.magnitude > 0.1f;
            animator.SetBool("walking", isWalking);
        }

        // Rotar si hay movimiento
        if (moveDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    void FixedUpdate()
    {
        if (moveDirection.magnitude >= 0.1f)
        {
            // Velocidad (caminar o correr con Shift)
            float speed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;

            // Movimiento físico
            Vector3 newPosition = rb.position + moveDirection * speed * Time.fixedDeltaTime;
            rb.MovePosition(newPosition);
        }
    }
    public bool IsRunning()
    {
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    }
}
