using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Handles player pickup interactions with NetworkPickupItem objects
/// Uses Q key to avoid conflict with placement system
/// </summary>
public class PlayerPickup : NetworkBehaviour
{
    [Header("Pickup Settings")]
    [SerializeField] private float pickupRange = 3f;
    [SerializeField] private KeyCode pickupKey = KeyCode.Q;
    [SerializeField] private LayerMask pickupLayer;

    [Header("Control")]
    [SerializeField] private bool isPickupEnabled = true;

    [Header("UI Feedback (Optional)")]
    [SerializeField] private GameObject pickupPromptUI;
    [SerializeField] private float promptOffset = 1.5f;

    private NetworkPickupItem nearestItem = null;
    private PlayerInventory inventory;
    private Camera playerCamera;

    public bool IsPickupEnabled { get => isPickupEnabled; set => isPickupEnabled = value; }

    // =========================================================
    // === INITIALIZATION ======================================
    // =========================================================

    private void Start()
    {
        // Get component references regardless of ownership
        // (needed for ServerRpc execution on server)
        inventory = GetComponent<PlayerInventory>();
        if (inventory == null)
        {
            Debug.LogError("[PlayerPickup] No PlayerInventory component found!");
        }

        // Only the owner client needs camera and UI setup
        if (!IsOwner) return;

        playerCamera = Camera.main;

        if (pickupPromptUI != null)
        {
            pickupPromptUI.SetActive(false);
        }

        Debug.Log($"[PlayerPickup] Initialized for player {OwnerClientId}. Use {pickupKey} to pick up items.");
    }
    // =========================================================
    // === UPDATE ==============================================
    // =========================================================

    private void Update()
    {
        if (!IsOwner) return;
        if (!isPickupEnabled) return;

        FindNearestPickupItem();
        UpdatePickupPrompt();

        // Attempt pickup
        if (Input.GetKeyDown(pickupKey) && nearestItem != null)
        {
            TryPickup();
        }
    }

    // =========================================================
    // === PICKUP DETECTION ====================================
    // =========================================================

    private void FindNearestPickupItem()
    {
        NetworkPickupItem[] allItems = FindObjectsByType<NetworkPickupItem>(FindObjectsSortMode.None);
        NetworkPickupItem closest = null;
        float closestDistance = pickupRange;

        foreach (NetworkPickupItem item in allItems)
        {
            // Skip already picked up items
            if (item.IsPickedUp) continue;

            // Skip deployed traps - check if item has a deployed trap component
            TrapBase trap = item.GetComponent<TrapBase>();
            if (trap != null && trap.IsDeployed)
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, item.transform.position);

            if (distance <= closestDistance)
            {
                closest = item;
                closestDistance = distance;
            }
        }

