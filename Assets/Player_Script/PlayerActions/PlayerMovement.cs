using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerMovement : MonoBehaviour
{
    // --- Movement ---
    [Header("Movement")]
    [SerializeField] private float movementSpeed = 6f;
    [SerializeField] private float jumpForce = 5.5f;

    // --- Look ---
    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 120f;

    // --- Grounding ---
    [Header("Grounding")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private float groundCheckOffset = 0.05f;
    private CapsuleCollider cap;

    // --- Optional Speed Clamp ---
    [Header("Optional Speed Clamp")]
    [SerializeField] private float maxHorizontalSpeed = 10f;

    // --- Crawl settings ---
    [Header("Crouch Settings")]
    [SerializeField] private float crouchHeight = 1.0f;
    [SerializeField] private float crouchCenterY = 0.5f; // adjust based on model
    private float originalHeight;
    private Vector3 originalCenter;

    // --- Internals ---
    private Rigidbody rb;
    private PlayerControls controls;
    private Animator anim;
    private Vector2 moveInput;
    private bool grounded;
    private bool prevGrounded;
    private float prevVerticalSpeed;

    // --- Crawl state (toggle on button press) ---
    private bool isCrouching = false;

    // ----------------------------------------------------------------------

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        anim = GetComponentInChildren<Animator>();
        cap = GetComponent<CapsuleCollider>();
        controls = new PlayerControls();

        // Store original collider values
        originalHeight = cap.height;
        originalCenter = cap.center;
    }

    void OnEnable() => controls.Player.Enable();
    void OnDisable() => controls.Player.Disable();

    // --- Centralized ground check ---
    private bool ComputeGrounded()
    {
        float halfHeight = Mathf.Max(cap.height * 0.5f, cap.radius);
        Vector3 feet = cap.transform.TransformPoint(cap.center)
                     + Vector3.down * (halfHeight - cap.radius + groundCheckOffset);

        return Physics.CheckSphere(
            feet,
            groundCheckRadius,
            groundMask,
            QueryTriggerInteraction.Ignore
        );
    }

    // --- Function to adjust capsule when crouching ---
    private void SetCrouchState(bool crouching)
    {
        if (crouching)
        {
            cap.height = crouchHeight;
            cap.center = new Vector3(originalCenter.x, crouchCenterY, originalCenter.z);
        }
        else
        {
            cap.height = originalHeight;
            cap.center = originalCenter;
        }
    }

    void Update()
    {
        // Inputs
        moveInput = controls.Player.Move.ReadValue<Vector2>();

        // Toggle crouch on button press (edge-trigger)
        if (controls.Player.Crawl.triggered)
        {
            isCrouching = !isCrouching;
            anim?.SetBool("IsCrawling", isCrouching);

            // adjust collider
            SetCrouchState(isCrouching);
        }

        // Mouse look (yaw)
        Vector2 lookDelta = controls.Player.Look != null
            ? controls.Player.Look.ReadValue<Vector2>()
            : (Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero);

        float yaw = lookDelta.x * mouseSensitivity * Time.deltaTime;
        transform.Rotate(0f, yaw, 0f);

        // Grounding
        prevGrounded = grounded;
        grounded = ComputeGrounded();

        // Jump: only when grounded and not crouching
        if (grounded && !isCrouching && controls.Player.Jump.triggered)
        {
            anim?.SetTrigger("Jump");
        }

        // Animator parameters
        if (anim)
        {
            float horizontalMag = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude;
            bool isWalking = grounded && (moveInput.sqrMagnitude > 0.01f) && horizontalMag > 0.01f;

            anim.SetBool("IsGrounded", grounded);
            anim.SetBool("IsWalking", isWalking);
            anim.SetFloat("VerticalSpeed", rb.linearVelocity.y);

            if (grounded && (moveInput.x != 0f || moveInput.y != 0f))
            {
                anim.SetFloat("MoveX", moveInput.x);
                anim.SetFloat("MoveZ", moveInput.y);
            }
            else
            {
                anim.SetFloat("MoveX", 0f);
                anim.SetFloat("MoveZ", 0f);
            }
        }

        prevVerticalSpeed = rb.linearVelocity.y;
    }

    void FixedUpdate()
    {
        // Movement (reuse grounded from Update)
        float speed = movementSpeed;

        Vector3 desiredXZ = grounded
            ? speed * (moveInput.x * transform.right + moveInput.y * transform.forward)
            : Vector3.zero;

        rb.linearVelocity = new Vector3(desiredXZ.x, rb.linearVelocity.y, desiredXZ.z);

        // Optional clamp (XZ only)
        Vector3 flat = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float maxSqr = maxHorizontalSpeed * maxHorizontalSpeed;

        if (flat.sqrMagnitude > maxSqr)
        {
            Vector3 limited = flat.normalized * maxHorizontalSpeed;
            rb.linearVelocity = new Vector3(limited.x, rb.linearVelocity.y, limited.z);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!cap) cap = GetComponent<CapsuleCollider>();
        if (!cap) return;

        float halfHeight = Mathf.Max(cap.height * 0.5f, cap.radius);
        Vector3 feet = cap.transform.TransformPoint(cap.center)
                     + Vector3.down * (halfHeight - cap.radius + groundCheckOffset);

        Gizmos.DrawWireSphere(feet, groundCheckRadius);
    }
}
