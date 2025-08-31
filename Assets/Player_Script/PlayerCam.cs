using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCam : MonoBehaviour
{
    public Transform playerBody;       // the root/capsule that moves
    public float sensX = 0.12f;
    public float sensY = 0.12f;

    private PlayerControls controls;
    private float xRot; // pitch

    void Awake() { controls = new PlayerControls(); }
    void OnEnable() => controls.Player.Enable();
    void OnDisable() => controls.Player.Disable();

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        Vector2 look = controls.Player.Look.ReadValue<Vector2>();
        float mouseX = look.x * sensX;   // yaw
        float mouseY = look.y * sensY;   // pitch

        // yaw on the body (rotates movement direction)
        if (playerBody) playerBody.Rotate(0f, mouseX, 0f);

        // pitch on the camera (this)
        xRot = Mathf.Clamp(xRot - mouseY, -90f, 90f);
        transform.localRotation = Quaternion.Euler(xRot, 0f, 0f);
    }
}
