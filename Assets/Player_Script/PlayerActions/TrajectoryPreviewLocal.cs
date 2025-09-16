
using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

[RequireComponent(typeof(LineRenderer))]
public class TrajectoryPreviewLocal : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private LineRenderer line;
    [SerializeField] private Transform firePoint;
    [SerializeField] private Camera cam; // local player's camera
    [SerializeField] private PlayerCam playerCam; // Reference to PlayerCam for aim state

    [Header("Preview Params")]
    [SerializeField] private float step = 0.05f;
    [SerializeField] private float previewTime = 5f;
    [SerializeField] private float apexHeight = 2f; // chosen peak height
    [SerializeField] private float maxRange = 30f; // maximum shooting range

    [Header("Local Projectile (optional)")]
    [SerializeField] private GameObject projectilePrefab; // plain prefab, no NGO

    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundMask = -1; // What layers count as ground
    [SerializeField] private bool useGroundHeight = true; // Whether to account for ground height

    [Header("Fire Settings")]
    [SerializeField] private bool fireOnPress = true; // Fire when pressing (true) or releasing (false)

    private PlayerControls controls;
    private Vector3 launchPos;
    private Vector3 lastFireDirection;
    private float lastFireV0;
    private float lastFireAngle;
    private float lastFireTime;
    private bool canFire = false;
    private bool showWasPressed = false;

    void Awake()
    {
        controls = new PlayerControls();

        if (!line) line = GetComponent<LineRenderer>();
        if (!firePoint) firePoint = transform;

        // Get camera and PlayerCam references
        if (!cam)
        {
            cam = Camera.main;
            if (!cam) cam = FindObjectOfType<Camera>();
        }

        if (!playerCam && cam)
        {
            playerCam = cam.GetComponent<PlayerCam>();
        }

        line.startWidth = 0.08f;
        line.endWidth = 0.08f;
        line.useWorldSpace = true;
        line.enabled = false; // Start hidden
    }

    void OnEnable()
    {
        controls.Player.Enable();

        // Subscribe to Show action events
        controls.Player.Show.performed += OnShowPerformed;
        controls.Player.Show.canceled += OnShowCanceled;
    }

    void OnDisable()
    {
        controls.Player.Show.performed -= OnShowPerformed;
        controls.Player.Show.canceled -= OnShowCanceled;

        controls.Player.Disable();
    }

    void Update()
    {
        // Check if Show action is held
        bool showHeld = controls.Player.Show.IsPressed();

        // Detect press/release for firing
        bool showJustPressed = showHeld && !showWasPressed;
        bool showJustReleased = !showHeld && showWasPressed;

        // Show trajectory only while Show action is held
        line.enabled = showHeld;

        if (!line.enabled)
        {
            canFire = false;
            showWasPressed = showHeld;
            return;
        }

        // --- origin follows firePoint ---
        launchPos = firePoint.position;

        // Get target position based on camera forward and aim distance
        Vector3 targetWorld = GetCameraAlignedTarget();

        // Calculate relative position (target - origin)
        Vector3 targetRel = targetWorld - launchPos;

        // Use the solver for 3D trajectory
        float angleRad, v0, tHit;
        bool ok = CalculatePathWithHeight(targetRel, apexHeight, out angleRad, out v0, out tHit);

        if (ok)
        {
            // Store the calculated values for firing
            Vector3 horizontalDir = new Vector3(targetRel.x, 0f, targetRel.z).normalized;
            lastFireDirection = horizontalDir;
            lastFireV0 = v0;
            lastFireAngle = angleRad;
            lastFireTime = tHit;
            canFire = true;

            DrawPath3D(launchPos, horizontalDir, v0, angleRad, tHit, step);

            // Fire based on settings
            bool shouldFire = fireOnPress ? showJustPressed : showJustReleased;
            if (shouldFire && canFire)
            {
                FireLocal(launchPos, lastFireDirection, lastFireV0, lastFireAngle, lastFireTime);
            }
        }
        else
        {
            canFire = false;
            // Fallback preview - straight forward at 45 degrees
            Vector3 forward = cam.transform.forward;
            Vector3 horizontalForward = new Vector3(forward.x, 0f, forward.z).normalized;
            DrawPath3D(launchPos, horizontalForward, 10f, 45f * Mathf.Deg2Rad, previewTime, step);
        }

        // Update state for next frame
        showWasPressed = showHeld;
    }

    private void OnShowPerformed(InputAction.CallbackContext context)
    {
        Debug.Log("[TrajectoryPreview] Show action performed (pressed)");
    }

    private void OnShowCanceled(InputAction.CallbackContext context)
    {
        Debug.Log("[TrajectoryPreview] Show action canceled (released)");

        // If fire on release is enabled and we can fire, do it now
        if (!fireOnPress && canFire)
        {
            FireLocal(launchPos, lastFireDirection, lastFireV0, lastFireAngle, lastFireTime);
        }
    }

    private Vector3 GetCameraAlignedTarget()
    {
        // Get aim distance from PlayerCam (or use default)
        float aimDistance = (playerCam != null && playerCam.IsAiming)
            ? playerCam.AimDistance
            : maxRange * 0.5f;

        // Use camera forward direction
        Vector3 cameraForward = cam.transform.forward;

        // Calculate target point along camera forward
        Vector3 targetPoint = launchPos + cameraForward * aimDistance;

        // Optionally adjust for ground height
        if (useGroundHeight)
        {
            // Cast a ray downward from the target point to find ground
            Ray groundRay = new Ray(targetPoint + Vector3.up * 100f, Vector3.down);
            if (Physics.Raycast(groundRay, out RaycastHit hit, 200f, groundMask))
            {
                // Use the ground height at target location
                targetPoint.y = hit.point.y;
            }
            else
            {
                // If no ground found, cast from the calculated point
                Ray forwardRay = new Ray(launchPos, cameraForward);
                if (Physics.Raycast(forwardRay, out hit, aimDistance, groundMask))
                {
                    targetPoint = hit.point;
                }
            }
        }

        return targetPoint;
    }

    // ---------------- Local fire (no networking) ----------------
    private void FireLocal(Vector3 origin, Vector3 direction, float v0, float angleRad, float flightTime)
    {
        if (!projectilePrefab)
        {
            Debug.LogWarning("[TrajectoryPreview] No projectile prefab assigned!");
            return;
        }

        Debug.Log($"[TrajectoryPreview] Firing projectile - V0: {v0:F1}, Angle: {angleRad * Mathf.Rad2Deg:F1}°, Time: {flightTime:F2}s");

        var go = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(direction));

        // If it has physics, disable so we can animate kinematically
        if (go.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        if (go.TryGetComponent<Collider>(out var col)) col.enabled = false;

        StartCoroutine(SimulateLocal3D(go.transform, origin, direction, v0, angleRad, flightTime));
    }

    private IEnumerator SimulateLocal3D(Transform t, Vector3 origin, Vector3 direction, float v0, float angleRad, float flightTime)
    {
        float g = -Physics.gravity.y;
        float tt = 0f;

        while (t && tt < flightTime)
        {
            tt += Time.deltaTime;

            // Calculate position in 3D
            float horizontalDistance = v0 * tt * Mathf.Cos(angleRad);
            float verticalDistance = v0 * tt * Mathf.Sin(angleRad) - 0.5f * g * tt * tt;

            Vector3 horizontalOffset = direction * horizontalDistance;
            Vector3 verticalOffset = Vector3.up * verticalDistance;

            t.position = origin + horizontalOffset + verticalOffset;
            yield return null;
        }
        if (t) Destroy(t.gameObject);
    }

    // ---------------- 3D trajectory solver ----------------
    private bool CalculatePathWithHeight(Vector3 targetRel, float apexHeight, out float angleRad, out float v0, out float time)
    {
        // Get horizontal distance (ignore Y for horizontal calculation)
        float horizontalDistance = new Vector3(targetRel.x, 0f, targetRel.z).magnitude;
        float verticalDistance = targetRel.y;
        float g = -Physics.gravity.y;

        angleRad = 0f; v0 = 0f; time = 0f;

        if (horizontalDistance < 1e-4f) return false;      // avoid divide by zero
        if (apexHeight <= 0f) return false;

        float b = Mathf.Sqrt(2f * g * apexHeight);    // initial vertical speed

        // Solve -0.5 g t^2 + b t - yt = 0
        float a = -0.5f * g;
        float c = -verticalDistance;
        float disc = b * b - 4f * a * c;
        if (disc < 0f) return false;

        float sqrtD = Mathf.Sqrt(disc);
        float t1 = (-b + sqrtD) / (2f * a);
        float t2 = (-b - sqrtD) / (2f * a);

        // pick the positive, larger time (descending branch)
        float t = Mathf.Max(t1, t2);
        if (t <= 0f || !float.IsFinite(t)) return false;

        float tanTheta = (b * t) / horizontalDistance;
        angleRad = Mathf.Atan(tanTheta);

        float sin = Mathf.Sin(angleRad);
        if (Mathf.Abs(sin) < 1e-4f) return false;

        v0 = b / sin;
        time = t;
        return float.IsFinite(v0) && v0 > 0f;
    }

    // -------- 3D Trajectory drawing --------
    private void DrawPath3D(Vector3 origin, Vector3 direction, float v0, float angleRad, float totalTime, float dt)
    {
        if (!line) return;

        dt = Mathf.Max(0.01f, dt);
        int count = Mathf.CeilToInt(totalTime / dt) + 1;
        line.positionCount = count;

        float g = -Physics.gravity.y;

        for (int i = 0; i < count; i++)
        {
            float t = Mathf.Min(i * dt, totalTime);

            // Calculate 3D trajectory
            float horizontalDistance = v0 * t * Mathf.Cos(angleRad);
            float verticalDistance = v0 * t * Mathf.Sin(angleRad) - 0.5f * g * t * t;

            Vector3 horizontalOffset = direction * horizontalDistance;
            Vector3 verticalOffset = Vector3.up * verticalDistance;

            line.SetPosition(i, origin + horizontalOffset + verticalOffset);
        }
    }

    // Debug visualization
    void OnDrawGizmosSelected()
    {
        if (playerCam != null && playerCam.IsAiming && cam != null)
        {
            // Show aim direction and distance
            Gizmos.color = Color.yellow;
            Vector3 start = firePoint ? firePoint.position : transform.position;
            Vector3 end = start + cam.transform.forward * playerCam.AimDistance;
            Gizmos.DrawLine(start, end);
            Gizmos.DrawWireSphere(end, 0.5f);
        }
    }
}