        nearestItem = closest;
    }

    // =========================================================
    // === PICKUP EXECUTION ====================================
    // =========================================================

    private void TryPickup()
    {
        if (!isPickupEnabled)
        {
            Debug.Log("[PlayerPickup] Pickup is currently disabled");
            return;
        }

        if (nearestItem == null)
        {
            Debug.LogWarning("[PlayerPickup] No item nearby to pick up!");
            return;
        }

        // Double-check that it's not a deployed trap
        TrapBase trap = nearestItem.GetComponent<TrapBase>();
        if (trap != null && trap.IsDeployed)
        {
            Debug.LogWarning("[PlayerPickup] Cannot pick up deployed trap!");
            return;
        }

        // Check inventory space locally first (faster feedback)
        if (inventory != null && inventory.IsInventoryFull())
        {
            Debug.LogWarning("[PlayerPickup] Inventory full!");
            ShowInventoryFullFeedback();
            return;
        }

        Debug.Log($"[PlayerPickup] Requesting pickup of {nearestItem.ItemName} (NetworkObjectId: {nearestItem.NetworkObjectId})");
        RequestPickupServerRpc(nearestItem.NetworkObjectId);
    }

    // =========================================================
    // === SERVER RPC ==========================================
    // =========================================================

    [ServerRpc]
    private void RequestPickupServerRpc(ulong itemNetworkId)
    {
        Debug.Log($"[PlayerPickup - SERVER] Client {OwnerClientId} requesting pickup of item {itemNetworkId}");

        // Validate network object exists
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(itemNetworkId, out NetworkObject itemNetObj))
        {
            Debug.LogError($"[PlayerPickup - SERVER] Item {itemNetworkId} not found in spawned objects!");
            NotifyPickupFailedClientRpc("Item not found");
            return;
        }

        // Get pickup component
        NetworkPickupItem item = itemNetObj.GetComponent<NetworkPickupItem>();
        if (item == null)
        {
            Debug.LogError($"[PlayerPickup - SERVER] NetworkObject {itemNetworkId} missing NetworkPickupItem component!");
            NotifyPickupFailedClientRpc("Invalid item");
            return;
        }

        // Check if already picked up
        if (item.IsPickedUp)
        {
            Debug.LogWarning($"[PlayerPickup - SERVER] Item {item.ItemName} already picked up!");
            NotifyPickupFailedClientRpc("Item already taken");
            return;
        }

        // Check if it's a deployed trap
        TrapBase trap = item.GetComponent<TrapBase>();
        if (trap != null && trap.IsDeployed)
        {
            Debug.LogWarning($"[PlayerPickup - SERVER] Cannot pick up deployed trap!");
            NotifyPickupFailedClientRpc("Cannot pick up deployed trap");
            return;
        }

        // Validate range (server authoritative)
        float distance = Vector3.Distance(transform.position, item.transform.position);
        if (distance > pickupRange)
        {
            Debug.LogWarning($"[PlayerPickup - SERVER] Player too far from item! Distance: {distance:F2}m");
            NotifyPickupFailedClientRpc("Too far away");
            return;
        }

        // Check inventory space
        if (inventory == null)
        {
            Debug.LogError($"[PlayerPickup - SERVER] No PlayerInventory component!");
            NotifyPickupFailedClientRpc("Inventory error");
            return;
        }

        if (inventory.IsInventoryFull())
        {
            Debug.LogWarning($"[PlayerPickup - SERVER] Inventory full for client {OwnerClientId}!");
            NotifyInventoryFullClientRpc();
            return;
        }

        // === ALL CHECKS PASSED - EXECUTE PICKUP ===

        // Add to inventory
        inventory.AddItemServerRpc(item.ItemID);

        // Mark item as picked up (triggers visual changes via NetworkVariable)
        item.PickupItem();

        Debug.Log($"[PlayerPickup - SERVER] Successfully picked up {item.ItemName} for client {OwnerClientId}");

        // Notify client of success
        NotifyPickupSuccessClientRpc(item.ItemName);
    }

    // =========================================================
    // === CLIENT RPC NOTIFICATIONS ============================
    // =========================================================

    [ClientRpc]
    private void NotifyPickupSuccessClientRpc(string itemName)
    {
        if (!IsOwner) return;

        Debug.Log($"[PlayerPickup - CLIENT] Successfully picked up: {itemName}");
        nearestItem = null;

        // Add success feedback (sound, UI notification, etc.)
        ShowPickupSuccessFeedback(itemName);
    }

    [ClientRpc]
    private void NotifyInventoryFullClientRpc()
    {
        if (!IsOwner) return;
        Debug.LogWarning("[PlayerPickup - CLIENT] Inventory is full!");
        ShowInventoryFullFeedback();
    }

    [ClientRpc]
    private void NotifyPickupFailedClientRpc(string reason)
    {
        if (!IsOwner) return;
        Debug.LogWarning($"[PlayerPickup - CLIENT] Pickup failed: {reason}");
    }

    // =========================================================
    // === UI FEEDBACK =========================================
    // =========================================================

    private void UpdatePickupPrompt()
    {
        if (pickupPromptUI == null) return;

        if (!isPickupEnabled)
        {
            pickupPromptUI.SetActive(false);
            return;
        }

        if (nearestItem != null && !nearestItem.IsPickedUp)
        {
            // Additional check for deployed traps
            TrapBase trap = nearestItem.GetComponent<TrapBase>();
            if (trap != null && trap.IsDeployed)
            {
                pickupPromptUI.SetActive(false);
                return;
            }

            pickupPromptUI.SetActive(true);

            // Position prompt above item
            if (playerCamera != null)
            {
                Vector3 screenPos = playerCamera.WorldToScreenPoint(nearestItem.transform.position + Vector3.up * promptOffset);
                pickupPromptUI.transform.position = screenPos;
            }
        }
        else
        {
            pickupPromptUI.SetActive(false);
        }
    }

    private void ShowPickupSuccessFeedback(string itemName)
    {
        // Implement UI notification, sound, etc.
        // Example: UIManager.Instance.ShowNotification($"Picked up {itemName}");
    }

    private void ShowInventoryFullFeedback()
    {
        // Implement UI notification
        // Example: UIManager.Instance.ShowNotification("Inventory Full!", Color.red);
    }

    public void SetPickupEnabled(bool enabled)
    {
        isPickupEnabled = enabled;
        Debug.Log($"[PlayerPickup] Pickup {(enabled ? "enabled" : "disabled")}");

        if (!enabled && pickupPromptUI != null)
        {
            pickupPromptUI.SetActive(false);
        }
    }

    // =========================================================
    // === DEBUG ===============================================
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, pickupRange);

        if (nearestItem != null && Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, nearestItem.transform.position);
        }
    }
}