using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Server-authoritative item spawning system
/// </summary>
public class ItemManager : NetworkBehaviour
{
    public static ItemManager Instance { get; private set; }

    [Header("Item Database")]
    [SerializeField] private NetworkPickupItem[] itemPrefabs;
    [Tooltip("Item IDs start at 1. Array index 0 = Item ID 1")]

    [Header("Spawn Settings")]
    [SerializeField] private float minSpawnHeight = 0.5f;
    [SerializeField] private int maxActiveItems = 100;

    // Track spawned items
    private readonly List<NetworkObject> spawnedItems = new List<NetworkObject>();

    // Track pending spawn requests (clientId -> callback)
    private readonly Dictionary<ulong, System.Action<NetworkObject>> pendingSpawnCallbacks = new Dictionary<ulong, System.Action<NetworkObject>>();

    // =========================================================
    // === SINGLETON SETUP =====================================
    // =========================================================

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[ItemManager] Duplicate ItemManager found! Destroying...");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ValidateItemDatabase();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            Debug.Log("[ItemManager] Server item spawning system active");
        }
    }

    // =========================================================
    // === VALIDATION ==========================================
    // =========================================================

    private void ValidateItemDatabase()
    {
        if (itemPrefabs == null || itemPrefabs.Length == 0)
        {
            Debug.LogError("[ItemManager] No item prefabs assigned! Please assign items in Inspector.");
            return;
        }

        for (int i = 0; i < itemPrefabs.Length; i++)
        {
            if (itemPrefabs[i] == null)
            {
                Debug.LogWarning($"[ItemManager] Item slot {i} (ID {i + 1}) is null!");
                continue;
            }

            NetworkObject netObj = itemPrefabs[i].GetComponent<NetworkObject>();
            if (netObj == null)
            {
                Debug.LogError($"[ItemManager] Item '{itemPrefabs[i].name}' missing NetworkObject component!");
            }

            NetworkPickupItem pickup = itemPrefabs[i].GetComponent<NetworkPickupItem>();
            if (pickup == null)
            {
                Debug.LogError($"[ItemManager] Item '{itemPrefabs[i].name}' missing NetworkPickupItem component!");
            }
        }

        Debug.Log($"[ItemManager] Loaded {itemPrefabs.Length} item prefabs");
    }

    // =========================================================
    // === PUBLIC API ==========================================
    // =========================================================

    public bool HasItemData(int itemId)
    {
        int index = itemId - 1;
        return itemPrefabs != null &&
               index >= 0 &&
               index < itemPrefabs.Length &&
               itemPrefabs[index] != null;
    }

    public string GetItemName(int itemId)
    {
        NetworkPickupItem prefab = GetItemPrefab(itemId);
        return prefab != null ? prefab.ItemName : $"Unknown Item {itemId}";
    }

    public int GetSpawnedItemCount()
    {
        CleanupDestroyedItems();
        return spawnedItems.Count;
    }

    /// <summary>
    /// Request to spawn an item and get a callback with the spawned object reference
    /// </summary>
    public void RequestSpawnItem(int itemId, Vector3 position, Quaternion rotation, System.Action<NetworkObject> onSpawned)
    {
        if (!IsOwner)
        {
            Debug.LogError("[ItemManager] RequestSpawnItem must be called by the owner!");
            return;
        }

        // Store callback
        ulong clientId = NetworkManager.Singleton.LocalClientId;
        pendingSpawnCallbacks[clientId] = onSpawned;

        // Send request to server
        RequestSpawnItemServerRpc(itemId, position, rotation, clientId);
    }

    // =========================================================
    // === SERVER RPC - SPAWN ITEM =============================
    // =========================================================

    [ServerRpc(RequireOwnership = false)]
    public void RequestSpawnItemServerRpc(int itemId, Vector3 requestedPosition, Quaternion rotation, ulong senderClientId)
    {
        if (!IsServer)
        {
            Debug.LogError("[ItemManager] RequestSpawnItemServerRpc called but not server!");
            return;
        }

        // Validate item ID
        if (!HasItemData(itemId))
        {
            Debug.LogWarning($"[ItemManager] Invalid item ID {itemId} requested by client {senderClientId}");
            NotifySpawnFailedClientRpc(senderClientId);
            return;
        }

        // Check spawn limit
        CleanupDestroyedItems();
        if (spawnedItems.Count >= maxActiveItems)
        {
            Debug.LogWarning($"[ItemManager] Max item limit reached ({maxActiveItems}). Cannot spawn more items.");
            NotifySpawnFailedClientRpc(senderClientId);
            return;
        }

        // Adjust position if too low
        if (requestedPosition.y < minSpawnHeight)
        {
            requestedPosition.y = minSpawnHeight;
        }

        // Get prefab
        NetworkPickupItem prefab = GetItemPrefab(itemId);
        if (prefab == null)
        {
            Debug.LogError($"[ItemManager] Failed to get prefab for item ID {itemId}");
            NotifySpawnFailedClientRpc(senderClientId);
            return;
        }

        // Instantiate item
        NetworkPickupItem newItem = Instantiate(prefab, requestedPosition, rotation);

        NetworkObject netObj = newItem.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError($"[ItemManager] Prefab for item ID {itemId} missing NetworkObject!");
            Destroy(newItem.gameObject);
            NotifySpawnFailedClientRpc(senderClientId);
            return;
        }

        // Set item data
        newItem.GetType().GetField("itemID",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(newItem, itemId);

        if (string.IsNullOrEmpty(newItem.ItemName))
        {
            newItem.GetType().GetField("itemName",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(newItem, $"Item_{itemId}");
        }

        // Spawn on network
        netObj.Spawn(true);

        // Track spawned item
        spawnedItems.Add(netObj);

        Debug.Log($"[ItemManager] Spawned item ID {itemId} with NetworkObjectId {netObj.NetworkObjectId} for client {senderClientId}");

        // Notify the requesting client with the NetworkObjectId
        NotifySpawnSuccessClientRpc(senderClientId, netObj.NetworkObjectId);
    }

    // =========================================================
    // === CLIENT RPC - SPAWN NOTIFICATIONS ====================
    // =========================================================

    [ClientRpc]
    private void NotifySpawnSuccessClientRpc(ulong targetClientId, ulong networkObjectId)
    {
        // Only process if this is the client that requested the spawn
        if (NetworkManager.Singleton.LocalClientId != targetClientId)
            return;

        Debug.Log($"[ItemManager] Client received spawn success for NetworkObjectId {networkObjectId}");

        // Find the spawned network object
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject spawnedObject))
        {
            // Invoke callback if it exists
            if (pendingSpawnCallbacks.TryGetValue(targetClientId, out var callback))
            {
                callback?.Invoke(spawnedObject);
                pendingSpawnCallbacks.Remove(targetClientId);
            }
        }
        else
        {
            Debug.LogError($"[ItemManager] Could not find spawned NetworkObject with ID {networkObjectId}");
        }
    }

    [ClientRpc]
    private void NotifySpawnFailedClientRpc(ulong targetClientId)
    {
        // Only process if this is the client that requested the spawn
        if (NetworkManager.Singleton.LocalClientId != targetClientId)
            return;

        Debug.LogWarning("[ItemManager] Spawn request failed on server");

        // Invoke callback with null to indicate failure
        if (pendingSpawnCallbacks.TryGetValue(targetClientId, out var callback))
        {
            callback?.Invoke(null);
            pendingSpawnCallbacks.Remove(targetClientId);
        }
    }

    // =========================================================
    // === INTERNAL HELPERS ====================================
    // =========================================================

    private NetworkPickupItem GetItemPrefab(int itemId)
    {
        int index = itemId - 1;

        if (itemPrefabs == null || index < 0 || index >= itemPrefabs.Length)
        {
            return null;
        }

        return itemPrefabs[index];
    }

    private void CleanupDestroyedItems()
    {
        spawnedItems.RemoveAll(item => item == null || !item.IsSpawned);
    }

#if UNITY_EDITOR
    [ContextMenu("List All Item Prefabs")]
    private void ListAllItems()
    {
        if (itemPrefabs == null || itemPrefabs.Length == 0)
        {
            Debug.Log("No item prefabs assigned");
            return;
        }

        Debug.Log($"=== Item Database ({itemPrefabs.Length} items) ===");
        for (int i = 0; i < itemPrefabs.Length; i++)
        {
            if (itemPrefabs[i] != null)
            {
                Debug.Log($"  ID {i + 1}: {itemPrefabs[i].ItemName} ({itemPrefabs[i].name})");
            }
            else
            {
                Debug.Log($"  ID {i + 1}: [NULL]");
            }
        }
    }
#endif
}