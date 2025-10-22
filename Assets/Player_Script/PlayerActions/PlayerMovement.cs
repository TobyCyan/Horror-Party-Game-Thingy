using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerMovement : MonoBehaviour
{
    // --- Movement ---
    [Header("Movement")]
    [SerializeField] private float baseMovementSpeed = 6f;
    public float movementSpeed = 6f;
    [SerializeField] private float jumpForce = 5.5f; // anim-only

    // --- Look ---
    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 120f;

    // --- Grounding ---
    [Header("Grounding")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float groundCheckRadius = 0.3f;  // INCREASED from 0.2f
    [SerializeField] private float groundCheckOffset = 0.1f;  // INCREASED from 0.05f
    [SerializeField] private float coyoteTime = 0.15f;        // NEW: Grace period before falling
    [SerializeField] private float groundedBuffer = 0.1f;     // NEW: Time before considering airborne
    private CapsuleCollider cap;
    private float lastGroundedTime;                           // NEW: Track when last grounded
    private float airborneTime;                               // NEW: Track time in air

    // --- Optional Speed Clamp ---
    [Header("Optional Speed Clamp")]
    [SerializeField] private float maxHorizontalSpeed = 10f;

    // --- Crouch settings ---
    [Header("Crouch Settings")]
    [SerializeField] private float crouchHeight = 1.0f;
    [SerializeField] private float crouchCenterY = 0.5f;
    [SerializeField] private float crouchSpeedMultiplier = 0.55f;
    [SerializeField] private float stanceLerpSpeed = 12f;

    // --- Airborne stance ---
    [Header("Airborne Capsule")]
    [SerializeField] private float airHeightMultiplier = 1.0f;
    [SerializeField] private float airCenterYOffset = 0.0f;

    // --- Forces & Damping ---
    [Header("Forces & Damping")]
    [SerializeField] private float groundAcceleration = 45f;
    [SerializeField] private float airAcceleration = 15f;
    [SerializeField] private float groundDrag = 2f;
    [SerializeField] private float airDrag = 0.2f;

    // --- Blindness VFX ---
    [Header("Blind Effect")]
    [SerializeField] private GameObject blindEffect;

    // --- Internals ---
    [SerializeField] private Camera ownerCamera;

    private Rigidbody rb;
    private PlayerControls controls;
    private Animator anim;
    public Vector2 moveInput;
    private bool grounded;
    private bool prevGrounded;
    private bool isGroundedForAnimation;  // NEW: Buffered ground state for animation

    private bool isCrouching = false;
    private bool isBlinded = false;
    private float blindTimer = 0f;

    // stance targets
    private float originalHeight;
    private Vector3 originalCenter;
    private float targetHeight;
    private Vector3 targetCenter;

    // freeze
    private bool isStunned = false;
    private float stunTimer = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        anim = GetComponentInChildren<Animator>();
        cap = GetComponent<CapsuleCollider>();
        controls = new PlayerControls();

        originalHeight = cap.height;
        originalCenter = cap.center;
        targetHeight = originalHeight;
        targetCenter = originalCenter;

        if (blindEffect) blindEffect.SetActive(false);
    }

    void OnEnable()
    {
        controls.Player.Enable();
        rb.useGravity = true;
    }

    void OnDisable()
    {
        controls.Player.Disable();
        rb.useGravity = false;
    }

    private bool ComputeGrounded()
    {
        // Multiple ground checks for better reliability
        float halfHeight = Mathf.Max(cap.height * 0.5f, cap.radius);
        Vector3 feet = cap.transform.TransformPoint(cap.center)
                     + Vector3.down * (halfHeight - cap.radius + groundCheckOffset);

        // Primary sphere check
        bool sphereCheck = Physics.CheckSphere(feet, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore);

        // Additional raycast for more precision
        bool rayCheck = Physics.Raycast(transform.position, Vector3.down, halfHeight + groundCheckOffset * 2f, groundMask, QueryTriggerInteraction.Ignore);

        // Consider grounded if either check passes
        return sphereCheck || rayCheck;
    }

    private void SetCrouchState(bool crouching)
    {
        isCrouching = crouching;
        if (crouching)
        {
            targetHeight = crouchHeight;
            targetCenter = new Vector3(originalCenter.x, crouchCenterY, originalCenter.z);
        }
        else
        {
            targetHeight = originalHeight;
            targetCenter = originalCenter;
        }
    }

    private void SetAirborneCapsule()
    {
        targetHeight = originalHeight * airHeightMultiplier;
        targetCenter = new Vector3(originalCenter.x, originalCenter.y + airCenterYOffset, originalCenter.z);
    }

    void Update()
    {
        // Freeze timer
        if (isStunned)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f) Unfreeze();
            return;
        }

        // --- Blind test key ---
        if (controls.Player.Blind.triggered)
            Blind(5f);

        // Inputs
        moveInput = controls.Player.Move.ReadValue<Vector2>();

        // Crouch toggle
        if (controls.Player.Crawl.triggered)
        {
            bool next = !isCrouching;
            SetCrouchState(next);
            anim.SetBool("IsCrawling", next);
        }

        // Grounding with buffer
        prevGrounded = grounded;
        grounded = ComputeGrounded();

        // Update grounded timers
        if (grounded)
        {
            lastGroundedTime = Time.time;
            airborneTime = 0f;
        }
        else
        {
            airborneTime += Time.deltaTime;
        }

        // Use buffered ground state for animation (prevents flickering)
        if (grounded)
        {
            isGroundedForAnimation = true;
        }
        else if (airborneTime > groundedBuffer)  // Only consider airborne after buffer time
        {
            isGroundedForAnimation = false;
        }

        // Stance selection
        if (!isGroundedForAnimation) SetAirborneCapsule();
        else SetCrouchState(isCrouching);

        // Smoothly apply collider stance
        cap.height = Mathf.Lerp(cap.height, targetHeight, Time.deltaTime * stanceLerpSpeed);
        cap.center = Vector3.Lerp(cap.center, targetCenter, Time.deltaTime * stanceLerpSpeed);

        // Jump (animation trigger only) - use coyote time
        bool canJump = grounded || (Time.time - lastGroundedTime < coyoteTime);
        if (canJump && !isCrouching && controls.Player.Jump.triggered)
            anim.SetTrigger("Jump");

        // Animator params with buffered ground state
        if (anim)
        {
            Vector3 lv = rb.linearVelocity;
            float horizontalMag = new Vector3(lv.x, 0f, lv.z).magnitude;
            bool isWalking = isGroundedForAnimation && (moveInput.sqrMagnitude > 0.01f) && horizontalMag > 0.01f;

            anim.SetBool("IsGrounded", isGroundedForAnimation);  // Use buffered state
            anim.SetBool("IsWalking", isWalking);
            anim.SetFloat("VerticalSpeed", lv.y);

            if (isGroundedForAnimation && (moveInput.x != 0f || moveInput.y != 0f))
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

        // Blind timer
        if (isBlinded)
        {
            blindTimer -= Time.deltaTime;
            if (blindTimer <= 0f) Unblind();
        }
    }

    void FixedUpdate()
    {
        if (isStunned)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        rb.linearDamping = grounded ? groundDrag : airDrag;

        Vector3 wish = (moveInput.x * transform.right + moveInput.y * transform.forward);
        if (wish.sqrMagnitude > 1f) wish.Normalize();

        float topSpeed = movementSpeed * (isCrouching ? crouchSpeedMultiplier : 1f);
        Vector3 v = rb.linearVelocity;
        Vector3 vXZ = new Vector3(v.x, 0f, v.z);

        float speedAlong = (wish.sqrMagnitude > 0f) ? Vector3.Dot(vXZ, wish) : 0f;
        float maxAccel = grounded ? 25f : 10f;
        float wantDelta = Mathf.Clamp(topSpeed - speedAlong, 0f, maxAccel * Time.fixedDeltaTime);

        // accelerate toward target speed along input direction
        if (wish.sqrMagnitude > 0f && wantDelta > 0f)
        {
            Vector3 acc = wish * (wantDelta / Time.fixedDeltaTime);
            rb.AddForce(acc, ForceMode.Acceleration);
        }

        // gentle braking when no input
        if (wish.sqrMagnitude == 0f && vXZ.sqrMagnitude > 0.0001f)
        {
            Vector3 brake = -vXZ.normalized * maxAccel;
            rb.AddForce(brake, ForceMode.Acceleration);

            // prevent overshoot reversing direction
            Vector3 newXZ = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            if (Vector3.Dot(newXZ, vXZ) < 0f)
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }
    }

    public void FreezeInPlace()
    {
        rb.linearVelocity = Vector3.zero;
        anim.SetBool("IsWalking", false);
        anim.SetFloat("MoveX", 0f);
        anim.SetFloat("MoveZ", 0f);
    }

    public void ResetMovementSpeed()
    {
        movementSpeed = baseMovementSpeed;
    }

    public void SetMovementSpeedByModifier(float modifier)
    {
        movementSpeed *= modifier;
    }

    void OnDrawGizmosSelected()
    {
        if (!cap) cap = GetComponent<CapsuleCollider>();
        if (!cap) return;

        float halfHeight = Mathf.Max(cap.height * 0.5f, cap.radius);
        Vector3 feet = cap.transform.TransformPoint(cap.center)
                     + Vector3.down * (halfHeight - cap.radius + groundCheckOffset);

        // Show the ground check sphere
        Gizmos.color = grounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(feet, groundCheckRadius);

        // Show additional raycast
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, Vector3.down * (halfHeight + groundCheckOffset * 2f));
    }

    // ----------------- Blind API -----------------
    public void Blind(float duration)
    {
        Debug.Log($"BLIND - Starting blind effect for {duration} seconds");

        if (blindEffect == null)
        {
            Debug.LogWarning("[Blind] No blindEffect assigned.");
            return;
        }

        isBlinded = true;
        blindTimer = Mathf.Max(0f, duration);
        blindEffect.SetActive(true);

        int localBarrierLayer = LayerMask.NameToLayer("LocalBarrier");
        if (localBarrierLayer == -1)
        {
            Debug.LogError("[Blind] LocalBarrier layer does not exist! Add it in Project Settings > Tags and Layers");
            return;
        }

        if (ownerCamera == null)
        {
            ownerCamera = GetComponentInChildren<Camera>();
            if (ownerCamera == null) ownerCamera = Camera.main;

            if (ownerCamera == null)
            {
                Debug.LogError("[Blind] No camera found to modify culling mask!");
                return;
            }
        }

        int layerBit = 1 << localBarrierLayer;
        ownerCamera.cullingMask |= layerBit;

        Debug.Log($"[PlayerMovement] Blinded - Added LocalBarrier layer to camera. New mask: {ownerCamera.cullingMask}");
    }

    private void Unblind()
    {
        isBlinded = false;
        blindTimer = 0f;

        if (blindEffect) blindEffect.SetActive(false);

        int localBarrierLayer = LayerMask.NameToLayer("LocalBarrier");
        if (localBarrierLayer != -1 && ownerCamera != null)
        {
            int layerBit = 1 << localBarrierLayer;
            ownerCamera.cullingMask &= ~layerBit;

            Debug.Log($"[PlayerMovement] Unblinded - Removed LocalBarrier layer from camera. New mask: {ownerCamera.cullingMask}");
        }
    }

    // ----------------- Stun API -----------------
    public void Stun(float duration)
    {
        isStunned = true;
        stunTimer = Mathf.Max(0f, duration);

        FreezeInPlace();
        Debug.Log($"[PlayerMovement] Frozen for {duration:0.00}s");
    }

    private void Unfreeze()
    {
        isStunned = false;
        stunTimer = 0f;
        Debug.Log("[PlayerMovement] Unfrozen");
    }
}