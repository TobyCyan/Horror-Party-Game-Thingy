
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCam : MonoBehaviour
{
    public Transform playerBody;    // drag your player root/capsule here
    public float sensX = 0.12f;
    public float sensY = 0.12f;

    private PlayerControls controls;
    private float xRot;
    private float yRot; 

    void Awake() { controls = new PlayerControls(); }
    void OnEnable() { controls.Player.Enable(); }
    void OnDisable() { controls.Player.Disable(); }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = true;
    }

    void Update()
    {
        Vector2 look = controls.Player.Look.ReadValue<Vector2>();
        float mouseX = look.x * sensX;   // YAW (left–right)
        float mouseY = look.y * sensY;   // PITCH (up–down)

        // pitch on camera (this)
        xRot -= mouseY;
        xRot = Mathf.Clamp(xRot, -90f, 90f);

        yRot += mouseX;
        transform.localRotation = Quaternion.Euler(xRot, yRot, 0f);

        // yaw on player root
        if (playerBody != null)
            playerBody.rotation = Quaternion.Euler(0f, yRot, 0f);
    }
}
