using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Server-authoritative item spawning system
/// FIXED: ID-based lookup instead of index-based for more robust item management
/// </summary>
public class ItemManager : NetworkBehaviour
{
    public static ItemManager Instance { get; private set; }

    [Header("Item Database")]
    [SerializeField] private NetworkPickupItem[] itemPrefabs;
    [Tooltip("Items are matched by their ItemID field, not array index")]

    [Header("Spawn Settings")]
    [SerializeField] private float minSpawnHeight = 0.5f;
    [SerializeField] private int maxActiveItems = 100;

    // Track spawned items
    private readonly List<NetworkObject> spawnedItems = new List<NetworkObject>();

    // Track pending spawn requests with transaction IDs
    private readonly Dictionary<int, SpawnTransaction> pendingSpawnTransactions = new Dictionary<int, SpawnTransaction>();
    private int nextTransactionId = 1;

    private class SpawnTransaction
    {
        public ulong clientId;
        public System.Action<NetworkObject> callback;
        public float timestamp;
    }

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
            // Clean up old transactions periodically
            InvokeRepeating(nameof(CleanupOldTransactions), 5f, 5f);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            CancelInvoke(nameof(CleanupOldTransactions));
        }
        base.OnNetworkDespawn();
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

        int validCount = 0;
        HashSet<int> seenIds = new HashSet<int>();

        for (int i = 0; i < itemPrefabs.Length; i++)
        {
            if (itemPrefabs[i] == null)
            {
                Debug.LogWarning($"[ItemManager] Item slot {i} is null!");
                continue;
            }

            NetworkObject netObj = itemPrefabs[i].GetComponent<NetworkObject>();
            if (netObj == null)
            {
                Debug.LogError($"[ItemManager] Item '{itemPrefabs[i].name}' missing NetworkObject component!");
                continue;
            }

            NetworkPickupItem pickup = itemPrefabs[i].GetComponent<NetworkPickupItem>();
            if (pickup == null)
            {
                Debug.LogError($"[ItemManager] Item '{itemPrefabs[i].name}' missing NetworkPickupItem component!");
                continue;
            }

            int itemId = pickup.ItemID;
            if (itemId <= 0)
            {
                Debug.LogError($"[ItemManager] Item '{itemPrefabs[i].name}' has invalid ItemID: {itemId}. Must be > 0!");
                continue;
            }

            if (seenIds.Contains(itemId))
            {
                Debug.LogError($"[ItemManager] Duplicate ItemID {itemId} found! Item '{itemPrefabs[i].name}' conflicts with another item!");
                continue;
            }

            seenIds.Add(itemId);
            validCount++;
            Debug.Log($"[ItemManager] Registered item: ID {itemId} - {pickup.ItemName} ({itemPrefabs[i].name})");
        }

        Debug.Log($"[ItemManager] Loaded {validCount}/{itemPrefabs.Length} valid item prefabs");
    }

    // =========================================================
    // === PUBLIC API ==========================================
    // =========================================================

    /// <summary>
    /// Check if we have item data for the given ID by searching through the array
    /// </summary>
    public bool HasItemData(int itemId)
    {
        return GetItemPrefab(itemId) != null;
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
    /// Can be called by ANY client - ServerRpc handles permissions
    /// </summary>
    public void RequestSpawnItem(int itemId, Vector3 position, Quaternion rotation, System.Action<NetworkObject> onSpawned)
    {
        // Validate we're in a network session
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            Debug.LogError("[ItemManager] Not connected to network!");
            onSpawned?.Invoke(null);
            return;
        }

        if (!HasItemData(itemId))
        {
            Debug.LogError($"[ItemManager] Invalid item ID: {itemId}");
            onSpawned?.Invoke(null);
            return;
        }

        // Generate transaction ID
        int transactionId = nextTransactionId++;
        ulong clientId = NetworkManager.Singleton.LocalClientId;

        // Store callback with transaction
        SpawnTransaction transaction = new SpawnTransaction
        {
            clientId = clientId,
            callback = onSpawned,
            timestamp = Time.time
        };
        pendingSpawnTransactions[transactionId] = transaction;

        Debug.Log($"[ItemManager] Client {clientId} requesting spawn with transaction ID {transactionId}");

        // Send request to server (ServerRpc handles ownership automatically)
        RequestSpawnItemServerRpc(itemId, position, rotation, clientId, transactionId);
    }

    // =========================================================
    // === SERVER RPC - SPAWN ITEM =============================
    // =========================================================

    [ServerRpc(RequireOwnership = false)]
    public void RequestSpawnItemServerRpc(int itemId, Vector3 requestedPosition, Quaternion rotation, ulong senderClientId, int transactionId)
    {
        if (!IsServer)
        {
            Debug.LogError("[ItemManager] RequestSpawnItemServerRpc called but not server!");
            return;
        }

        Debug.Log($"[ItemManager - SERVER] Processing spawn request - ItemID: {itemId}, ClientID: {senderClientId}, TransactionID: {transactionId}");

        // Validate item ID by searching for the prefab
        NetworkPickupItem prefab = GetItemPrefab(itemId);
        if (prefab == null)
        {
            Debug.LogWarning($"[ItemManager - SERVER] Invalid item ID {itemId} requested by client {senderClientId} - no matching prefab found");
            NotifySpawnFailedClientRpc(senderClientId, transactionId, "Invalid item ID");
            return;
        }

        // Check spawn limit
        CleanupDestroyedItems();
        if (spawnedItems.Count >= maxActiveItems)
        {
            Debug.LogWarning($"[ItemManager - SERVER] Max item limit reached ({maxActiveItems}). Cannot spawn more items.");
            NotifySpawnFailedClientRpc(senderClientId, transactionId, "Item limit reached");
            return;
        }

        // Adjust position if too low
        if (requestedPosition.y < minSpawnHeight)
        {
            requestedPosition.y = minSpawnHeight;
            Debug.Log($"[ItemManager - SERVER] Adjusted spawn position Y to {minSpawnHeight}");
        }

        // Instantiate item
        NetworkPickupItem newItem = Instantiate(prefab, requestedPosition, rotation);
        if (newItem == null)
        {
            Debug.LogError($"[ItemManager - SERVER] Failed to instantiate item ID {itemId}");
            NotifySpawnFailedClientRpc(senderClientId, transactionId, "Instantiation failed");
            return;
        }

        NetworkObject netObj = newItem.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError($"[ItemManager - SERVER] Prefab for item ID {itemId} missing NetworkObject!");
            Destroy(newItem.gameObject);
            NotifySpawnFailedClientRpc(senderClientId, transactionId, "Missing NetworkObject");
            return;
        }

        // Set item data using reflection (ensure ID is set correctly)
        try
        {
            var itemIDField = newItem.GetType().GetField("itemID",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (itemIDField != null)
            {
                itemIDField.SetValue(newItem, itemId);
            }

            if (string.IsNullOrEmpty(newItem.ItemName))
            {
                var itemNameField = newItem.GetType().GetField("itemName",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (itemNameField != null)
                {
                    itemNameField.SetValue(newItem, prefab.ItemName);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ItemManager - SERVER] Error setting item data: {e.Message}");
        }

        // Spawn on network
        try
        {
            netObj.Spawn(true);

            // Track spawned item
            spawnedItems.Add(netObj);

            Debug.Log($"[ItemManager - SERVER] ✅ Successfully spawned item ID {itemId} ({prefab.ItemName}) with NetworkObjectId {netObj.NetworkObjectId} for client {senderClientId}");

            // Notify the requesting client with the NetworkObjectId
            NotifySpawnSuccessClientRpc(senderClientId, transactionId, netObj.NetworkObjectId);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ItemManager - SERVER] Error spawning item: {e.Message}");
            Destroy(newItem.gameObject);
            NotifySpawnFailedClientRpc(senderClientId, transactionId, "Spawn error");
        }
    }

    // =========================================================
    // === CLIENT RPC - SPAWN NOTIFICATIONS ====================
    // =========================================================

    [ClientRpc]
    private void NotifySpawnSuccessClientRpc(ulong targetClientId, int transactionId, ulong networkObjectId)
    {
        // Only process if this is the client that requested the spawn
        if (NetworkManager.Singleton.LocalClientId != targetClientId)
            return;

        Debug.Log($"[ItemManager - CLIENT] Received spawn success - TransactionID: {transactionId}, NetworkObjectId: {networkObjectId}");

        // Get the transaction
        if (!pendingSpawnTransactions.TryGetValue(transactionId, out SpawnTransaction transaction))
        {
            Debug.LogWarning($"[ItemManager - CLIENT] No pending transaction found for ID {transactionId}");
            return;
        }

        // Find the spawned network object
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject spawnedObject))
        {
            Debug.Log($"[ItemManager - CLIENT] ✅ Found spawned object, invoking callback");
            transaction.callback?.Invoke(spawnedObject);
        }
        else
        {
            Debug.LogError($"[ItemManager - CLIENT] Could not find spawned NetworkObject with ID {networkObjectId}");
            transaction.callback?.Invoke(null);
        }

        // Clean up transaction
        pendingSpawnTransactions.Remove(transactionId);
    }

    [ClientRpc]
    private void NotifySpawnFailedClientRpc(ulong targetClientId, int transactionId, string reason)
    {
        // Only process if this is the client that requested the spawn
        if (NetworkManager.Singleton.LocalClientId != targetClientId)
            return;

        Debug.LogWarning($"[ItemManager - CLIENT] Spawn request failed - TransactionID: {transactionId}, Reason: {reason}");

        // Get the transaction
        if (pendingSpawnTransactions.TryGetValue(transactionId, out SpawnTransaction transaction))
        {
            // Invoke callback with null to indicate failure
            transaction.callback?.Invoke(null);

            // Clean up transaction
            pendingSpawnTransactions.Remove(transactionId);
        }
    }

    // =========================================================
    // === INTERNAL HELPERS ====================================
    // =========================================================

    /// <summary>
    /// Get item prefab by searching through the array for matching ItemID
    /// This is more robust than index-based lookup
    /// </summary>
    private NetworkPickupItem GetItemPrefab(int itemId)
    {
        if (itemPrefabs == null || itemPrefabs.Length == 0)
        {
            return null;
        }

        // Search through all prefabs to find matching ID
        for (int i = 0; i < itemPrefabs.Length; i++)
        {
            if (itemPrefabs[i] == null)
                continue;

            NetworkPickupItem pickup = itemPrefabs[i].GetComponent<NetworkPickupItem>();
            if (pickup != null && pickup.ItemID == itemId)
            {
                return itemPrefabs[i];
            }
        }

        Debug.LogWarning($"[ItemManager] No prefab found with ItemID: {itemId}");
        return null;
    }

    private void CleanupDestroyedItems()
    {
        spawnedItems.RemoveAll(item => item == null || !item.IsSpawned);
    }

    private void CleanupOldTransactions()
    {
        float currentTime = Time.time;
        List<int> toRemove = new List<int>();

        foreach (var kvp in pendingSpawnTransactions)
        {
            // Remove transactions older than 10 seconds
            if (currentTime - kvp.Value.timestamp > 10f)
            {
                Debug.LogWarning($"[ItemManager] Removing stale transaction {kvp.Key} for client {kvp.Value.clientId}");
                toRemove.Add(kvp.Key);
            }
        }

        foreach (int id in toRemove)
        {
            if (pendingSpawnTransactions.TryGetValue(id, out SpawnTransaction trans))
            {
                trans.callback?.Invoke(null); // Notify failure
            }
            pendingSpawnTransactions.Remove(id);
        }
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

        Debug.Log($"=== Item Database ({itemPrefabs.Length} slots) ===");
        for (int i = 0; i < itemPrefabs.Length; i++)
        {
            if (itemPrefabs[i] != null)
            {
                NetworkPickupItem pickup = itemPrefabs[i].GetComponent<NetworkPickupItem>();
                if (pickup != null)
                {
                    Debug.Log($"  Slot {i}: ID {pickup.ItemID} - {pickup.ItemName} ({itemPrefabs[i].name})");
                }
                else
                {
                    Debug.Log($"  Slot {i}: {itemPrefabs[i].name} [NO NetworkPickupItem COMPONENT]");
                }
            }
            else
            {
                Debug.Log($"  Slot {i}: [NULL]");
            }
        }
    }

    [ContextMenu("Print Spawned Items Count")]
    private void PrintSpawnedCount()
    {
        CleanupDestroyedItems();
        Debug.Log($"Active spawned items: {spawnedItems.Count}/{maxActiveItems}");
    }

    [ContextMenu("Print Pending Transactions")]
    private void PrintPendingTransactions()
    {
        Debug.Log($"=== Pending Transactions ({pendingSpawnTransactions.Count}) ===");
        foreach (var kvp in pendingSpawnTransactions)
        {
            float age = Time.time - kvp.Value.timestamp;
            Debug.Log($"  Transaction {kvp.Key}: Client {kvp.Value.clientId}, Age: {age:F2}s");
        }
    }

    [ContextMenu("Validate Item IDs")]
    private void ValidateItemIDs()
    {
        if (itemPrefabs == null || itemPrefabs.Length == 0)
        {
            Debug.LogWarning("No item prefabs to validate");
            return;
        }

        Debug.Log("=== Item ID Validation ===");
        HashSet<int> foundIds = new HashSet<int>();
        int issues = 0;

        for (int i = 0; i < itemPrefabs.Length; i++)
        {
            if (itemPrefabs[i] == null)
            {
                Debug.LogWarning($"Slot {i}: NULL prefab");
                issues++;
                continue;
            }

            NetworkPickupItem pickup = itemPrefabs[i].GetComponent<NetworkPickupItem>();
            if (pickup == null)
            {
                Debug.LogError($"Slot {i} ({itemPrefabs[i].name}): Missing NetworkPickupItem component");
                issues++;
                continue;
            }

            int id = pickup.ItemID;
            if (id <= 0)
            {
                Debug.LogError($"Slot {i} ({itemPrefabs[i].name}): Invalid ID {id} (must be > 0)");
                issues++;
                continue;
            }

            if (foundIds.Contains(id))
            {
                Debug.LogError($"Slot {i} ({itemPrefabs[i].name}): Duplicate ID {id}!");
                issues++;
                continue;
            }

            foundIds.Add(id);
            Debug.Log($"Slot {i}: ✅ ID {id} - {pickup.ItemName}");
        }

        if (issues == 0)
        {
            Debug.Log($"✅ All {foundIds.Count} items validated successfully!");
        }
        else
        {
            Debug.LogWarning($"⚠️ Validation complete with {issues} issue(s)");
        }
    }
#endif
}