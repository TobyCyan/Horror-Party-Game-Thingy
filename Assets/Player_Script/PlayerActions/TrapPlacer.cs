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
    [SerializeField] private float spawnHeightOffset = 0.01f;

    [Header("Control")]
    [SerializeField] private bool isPlacementEnabled = true;

    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerInventory inventory;

    [Header("Preview Settings")]
    [SerializeField] private float previewCubeAlpha = 0.15f;
    [SerializeField] private float previewCubeSize = 0.4f;
    [SerializeField] private float previewCubeVerticalOffset = -0.2f; // Negative to lower, positive to raise

    private LocalCrosshairUI crosshairUI;

    public bool IsPlacementEnabled { get => isPlacementEnabled; set => isPlacementEnabled = value; }

    private bool placementModeActive = false;
    private bool canPlaceHere = false;
    private Vector3 placementPosition;
    private Quaternion placementRotation;
    private float lastPlacementTime;
    private GameObject previewCube; // For visualizing placement position

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
        Material mat = new Material(Shader.Find("Standard"));
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

        if (canPlaceHere && placementModeActive)
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
        crosshairUI.SetValidPlacement(canPlaceHere);
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

        // Calculate spawn position with height offset
        Vector3 spawnPosition = placementPosition + Vector3.up * spawnHeightOffset;

        // Hide preview cube immediately after placing
        if (previewCube != null)
        {
            previewCube.SetActive(false);
        }

        // Use callback version to get spawned object reference
        ItemManager.Instance.RequestSpawnItem(itemId, spawnPosition, placementRotation, (spawnedObject) =>
        {
            if (spawnedObject != null)
            {
                TrapBase trap = spawnedObject.GetComponent<TrapBase>();
                if (trap != null)
                {
                    trap.Deploy(spawnPosition, placementRotation, gameObject);
                }
            }
        });

        inventory.PopItemServerRpc(OwnerClientId);

        lastPlacementTime = Time.time;

        if (inventory.GetItemCount() == 0)
        {
            ExitPlacementMode();
        }
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
}