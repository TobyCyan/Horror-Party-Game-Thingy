using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Handles trap/item placement with visual feedback
/// Hold E to enter placement mode, Left-click to place
/// FIXED: Better error handling and callback integration
/// </summary>
public class TrapPlacer : NetworkBehaviour
{
    [Header("Placement Settings")]
    [SerializeField] private float maxPlacementDistance = 10f;
    [SerializeField] private KeyCode placeKey = KeyCode.Mouse0;
    [SerializeField] private KeyCode placementModeKey = KeyCode.E;
    [SerializeField] private LayerMask placementLayer = -1;
    [SerializeField] private float placementCooldown = 0.3f;
    [SerializeField] private float spawnHeightOffset = 0.01f;

    [Header("Control")]
    [SerializeField] private bool isPlacementEnabled = true;

    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerInventory inventory;

    [Header("Preview Settings")]
    [SerializeField] private float previewCubeAlpha = 0.15f;
    [SerializeField] private float previewCubeSize = 0.4f;
    [SerializeField] private float previewCubeVerticalOffset = -0.2f;
    [SerializeField] private Shader previewCubeShader;

    private LocalCrosshairUI crosshairUI;

    public bool IsPlacementEnabled { get => isPlacementEnabled; set => isPlacementEnabled = value; }

    private bool placementModeActive = false;
    private bool canPlaceHere = false;
    private Vector3 placementPosition;
    private Quaternion placementRotation;
    private float lastPlacementTime;
    private GameObject previewCube;
    private bool isPlacementInProgress = false; // Prevent multiple simultaneous placements

    private void Start()
    {
        if (!IsOwner) return;

        // Find camera more reliably
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }

