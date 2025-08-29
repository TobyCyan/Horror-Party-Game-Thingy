using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] public Transform orientation;
    [SerializeField] public float moveSpeed = 6f;
    [SerializeField] public float sprintMultiplier = 1.6f;
    [SerializeField] public float jumpForce = 5.5f;
    [SerializeField] public float targetSpeed = 10f; 
    [SerializeField] public float airMultiplier = 0.5f;
    [SerializeField] public float brakeStrength = 3f;

    private PlayerControls controls;
    private Rigidbody rb;
    private float xRot;
    private bool grounded;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        controls = new PlayerControls();
    }

    void OnEnable() => controls.Player.Enable();
    void OnDisable() => controls.Player.Disable();

    void Update()
    {
        Vector2 moveInput = controls.Player.Move.ReadValue<Vector2>();
        bool sprint = controls.Player.Sprint.IsPressed();

        Vector3 forward = Vector3.ProjectOnPlane(orientation.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(orientation.right, Vector3.up).normalized;
        Vector3 dir = (forward * moveInput.y + right * moveInput.x).normalized;

        if (rb.linearVelocity.magnitude > targetSpeed)
        {
            SpeedControl();
        }

        if (grounded)
        {
            float targetSpeed = moveSpeed * (sprint ? sprintMultiplier : 1f);

            if (dir.sqrMagnitude > 0.01f)  // moving
            {
                rb.AddForce(dir * targetSpeed * 10f, ForceMode.Force);
            }
            else  // no input -> brake
            {
                rb.linearVelocity = Vector3.Lerp(
                    rb.linearVelocity,
                    new Vector3(0f, rb.linearVelocity.y, 0f),   // keep gravity
                    brakeStrength * Time.deltaTime
                );
            }

            if (controls.Player.Jump.triggered)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            }
        }
        else
        {
            rb.AddForce(dir * moveSpeed * 10f * airMultiplier, ForceMode.Force);
        }

    }

    void FixedUpdate()
    {
        grounded = Physics.Raycast(transform.position, Vector3.down, 1.1f); // simple ground check
    }

    void SpeedControl()
    {
        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (flatVel.magnitude > moveSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * moveSpeed;
            rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
        }
    }
}
