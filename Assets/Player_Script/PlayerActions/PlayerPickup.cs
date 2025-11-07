using UnityEngine;
using Unity.Netcode;
using System;

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
    [SerializeField] private AudioSettings pickupSfxSettings;

    [Header("Control")]
    [SerializeField] private bool isPickupEnabled = true;

    [Header("UI Feedback (Optional)")]
    [SerializeField] private GameObject pickupPromptUI;
    [SerializeField] private float promptOffset = 1.5f;

    [Header("Pickup Protection")]
    [SerializeField] private float pickupCooldown = 0.2f; // Prevent spam clicks

    private NetworkPickupItem nearestItem = null;
    private PlayerInventory inventory;
    private Camera playerCamera;
    private Player player;
    private float lastPickupAttemptTime = 0f;

    public bool IsPickupEnabled { get => isPickupEnabled; set => isPickupEnabled = value; }
    public event Action OnTrapPickUp;

    // =========================================================
    // === INITIALIZATION ======================================
    // =========================================================

    private void Start()
    {
        player = GetComponent<Player>();
        inventory = GetComponent<PlayerInventory>();
        if (!IsOwner) return;

        if (inventory == null)
        {
            Debug.LogError("[PlayerPickup] No PlayerInventory component found!");
            enabled = false;
            return;
        }

        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            Debug.LogError("[PlayerPickup] No main camera found!");
            enabled = false;
            return;
        }

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

        // Attempt pickup with cooldown protection
        if (Input.GetKeyDown(pickupKey) && nearestItem != null)
        {
            if (Time.time - lastPickupAttemptTime >= pickupCooldown)
            {
                TryPickup();
                lastPickupAttemptTime = Time.time;
            }
            else
            {
                Debug.Log("[PlayerPickup] Pickup on cooldown");
            }
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
            // Use the item's own method to check if it can be picked up
            if (!item.CanBePickedUp()) continue;

            // Skip deployed traps
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

        // Verify item can still be picked up (double-check)
        if (!nearestItem.CanBePickedUp())
        {
            Debug.LogWarning("[PlayerPickup] Item is no longer available for pickup!");
            nearestItem = null;
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

        // Send pickup request to server
        RequestPickupServerRpc(nearestItem.NetworkObjectId);

        // Clear nearest item immediately to prevent double-clicks
        nearestItem = null;
    }

    // =========================================================
    // === SERVER RPC - TRANSACTIONAL PICKUP ===================
    // =========================================================

    [ServerRpc]
    private void RequestPickupServerRpc(ulong itemNetworkId)
    {
        Debug.Log($"[PlayerPickup - SERVER] Client {OwnerClientId} requesting pickup of item {itemNetworkId}");

        // === VALIDATION PHASE ===

        // 1. Validate network object exists
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(itemNetworkId, out NetworkObject itemNetObj))
        {
            Debug.LogError($"[PlayerPickup - SERVER] Item {itemNetworkId} not found in spawned objects!");
            NotifyPickupFailedClientRpc("Item not found");
            return;
        }

        // 2. Get pickup component
        NetworkPickupItem item = itemNetObj.GetComponent<NetworkPickupItem>();
        if (item == null)
        {
            Debug.LogError($"[PlayerPickup - SERVER] NetworkObject {itemNetworkId} missing NetworkPickupItem component!");
            NotifyPickupFailedClientRpc("Invalid item");
            return;
        }

        // 3. Check if item can be picked up (using item's own method)
        if (!item.CanBePickedUp())
        {
            Debug.LogWarning($"[PlayerPickup - SERVER] Item {item.ItemName} cannot be picked up! IsPickedUp: {item.IsPickedUp}, IsDeployed: {item.IsDeployed}, InProgress: {item.IsPickupInProgress}");
            NotifyPickupFailedClientRpc("Item already taken");
            return;
        }

        // 4. Check if it's a deployed trap (extra safety)
        TrapBase trap = item.GetComponent<TrapBase>();
        if (trap != null && trap.IsDeployed)
        {
            Debug.LogWarning($"[PlayerPickup - SERVER] Cannot pick up deployed trap!");
            NotifyPickupFailedClientRpc("Cannot pick up deployed trap");
            return;
        }

        // 5. Validate range (server authoritative)
        float distance = Vector3.Distance(transform.position, item.transform.position);
        if (distance > pickupRange + 1f) // Small tolerance for latency
        {
            Debug.LogWarning($"[PlayerPickup - SERVER] Player too far from item! Distance: {distance:F2}m");
            NotifyPickupFailedClientRpc("Too far away");
            return;
        }

        // 6. Check inventory space
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

        // === TRANSACTION PHASE ===

        // 7. Start pickup transaction (marks item as in-progress)
        if (!item.StartPickupProcess())
        {
            Debug.LogError($"[PlayerPickup - SERVER] Failed to start pickup process for {item.ItemName}");
            NotifyPickupFailedClientRpc("Pickup failed to start");
            return;
        }

        // 8. Try to add to inventory
        bool inventorySuccess = inventory.TryAddItemServer(item.ItemID);

        if (!inventorySuccess)
        {
            // ROLLBACK: Inventory add failed
            Debug.LogError($"[PlayerPickup - SERVER] Failed to add item {item.ItemID} to inventory! Rolling back...");
            item.CancelPickup();
            NotifyPickupFailedClientRpc("Failed to add to inventory");
            return;
        }

        // === COMMIT PHASE ===

        // 9. Inventory add succeeded - complete the pickup
        item.CompletePickup();

        Debug.Log($"[PlayerPickup - SERVER] Successfully picked up {item.ItemName} for client {OwnerClientId}");

        // 10. Despawn item after a short delay (gives time for effects to play)
        Invoke(nameof(DespawnItemDelayed), 0.1f);
        void DespawnItemDelayed() => item.DespawnItem();

        // 11. Notify client of success
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

        // Clear nearest item reference
        nearestItem = null;

        // Show success feedback
        ShowPickupSuccessFeedback(itemName);
        OnTrapPickUp?.Invoke();
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

        // Allow trying to find another item
        nearestItem = null;
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

        if (nearestItem != null && nearestItem.CanBePickedUp())
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
        // TODO: Implement UI notification, sound, etc.
        // Example: UIManager.Instance.ShowNotification($"Picked up {itemName}");
        Debug.Log($"[PlayerPickup] 🎯 Picked up: {itemName}");
        PlayPickUpSfx();
    }

    private void PlayPickUpSfx()
    {
        if (pickupSfxSettings.IsNullOrEmpty()) return;
        if (player == null) return;
        player.PlayLocalAudio(pickupSfxSettings);
    }

    private void ShowInventoryFullFeedback()
    {
        // TODO: Implement UI notification
        // Example: UIManager.Instance.ShowNotification("Inventory Full!", Color.red);
        Debug.Log("[PlayerPickup] ❌ Inventory Full!");
    }

    // =========================================================
    // === PUBLIC API ==========================================
    // =========================================================

    public void SetPickupEnabled(bool enabled)
    {
        isPickupEnabled = enabled;
        Debug.Log($"[PlayerPickup] Pickup {(enabled ? "enabled" : "disabled")}");

        if (!enabled)
        {
            nearestItem = null;
            if (pickupPromptUI != null)
            {
                pickupPromptUI.SetActive(false);
            }
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
            Gizmos.color = nearestItem.CanBePickedUp() ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position, nearestItem.transform.position);
            Gizmos.DrawWireSphere(nearestItem.transform.position, 0.3f);
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Print Inventory Status")]
    private void PrintInventoryStatus()
    {
        if (inventory != null)
        {
            Debug.Log($"=== Inventory Status ===");
            Debug.Log($"Count: {inventory.GetItemCount()}");
            Debug.Log($"Front Item: {inventory.PeekFrontItem()}");
            Debug.Log($"Is Full: {inventory.IsInventoryFull()}");
        }
        else
        {
            Debug.LogWarning("No inventory component");
        }
    }
#endif
}