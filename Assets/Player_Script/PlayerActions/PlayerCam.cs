using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCam : MonoBehaviour
{
    [Header("Refs")]
    public Transform playerBody;                 // root that moves (yaw applied here)
    [SerializeField] private CapsuleCollider playerCapsule; // assign your player’s CapsuleCollider

    [Header("Look")]
    public float sensX = 0.12f;  // Input System delta is per-frame pixels; usually no deltaTime
    public float sensY = 0.12f;

    [Header("Eye Height Follow (optional)")]
    [SerializeField] private float eyeOffsetFromTop = 0.08f; // small margin below capsule top
    [SerializeField] private float eyeLerpSpeed = 12f;       // smoothing for vertical follow

    private PlayerControls controls;
    private float yaw;   // around Y on body
    private float pitch; // around X on camera

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
        //Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;

        if (playerBody) yaw = playerBody.eulerAngles.y;

        pitch = transform.localEulerAngles.x;
        if (pitch > 180f) pitch -= 360f; // convert to [-180,180]
    }

    void LateUpdate()
    {
        Vector2 look = controls.Player.Look.ReadValue<Vector2>();

        // Yaw/Pitch
        yaw += look.x * sensX;
        pitch -= look.y * sensY;
        pitch = Mathf.Clamp(pitch, -90f, 90f);

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
}
