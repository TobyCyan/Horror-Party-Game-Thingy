using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Handles trap/item placement with visual feedback
/// Hold E to enter placement mode, Left-click to place
/// </summary>
public class TrapPlacer : NetworkBehaviour
{
    [Header("Placement Settings")]
    [SerializeField] private float maxPlacementDistance = 10f;
    [SerializeField] private KeyCode placeKey = KeyCode.Mouse0;
    [SerializeField] private KeyCode placementModeKey = KeyCode.E;
    [SerializeField] private LayerMask placementLayer = -1;
    [SerializeField] private float placementCooldown = 0.3f;
    [SerializeField] private float spawnHeightOffset = 0.5f;

    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerInventory inventory;

    [Header("Debug Settings")]
    [SerializeField] private bool showDebugVisuals = true;
    [SerializeField] private GameObject debugCubePrefab; // Optional: assign a cube prefab for testing
    [SerializeField] private float debugCubeAlpha = 0.15f; 

    private LocalCrosshairUI crosshairUI;

 

    private bool placementModeActive = false;
    private bool canPlaceHere = false;
    private Vector3 placementPosition;
    private Quaternion placementRotation;
    private float lastPlacementTime;
    private GameObject debugCube; // For visualizing placement position

    private void Start()
    {
        if (!IsOwner) return;

        // Find camera more reliably
        if (playerCamera == null)
        {
            // Try to find camera in children first (common for FPS setups)
            playerCamera = GetComponentInChildren<Camera>();

            if (playerCamera == null)
            {
                // Fall back to main camera
                playerCamera = Camera.main;
            }

            if (playerCamera == null)
            {
                Debug.LogError("[TrapPlacer] No camera found! Please assign in inspector.");
                enabled = false;
                return;
            }
        }

        // Verify camera is actually rendering
        if (!playerCamera.enabled)
        {
            Debug.LogWarning("[TrapPlacer] Assigned camera is disabled!");
        }

        if (inventory == null)
        {
            inventory = GetComponent<PlayerInventory>();
        }

        if (crosshairUI == null)
        {
            crosshairUI = GetComponent<LocalCrosshairUI>();
        }

        if (crosshairUI != null)
        {
            crosshairUI.SetCrosshairVisible(false);
        }

        // Create debug cube if needed
        if (showDebugVisuals && debugCubePrefab == null)
        {
            debugCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            debugCube.transform.localScale = Vector3.one * 0.4f;
            debugCube.GetComponent<Collider>().enabled = false;

            // Make material transparent
            var renderer = debugCube.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Standard"));
            mat.SetFloat("_Mode", 3); // Transparent mode
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            mat.color = new Color(0f, 1f, 0f, debugCubeAlpha); // Green with high transparency
            renderer.material = mat;

            debugCube.SetActive(false);
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        // CHANGED: Check for items BEFORE allowing placement mode
        bool hasItems = inventory != null && inventory.GetItemCount() > 0;

        if (Input.GetKey(placementModeKey) && hasItems)
        {
            if (!placementModeActive)
            {
                EnterPlacementMode();
            }
            UpdatePlacementMode();
        }
        else if (placementModeActive)
        {
            ExitPlacementMode();
        }
    }

    private void EnterPlacementMode()
    {
        // CHANGED: Double-check inventory before entering placement mode
        if (inventory == null || inventory.GetItemCount() == 0)
        {
            Debug.Log("[TrapPlacer] Cannot enter placement mode - no items in inventory");
            return;
        }

        placementModeActive = true;

        if (crosshairUI != null)
        {
            crosshairUI.SetCrosshairVisible(true);
        }

        Debug.Log("[TrapPlacer] Entered placement mode");
    }

    private void ExitPlacementMode()
    {
        placementModeActive = false;

        if (crosshairUI != null)
        {
            crosshairUI.SetCrosshairVisible(false);
        }

        // CHANGED: Ensure debug cube is hidden immediately
        if (debugCube != null)
        {
            debugCube.SetActive(false);
        }

        Debug.Log("[TrapPlacer] Exited placement mode");
    }

    private void UpdatePlacementMode()
    {
        CheckPlacementPosition();
        UpdateCrosshairFeedback();
        UpdateDebugVisuals();

        if (Input.GetKeyDown(placeKey))
        {
            // CHANGED: Additional check for inventory before placing
            if (inventory == null || inventory.GetItemCount() == 0)
            {
                Debug.LogWarning("[TrapPlacer] Cannot place - no items in inventory!");
                ExitPlacementMode();
                return;
            }

            if (canPlaceHere)
            {
                TryPlaceItem();
            }
            else
            {
                Debug.LogWarning("[TrapPlacer] Cannot place here - invalid location!");
            }
        }
    }

    private void CheckPlacementPosition()
    {
        if (playerCamera == null)
        {
            canPlaceHere = false;
            return;
        }

        if (placementLayer.value == 0)
        {
            Debug.LogError("[TrapPlacer] Placement Layer is set to NOTHING! Check Inspector!");
            canPlaceHere = false;
            return;
        }

        // METHOD 1: Screen center raycast (most accurate for UI crosshair)
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));