            if (playerCamera == null)
            {
                Debug.LogError("[TrapPlacer] No camera found! Please assign in inspector.");
                enabled = false;
                return;
            }
        }

        if (!playerCamera.enabled)
        {
            Debug.LogWarning("[TrapPlacer] Assigned camera is disabled!");
        }

        if (inventory == null)
        {
            inventory = GetComponent<PlayerInventory>();
            if (inventory == null)
            {
                Debug.LogError("[TrapPlacer] No PlayerInventory found!");
                enabled = false;
                return;
            }
        }

        if (crosshairUI == null)
        {
            crosshairUI = GetComponent<LocalCrosshairUI>();
        }

        if (crosshairUI != null)
        {
            crosshairUI.SetCrosshairVisible(false);
        }

        // Create preview cube
        CreatePreviewCube();
    }

    private void CreatePreviewCube()
    {
        previewCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        previewCube.name = "PlacementPreview";
        previewCube.transform.localScale = Vector3.one * previewCubeSize;

        // Remove collider so it doesn't interfere
        Destroy(previewCube.GetComponent<Collider>());

        // Make material transparent
        var renderer = previewCube.GetComponent<Renderer>();
        Material mat = new(previewCubeShader);
        mat.SetFloat("_Mode", 3); // Transparent mode
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
        mat.color = new Color(0f, 1f, 0f, previewCubeAlpha);
        renderer.material = mat;

        previewCube.SetActive(false);
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (!isPlacementEnabled)
        {
            if (placementModeActive)
            {
                ExitPlacementMode();
            }
            return;
        }

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
        if (!isPlacementEnabled)
        {
            Debug.Log("[TrapPlacer] Cannot enter placement mode - placement is disabled");
            return;
        }

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

        if (previewCube != null)
        {
            previewCube.SetActive(false);
        }

        Debug.Log("[TrapPlacer] Exited placement mode");
    }

    private void UpdatePlacementMode()
    {
        CheckPlacementPosition();
        UpdateCrosshairFeedback();
        UpdatePreviewCube();

        if (Input.GetKeyDown(placeKey))
        {
            if (inventory == null || inventory.GetItemCount() == 0)
            {
                Debug.LogWarning("[TrapPlacer] Cannot place - no items in inventory!");
                ExitPlacementMode();
                return;
            }

            if (isPlacementInProgress)
            {
                Debug.Log("[TrapPlacer] Placement already in progress, please wait...");
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

        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, maxPlacementDistance, placementLayer))
        {
            canPlaceHere = true;
            placementPosition = hit.point;
            placementRotation = Quaternion.identity;
        }
        else
        {
            canPlaceHere = false;
        }
    }

    private void UpdatePreviewCube()
    {
        if (previewCube == null) return;

        if (canPlaceHere && placementModeActive && !isPlacementInProgress)
        {
            if (!previewCube.activeSelf)
            {
                previewCube.SetActive(true);
            }

            // Position preview cube with adjustable vertical offset
            previewCube.transform.position = placementPosition + Vector3.up * (spawnHeightOffset + previewCubeVerticalOffset);

            // Update color based on validity
            var renderer = previewCube.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                renderer.material.color = new Color(0f, 1f, 0f, previewCubeAlpha);
            }
        }
        else
        {
            if (previewCube.activeSelf)
            {
                previewCube.SetActive(false);
            }
        }
    }

    private void UpdateCrosshairFeedback()
    {
        if (crosshairUI == null) return;
        crosshairUI.SetValidPlacement(canPlaceHere && !isPlacementInProgress);
    }

    private void TryPlaceItem()
    {
        if (!isPlacementEnabled)
        {
            Debug.Log("[TrapPlacer] Placement is currently disabled");
            ExitPlacementMode();
            return;
        }

        if (Time.time - lastPlacementTime < placementCooldown)
        {
            Debug.Log("[TrapPlacer] Placement on cooldown");
            return;
        }

        if (inventory == null)
        {
            Debug.LogWarning("[TrapPlacer] No PlayerInventory!");
            return;
        }

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

        // Mark placement in progress
        isPlacementInProgress = true;

        // Calculate spawn position with height offset
        Vector3 spawnPosition = placementPosition + Vector3.up * spawnHeightOffset;

        // Hide preview cube immediately
        if (previewCube != null)
        {
            previewCube.SetActive(false);
        }

        Debug.Log($"[TrapPlacer] Requesting to place item ID {itemId} at {spawnPosition}");

        // Request spawn with callback
        ItemManager.Instance.RequestSpawnItem(itemId, spawnPosition, placementRotation, OnItemSpawned);

        lastPlacementTime = Time.time;
    }

    /// <summary>
    /// Callback when item spawn completes (success or failure)
    /// </summary>
    private void OnItemSpawned(NetworkObject spawnedObject)
    {
        if (spawnedObject != null)
        {
            Debug.Log($"[TrapPlacer] ✅ Item spawned successfully: {spawnedObject.NetworkObjectId}");

            // Get the item ID before we remove it from inventory
            int placedItemId = inventory.PeekFrontItem();

            // Remove from inventory
            inventory.PopItemServerRpc(OwnerClientId);

            // If it has a trap component, deploy it
            TrapBase trap = spawnedObject.GetComponent<TrapBase>();
            if (trap != null)
            {
                trap.Deploy(spawnedObject.transform.position, spawnedObject.transform.rotation, gameObject);
                Debug.Log($"[TrapPlacer] Deployed trap at {spawnedObject.transform.position}");
            }

            // Check if we should exit placement mode (no more items)
            if (inventory.GetItemCount() == 0)
            {
                ExitPlacementMode();
            }
        }
        else
        {
            Debug.LogError("[TrapPlacer] ❌ Item spawn failed!");
            // Don't remove from inventory if spawn failed
        }

        // Reset placement in progress flag
        isPlacementInProgress = false;
    }

    public void SetPlacementEnabled(bool enabled)
    {
        isPlacementEnabled = enabled;
        Debug.Log($"[TrapPlacer] Placement {(enabled ? "enabled" : "disabled")}");

        if (!enabled && placementModeActive)
        {
            ExitPlacementMode();
        }
    }

    public void DisableAllInteractions()
    {
        SetPlacementEnabled(false);

        var pickup = GetComponent<PlayerPickup>();
        if (pickup != null)
        {
            pickup.SetPickupEnabled(false);
        }
    }

    public void EnableAllInteractions()
    {
        SetPlacementEnabled(true);

        var pickup = GetComponent<PlayerPickup>();
        if (pickup != null)
        {
            pickup.SetPickupEnabled(true);
        }
    }

    private void OnDestroy()
    {
        if (previewCube != null)
        {
            Destroy(previewCube);
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Test Force Exit Placement Mode")]
    private void TestForceExit()
    {
        if (placementModeActive)
        {
            ExitPlacementMode();
        }
        isPlacementInProgress = false;
    }
#endif
}