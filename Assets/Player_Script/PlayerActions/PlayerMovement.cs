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

    // --- Crouch settings ---
    [Header("Crouch Settings")]
    [SerializeField] private float crouchHeight = 1.0f;
    [SerializeField] private float crouchCenterY = 0.5f; // tweak per model
    private float originalHeight;
    private Vector3 originalCenter;

    // --- Blindness VFX (assign in Inspector) ---
    [Header("Blind Effect")]
    [SerializeField] private GameObject blindEffect;     // e.g., a sphere/dome on a layer above player
    [SerializeField] private float defaultBlindDuration = 10f;

    // --- Internals ---
    [SerializeField] private Camera ownerCamera; // assign in Inspector
    private Rigidbody rb;
    private PlayerControls controls;
    private Animator anim;
    private Vector2 moveInput;
    private bool grounded;
    private bool prevGrounded;
    private float prevVerticalSpeed;

    private bool isCrouching = false;
    private bool isBlinded = false;
    private float blindTimer = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        anim = GetComponentInChildren<Animator>();
        cap = GetComponent<CapsuleCollider>();
        controls = new PlayerControls();

        originalHeight = cap.height;
        originalCenter = cap.center;

        // ensure blind VFX starts disabled
        if (blindEffect) blindEffect.SetActive(false);
    }

    void OnEnable() => controls.Player.Enable();
    void OnDisable() => controls.Player.Disable();

    private bool ComputeGrounded()
    {
        float halfHeight = Mathf.Max(cap.height * 0.5f, cap.radius);
        Vector3 feet = cap.transform.TransformPoint(cap.center)
                     + Vector3.down * (halfHeight - cap.radius + groundCheckOffset);

        return Physics.CheckSphere(feet, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore);
    }

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
        // --- TEMP test key to trigger blind (press 'B') ---
        if (controls.Player.Blind.triggered)
            Blind(defaultBlindDuration);
        // -----------------------------------------------

        // Inputs
        moveInput = controls.Player.Move.ReadValue<Vector2>();

        // Crouch toggle
        if (controls.Player.Crawl.triggered)
        {
            isCrouching = !isCrouching;
            anim?.SetBool("IsCrawling", isCrouching);
            SetCrouchState(isCrouching);
        }

        // Look (yaw)
        Vector2 lookDelta = controls.Player.Look != null
            ? controls.Player.Look.ReadValue<Vector2>()
            : (Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero);
        transform.Rotate(0f, lookDelta.x * mouseSensitivity * Time.deltaTime, 0f);

        // Grounding
        prevGrounded = grounded;
        grounded = ComputeGrounded();

        // Jump
        if (grounded && !isCrouching && controls.Player.Jump.triggered)
            anim?.SetTrigger("Jump");

        // Animator params
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

        // Blind timer countdown
        if (isBlinded)
        {
            blindTimer -= Time.deltaTime;
            if (blindTimer <= 0f) Unblind();
        }

        prevVerticalSpeed = rb.linearVelocity.y;
    }

    void FixedUpdate()
    {
        float speed = movementSpeed;

        Vector3 desiredXZ = grounded
            ? speed * (moveInput.x * transform.right + moveInput.y * transform.forward)
            : Vector3.zero;

        rb.linearVelocity = new Vector3(desiredXZ.x, rb.linearVelocity.y, desiredXZ.z);

        // Optional speed clamp
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

    // ----------------- Blind API -----------------
    /// <summary>Trigger the blind effect for 'duration' seconds.</summary>
    public void Blind(float duration)
    {
        if (blindEffect == null) { Debug.LogWarning("[Blind] No blindEffect assigned."); return; }
        isBlinded = true;
        blindTimer = Mathf.Max(0f, duration);
        blindEffect.SetActive(true);
        int localBarrierLayer = LayerMask.NameToLayer("LocalBarrier");
        ownerCamera.cullingMask |= (1 << localBarrierLayer);

    }

    /// <summary>Stop the blind effect immediately.</summary>
    public void Unblind()
    {
        isBlinded = false;
        blindTimer = 0f;
        if (blindEffect) blindEffect.SetActive(false);
        int localBarrierLayer = LayerMask.NameToLayer("LocalBarrier");
        if (localBarrierLayer != -1)
            ownerCamera.cullingMask &= ~(1 << localBarrierLayer);

    }

    /// <summary>Convenience hook if you want to call from input/event.</summary>
    private void OnBlind()
    {
        Blind(defaultBlindDuration);
    }


    // ---------------------------------------------
}