        // Debug: Show exactly where ray is going
        if (showDebugVisuals)
        {
            Debug.DrawRay(ray.origin, ray.direction * maxPlacementDistance, Color.cyan, 0.1f);
        }

        if (Physics.Raycast(ray, out RaycastHit hit, maxPlacementDistance, placementLayer))
        {
            canPlaceHere = true;
            placementPosition = hit.point;
            placementRotation = Quaternion.identity;

            // Visual debug - shows where raycast hits
            if (showDebugVisuals)
            {
                Debug.DrawRay(hit.point, Vector3.up * 2f, Color.green, 0.1f);
                Debug.DrawRay(hit.point, Vector3.right * 0.5f, Color.green, 0.1f);
                Debug.DrawRay(hit.point, Vector3.forward * 0.5f, Color.green, 0.1f);
            }
        }
        else
        {
            canPlaceHere = false;
            if (showDebugVisuals)
            {
                Debug.DrawRay(ray.origin, ray.direction * maxPlacementDistance, Color.red, 0.1f);
            }
        }
    }

    private void UpdateDebugVisuals()
    {
        if (debugCube == null) return;

        // CHANGED: Immediate hide/show with no delay
        if (canPlaceHere && placementModeActive)
        {
            // Show preview cube at placement position
            if (!debugCube.activeSelf)
            {
                debugCube.SetActive(true);
            }
            debugCube.transform.position = placementPosition + Vector3.up * spawnHeightOffset;

            // Update color to green with high transparency
            var renderer = debugCube.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                renderer.material.color = new Color(0f, 1f, 0f, debugCubeAlpha);
            }
        }
        else
        {
            // Hide cube IMMEDIATELY when placement is invalid
            if (debugCube.activeSelf)
            {
                debugCube.SetActive(false);
            }
        }
    }

    private void UpdateCrosshairFeedback()
    {
        if (crosshairUI == null) return;
        crosshairUI.SetValidPlacement(canPlaceHere);
    }

    private void TryPlaceItem()
    {
        // Check cooldown
        if (Time.time - lastPlacementTime < placementCooldown)
        {
            return;
        }

        if (inventory == null)
        {
            Debug.LogWarning("[TrapPlacer] No PlayerInventory!");
            return;
        }

        // Get item to place
        int itemId = inventory.PeekFrontItem();
        if (itemId <= 0)
        {
            Debug.LogWarning("[TrapPlacer] No valid item in inventory!");
            ExitPlacementMode();
            return;
        }

        if (ItemManager.Instance == null)
        {
            Debug.LogWarning("[TrapPlacer] ItemManager not found in scene!");
            return;
        }

        if (!ItemManager.Instance.HasItemData(itemId))
        {
            Debug.LogWarning($"[TrapPlacer] Invalid item ID {itemId}!");
            return;
        }

        // Calculate spawn position with height offset
        Vector3 spawnPosition = placementPosition + Vector3.up * spawnHeightOffset;

        // CHANGED: Hide debug cube IMMEDIATELY after placing
        if (debugCube != null)
        {
            debugCube.SetActive(false);
        }

        // ENHANCED DEBUG: Multiple visual indicators
        if (showDebugVisuals)
        {
            // Draw persistent markers at spawn position
            Debug.DrawRay(spawnPosition, Vector3.up * 3f, Color.red, 10f);
            Debug.DrawRay(spawnPosition, Vector3.right * 1f, Color.red, 10f);
            Debug.DrawRay(spawnPosition, Vector3.left * 1f, Color.red, 10f);
            Debug.DrawRay(spawnPosition, Vector3.forward * 1f, Color.red, 10f);
            Debug.DrawRay(spawnPosition, Vector3.back * 1f, Color.red, 10f);

            // Draw line from camera to spawn position
            Debug.DrawLine(playerCamera.transform.position, spawnPosition, Color.magenta, 10f);

            // Create a persistent debug cube at the placement location (for debugging purposes only)
            CreatePersistentDebugCube(spawnPosition);
        }

        // Request spawn from server
        ItemManager.Instance.RequestSpawnItemServerRpc(itemId, spawnPosition, placementRotation, OwnerClientId);

        // Remove from inventory
        inventory.PopItemServerRpc(OwnerClientId);

        lastPlacementTime = Time.time;

        // Exit placement mode if inventory is now empty
        if (inventory.GetItemCount() == 0)
        {
            ExitPlacementMode();
        }
        else
        {
            // CHANGED: Ensure debug cube can reappear for next placement
            // The cube will reappear in UpdateDebugVisuals if placement is still valid
        }
    }

    private void CreatePersistentDebugCube(Vector3 position)
    {
        // Create a temporary debug cube that appears and disappears immediately
        GameObject persistentCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        persistentCube.name = "DebugPlacementCube";
        persistentCube.transform.position = position;
        persistentCube.transform.localScale = Vector3.one * 0.5f;

        // Remove collider
        Destroy(persistentCube.GetComponent<Collider>());

        // Make it semi-transparent red (to distinguish from preview cube)
        var renderer = persistentCube.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.SetFloat("_Mode", 3); // Transparent mode
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
        mat.color = new Color(1f, 0f, 0f, 0.3f); // Red to show where item was placed
        renderer.material = mat;

        Destroy(persistentCube);

        Debug.Log($"[TrapPlacer] Placed item at {position}");
    }

    private void OnDrawGizmosSelected()
    {
        if (playerCamera == null) return;

        // Show both raycast methods for comparison
        Ray screenRay = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));
        Ray viewportRay = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(screenRay.origin, screenRay.direction * maxPlacementDistance);

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(viewportRay.origin, viewportRay.direction * maxPlacementDistance);

        if (canPlaceHere)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(placementPosition, Vector3.one * 0.3f);
            Gizmos.DrawWireCube(placementPosition + Vector3.up * spawnHeightOffset, Vector3.one * 0.2f);
        }
    }

    private void OnDestroy()
    {
        if (debugCube != null)
        {
            Destroy(debugCube);
        }
    }

    // Debug command to test crosshair alignment
    [ContextMenu("Test Crosshair Alignment")]
    private void TestCrosshairAlignment()
    {
        if (playerCamera == null) return;

        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            GameObject testCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            testCube.transform.position = hit.point;
            testCube.transform.localScale = Vector3.one * 0.5f;
            testCube.GetComponent<Renderer>().material.color = Color.cyan;
            Destroy(testCube.GetComponent<Collider>());

            Debug.Log($"[TEST] Created test cube at crosshair position: {hit.point}");
        }
    }
}