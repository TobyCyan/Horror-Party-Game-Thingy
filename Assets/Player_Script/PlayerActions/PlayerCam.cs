using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCam : MonoBehaviour
{
    [Header("Refs")]
    public Transform playerBody;                 // root that moves (yaw applied here)
    [SerializeField] private CapsuleCollider playerCapsule; // assign your player's CapsuleCollider

    [Header("Look")]
    public float sensX = 0.12f;  // Input System delta is per-frame pixels; usually no deltaTime
    public float sensY = 0.12f;

    [Header("Eye Height Follow (optional)")]
    [SerializeField] private float eyeOffsetFromTop = 0.08f; // small margin below capsule top
    [SerializeField] private float eyeLerpSpeed = 12f;       // smoothing for vertical follow

    [Header("Aim Mode")]
    [SerializeField] private KeyCode aimKey = KeyCode.Q;
    [SerializeField] private float aimDistanceMin = 5f;
    [SerializeField] private float aimDistanceMax = 30f;
    [SerializeField] private float aimDistanceSensitivity = 0.5f;

    private PlayerControls controls;
    private float yaw;   // around Y on body
    private float pitch; // around X on camera

    // Aim mode state
    private bool isAiming = false;
    private float frozenPitch = 0f;
    private float aimDistance = 15f; // Current aim distance

    // Public properties for trajectory system
    public bool IsAiming => isAiming;
    public float AimDistance => aimDistance;
    public Vector3 AimDirection => transform.forward;

    void Awake()
    {
        controls = new PlayerControls();
        // If not assigned in Inspector, try to find a capsule on the body
        if (!playerCapsule && playerBody)
            playerCapsule = playerBody.GetComponent<CapsuleCollider>();
    }

    void OnEnable() => controls.Player.Enable();
    void OnDisable() => controls.Player.Disable();

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (playerBody) yaw = playerBody.eulerAngles.y;
        pitch = transform.localEulerAngles.x;
        if (pitch > 180f) pitch -= 360f; // convert to [-180,180]

        // Initialize aim distance to middle of range
        aimDistance = (aimDistanceMin + aimDistanceMax) * 0.5f;
    }

    void LateUpdate()
    {
        // Check aim mode toggle
        bool aimKeyHeld = Input.GetKey(aimKey);

        if (aimKeyHeld && !isAiming)
        {
            // Enter aim mode
            EnterAimMode();
        }
        else if (!aimKeyHeld && isAiming)
        {
            // Exit aim mode
            ExitAimMode();
        }

        Vector2 look = controls.Player.Look.ReadValue<Vector2>();

        // Always apply yaw (horizontal rotation)
        yaw += look.x * sensX;

        if (isAiming)
        {
            // In aim mode: use mouse Y to control aim distance instead of pitch
            aimDistance -= look.y * aimDistanceSensitivity;
            aimDistance = Mathf.Clamp(aimDistance, aimDistanceMin, aimDistanceMax);

            // Keep pitch frozen
            pitch = frozenPitch;
        }
        else
        {
            // Normal mode: apply pitch as usual
            pitch -= look.y * sensY;
            pitch = Mathf.Clamp(pitch, -90f, 90f);
        }

        // Apply rotations
        if (playerBody) playerBody.rotation = Quaternion.Euler(0f, yaw, 0f);
        transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        // --- Follow capsule top so camera matches crouch/air changes ---
        if (playerCapsule)
        {
            // Top of capsule in *local* space of the player root
            float topLocalY = playerCapsule.center.y + (playerCapsule.height * 0.5f);
            float targetY = topLocalY - eyeOffsetFromTop;
            // Our transform is expected to be a child of playerBody; adjust local Y only
            Vector3 lp = transform.localPosition;
            lp.y = Mathf.Lerp(lp.y, targetY, Time.deltaTime * eyeLerpSpeed);
            transform.localPosition = lp;
        }
    }

    private void EnterAimMode()
    {
        isAiming = true;
        frozenPitch = pitch; // Freeze current pitch

        // Optional: Change cursor state for better aiming experience
        // Cursor.lockState = CursorLockMode.Confined;

        Debug.Log($"[PlayerCam] Entered aim mode. Pitch frozen at {frozenPitch:F1}°");
    }

    private void ExitAimMode()
    {
        isAiming = false;
        // Pitch will resume normal control in the next frame

        // Optional: Restore cursor state
        // Cursor.lockState = CursorLockMode.Locked;

        Debug.Log("[PlayerCam] Exited aim mode. Pitch control restored.");
    }

    // Helper method for debugging
    void OnGUI()
    {
        if (isAiming)
        {
            GUI.Label(new Rect(10, 10, 300, 20), $"AIM MODE - Distance: {aimDistance:F1}m");
            GUI.Label(new Rect(10, 30, 300, 20), $"Pitch (frozen): {frozenPitch:F1}°");
        }
    }
}