using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerMovement : MonoBehaviour
{
    // --- Movement ---
    [Header("Movement")]
    [SerializeField] private float movementSpeed = 6f;
    [SerializeField] private float jumpForce = 5.5f; // anim-only

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
    [SerializeField] private float groundDrag = 2f;   // use Rigidbody.drag
    [SerializeField] private float airDrag = 0.2f;    // use Rigidbody.drag

    // --- Blindness VFX ---
    [Header("Blind Effect")]
    [SerializeField] private GameObject blindEffect;
    [SerializeField] private float defaultBlindDuration = 10f;

    // --- Internals ---
    [SerializeField] private Camera ownerCamera;

    [SerializeField] private string localBarrierLayerName = "LocalBarrier";
    private int barrierBit = 0;
    private int savedMask = 0;
    private bool barrierApplied = false;


    private Rigidbody rb;
    private PlayerControls controls;
    private Animator anim;
    private Vector2 moveInput;
    private bool grounded;
    private bool prevGrounded;

    private bool isCrouching = false;
    private bool isBlinded = false;
    private float blindTimer = 0f;

    // stance targets
    private float originalHeight;
    private Vector3 originalCenter;
    private float targetHeight;
    private Vector3 targetCenter;

    // freeze
    private bool isFrozen = false;
    private float freezeTimer = 0f;

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
        int id = LayerMask.NameToLayer(localBarrierLayerName);
        barrierBit = (id >= 0) ? (1 << id) : 0;

        if (blindEffect) blindEffect.SetActive(false);
        if (!ownerCamera)
        {
            // Prefer the camera tagged "MainCamera"
            ownerCamera = Camera.main;

            // Fallback: grab any enabled Camera in the scene
            if (!ownerCamera)
            {
                var cams = FindObjectsOfType<Camera>(true); // include inactive just in case
                foreach (var cam in cams)
                {
                    if (cam.isActiveAndEnabled) { ownerCamera = cam; break; }
                }
            }

            if (!ownerCamera)
                Debug.LogWarning("[PlayerMovement] No Camera found. Assign 'ownerCamera' in the Inspector or tag your camera as MainCamera.");
        }
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
        if (isFrozen)
        {
            freezeTimer -= Time.deltaTime;
            if (freezeTimer <= 0f) Unfreeze();
            return; // skip inputs/anim while frozen
        }

        // --- Blind test key ---
        if (controls.Player.Blind.triggered)
            Blind(defaultBlindDuration);

        // --- Freeze test key ---
        if (controls.Player.Freeze != null && controls.Player.Freeze.triggered)
            Freeze(5f);

        // Blind test key
        if (controls.Player.Blind.triggered)
            Blind(defaultBlindDuration);

        // Inputs
        moveInput = controls.Player.Move.ReadValue<Vector2>();

        // Crouch toggle
        if (controls.Player.Crawl.triggered)
        {
            bool next = !isCrouching;
            SetCrouchState(next);
            anim?.SetBool("IsCrawling", next);
        }

        // Grounding
        prevGrounded = grounded;
        grounded = ComputeGrounded();

        // Stance selection
        if (!grounded) SetAirborneCapsule();
        else SetCrouchState(isCrouching);

        // Smoothly apply collider stance
        cap.height = Mathf.Lerp(cap.height, targetHeight, Time.deltaTime * stanceLerpSpeed);
        cap.center = Vector3.Lerp(cap.center, targetCenter, Time.deltaTime * stanceLerpSpeed);

        // Jump (animation trigger only)
        if (grounded && !isCrouching && controls.Player.Jump.triggered)
            anim?.SetTrigger("Jump");

        // Animator params
        if (anim)
        {
            Vector3 lv = rb.linearVelocity; // 
            float horizontalMag = new Vector3(lv.x, 0f, lv.z).magnitude;
            bool isWalking = grounded && (moveInput.sqrMagnitude > 0.01f) && horizontalMag > 0.01f;

            anim.SetBool("IsGrounded", grounded);
            anim.SetBool("IsWalking", isWalking);
            anim.SetFloat("VerticalSpeed", lv.y);

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

        // Blind timer
        if (isBlinded)
        {
            blindTimer -= Time.deltaTime;
            if (blindTimer <= 0f) Unblind();
        }
    }

    void FixedUpdate()
    {
        if (isFrozen)
        {
            rb.linearVelocity = Vector3.zero; //  pin every physics step
            return;
        }

        rb.linearDamping = grounded ? groundDrag : airDrag; // 

        Vector3 wish = (moveInput.x * transform.right + moveInput.y * transform.forward);
        if (wish.sqrMagnitude > 1f) wish.Normalize();

        float topSpeed = movementSpeed * (isCrouching ? crouchSpeedMultiplier : 1f);
        Vector3 v = rb.linearVelocity; // 
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
            Vector3 newXZ = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z); // 
            if (Vector3.Dot(newXZ, vXZ) < 0f)
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f); // 
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
    public void Blind(float duration)
    {
        if (blindEffect == null) { Debug.LogWarning("[Blind] No blindEffect assigned."); return; }
        //isBlinded = true;
        //blindTimer = Mathf.Max(0f, duration);
        //blindEffect.SetActive(true);

        //int localBarrierLayer = LayerMask.NameToLayer("LocalBarrier");
        //if (ownerCamera && localBarrierLayer != -1)
        //    ownerCamera.cullingMask |= (1 << localBarrierLayer);
        isBlinded = true;
        blindTimer = Mathf.Max(0f, duration);
        if (blindEffect) blindEffect.SetActive(true);

        if (ownerCamera && barrierBit != 0)
        {
            if (!barrierApplied)           // snapshot once
            {
                savedMask = ownerCamera.cullingMask;
                barrierApplied = true;
            }
            ownerCamera.cullingMask = savedMask | barrierBit; // ensure ON
        }
    }

    public void Unblind()
    {
        //isBlinded = false;
        //blindTimer = 0f;
        //if (blindEffect) blindEffect.SetActive(false);

        //int localBarrierLayer = LayerMask.NameToLayer("LocalBarrier");
        //if (ownerCamera && localBarrierLayer != -1)
        //    ownerCamera.cullingMask &= ~(1 << localBarrierLayer);
        isBlinded = false;
        blindTimer = 0f;
        if (blindEffect) blindEffect.SetActive(false);

        if (ownerCamera && barrierApplied)
        {
            ownerCamera.cullingMask = savedMask;  // exact restore
            barrierApplied = false;
        }
    }

    // ----------------- Freeze API -----------------
    /// <summary>Freeze the player's movement for 'duration' seconds.</summary>
    public void Freeze(float duration)
    {
        isFrozen = true;
        freezeTimer = Mathf.Max(0f, duration);

        rb.linearVelocity = Vector3.zero; //  immediate stop
        anim?.SetBool("IsWalking", false);
        anim?.SetFloat("MoveX", 0f);
        anim?.SetFloat("MoveZ", 0f);
        Debug.Log($"[PlayerMovement] Frozen for {duration:0.00}s");
    }

    /// <summary>Unfreeze the player immediately.</summary>
    public void Unfreeze()
    {
        isFrozen = false;
        freezeTimer = 0f;
        Debug.Log("[PlayerMovement] Unfrozen");
    }

    public void OnBlind()
    {
        Blind(defaultBlindDuration);
    }

    private void OnCollisionEnter(Collision other)
    {
        NetworkObject nObj = other.gameObject.GetComponent<NetworkObject>();
        if (nObj)
        {
            ChangeOwnerServerRpc(other.gameObject.GetComponent<NetworkObject>());
        }
    }

    // Give Last touch player authority to move it
    [Rpc(SendTo.Server)]
    void ChangeOwnerServerRpc(NetworkObject other, RpcParams rpcParams = default)
    {
        other.ChangeOwnership(rpcParams.Receive.SenderClientId);
    }
}