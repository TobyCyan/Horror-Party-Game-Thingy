using UnityEngine;
using UnityEngine.InputSystem;

public class MouseRaycaster : MonoBehaviour
{
    [Header("Camera / Ray")]
    [SerializeField] private Camera cam;
    [SerializeField] private float maxDistance = 3f;
    [SerializeField] private float sphereRadius = 0.05f;
    [SerializeField] private LayerMask mask = ~0;

    [Header("Debug")]
    [SerializeField] private bool debugDrawRay = true;
    [SerializeField] private bool logEveryFrame = false; // set true if you want spammy logs each frame

    private PlayerControls controls;
    private IInteractable current;
    private string lastHitName = null;

    void Awake()
    {
        controls = new PlayerControls();
        if (!cam) cam = Camera.main;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    void OnEnable() => controls.Player.Enable();
    void OnDisable() => controls.Player.Disable();

    void Update()
    {
        if (!cam) return;

        // Fallback if Mouse is null (eg. on controller-only devices)
        Vector2 mousePos = (Mouse.current != null)
            ? Mouse.current.position.ReadValue()
            : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        Ray ray = cam.ScreenPointToRay(mousePos);
        if (debugDrawRay) Debug.DrawRay(ray.origin, ray.direction * maxDistance, Color.yellow, 0f);

        IInteractable found = null;
        RaycastHit hit;
        bool hitOk = sphereRadius > 0f
            ? Physics.SphereCast(ray, sphereRadius, out hit, maxDistance, mask, QueryTriggerInteraction.Ignore)
            : Physics.Raycast(ray, out hit, maxDistance, mask, QueryTriggerInteraction.Ignore);

        if (hitOk)
        {
            found = hit.collider.GetComponentInParent<IInteractable>();

            // Log changes (only when target changes unless logEveryFrame)
            if (logEveryFrame || hit.collider.name != lastHitName)
            {
                string msg =
                    $"[MouseRaycaster] Hit: '{hit.collider.name}' dist={hit.distance:0.00} " +
                    $"layer={LayerMask.LayerToName(hit.collider.gameObject.layer)} " +
                    $"interactable={(found != null ? found.GetType().Name : "None")}";
                //Debug.Log(msg);
                lastHitName = hit.collider.name;
            }
        }
        else
        {
            if (lastHitName != null)
            {
                //Debug.Log("[MouseRaycaster] No hit.");
                lastHitName = null;
            }
            else if (logEveryFrame)
            {
                //Debug.Log("[MouseRaycaster] No hit (frame).");
            }
        }

        current = found;

        // Interact only via PlayerControls
        bool interactPressed = controls.Player.Interact != null && controls.Player.Interact.triggered;
        if (interactPressed)
        {
            if (current == null)
            {
                //Debug.Log("[MouseRaycaster] Interact pressed but no IInteractable under cursor.");
                return;
            }

            var ctx = new InteractionContext(user: transform, camera: cam, hit: hit);
            bool can = current.CanInteract(ctx);
            //Debug.Log($"[MouseRaycaster] Interact pressed on '{hit.collider.name}'. CanInteract={can}");

            if (can)
            {
                current.Interact(ctx);
                //Debug.Log($"[MouseRaycaster] Interact() CALLED on {current.GetType().Name} ({hit.collider.name}).");
            }
            else
            {
                //Debug.Log("[MouseRaycaster] CanInteract returned false; interaction blocked.");
            }
        }
    }
}
