using UnityEngine;
using UnityEngine.InputSystem;

public class CenterRaycaster : MonoBehaviour
{
    [Header("Camera / Ray")]
    [SerializeField] private Camera cam;
    [SerializeField] private float maxDistance = 3f;
    [SerializeField] private float sphereRadius = 0.05f;
    [SerializeField] private LayerMask mask = ~0;

    [Header("Crosshair (SpriteRenderer)")]
    [SerializeField] private SpriteRenderer crosshairRenderer; // worldspace/screen overlay object
    [SerializeField] private Sprite normalSprite;              // drag your PNG sprite asset
    [SerializeField] private Sprite interactSprite;            // sprite shown briefly on interact
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color interactColor = Color.cyan;
    [SerializeField] private float flashTime = 0.08f;

    private PlayerControls controls;
    private IInteractable current;
    private float flashTimer;

    void Awake()
    {
        controls = new PlayerControls();
        if (!cam) cam = Camera.main;

        if (crosshairRenderer)
        {
            if (normalSprite) crosshairRenderer.sprite = normalSprite;
            crosshairRenderer.color = normalColor;
        }
    }

    void OnEnable() => controls.Player.Enable();
    void OnDisable() => controls.Player.Disable();

    void Update()
    {
        if (!cam) return;

        // center ray
        Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f));
        RaycastHit hit;
        bool hitOk = sphereRadius > 0f
            ? Physics.SphereCast(ray, sphereRadius, out hit, maxDistance, mask, QueryTriggerInteraction.Ignore)
            : Physics.Raycast(ray, out hit, maxDistance, mask, QueryTriggerInteraction.Ignore);

        current = hitOk ? hit.collider.GetComponentInParent<IInteractable>() : null;

        // trigger only on Interact action
        bool interactPressed = controls.Player.Interact != null && controls.Player.Interact.triggered;
        if (interactPressed && current != null)
        {
            var ctx = new InteractionContext(user: transform, camera: cam, hit: hit);
            if (current.CanInteract(ctx))
            {
                current.Interact(ctx);
                flashTimer = flashTime;

                if (crosshairRenderer)
                {
                    if (interactSprite) crosshairRenderer.sprite = interactSprite;
                    crosshairRenderer.color = interactColor;
                }
            }
        }

        // reset after flash
        if (crosshairRenderer)
        {
            if (flashTimer > 0f)
            {
                flashTimer -= Time.deltaTime;
                if (flashTimer <= 0f)
                {
                    if (normalSprite) crosshairRenderer.sprite = normalSprite;
                    crosshairRenderer.color = normalColor;
                }
            }
            else
            {
                if (normalSprite && crosshairRenderer.sprite != normalSprite)
                    crosshairRenderer.sprite = normalSprite;
                if (crosshairRenderer.color != normalColor)
                    crosshairRenderer.color = normalColor;
            }
        }
    }
}
