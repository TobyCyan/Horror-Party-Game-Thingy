using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Queue-based player inventory for PvP games.
/// Only stores item IDs (FIFO order). Designed for simple pickup-place mechanics.
/// FIXED: Added proper verification and error handling
/// </summary>
public class PlayerInventory : NetworkBehaviour
{
    [Header("Inventory Settings")]
    [SerializeField] private int maxInventorySlots = 1; // currently one slot only

    // Queue holds item IDs in FIFO order
    private readonly Queue<int> itemQueue = new Queue<int>();

    // Network sync list for replication
    private NetworkList<int> networkItems;

    void Awake()
    {
        networkItems = new NetworkList<int>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        networkItems.OnListChanged += _ => SyncQueueWithNetworkList();
        Debug.Log($"[PlayerInventory] Spawned. IsServer: {IsServer}, MaxSlots: {maxInventorySlots}");
    }

    public override void OnDestroy()
    {
        if (networkItems != null)
        {
            networkItems.OnListChanged -= _ => SyncQueueWithNetworkList();
        }
        base.OnDestroy();
    }

    // ============================================================
    // === PUBLIC METHODS =========================================
    // ============================================================

    public bool IsInventoryFull()
    {
        return itemQueue.Count >= maxInventorySlots;
    }

    public int GetAvailableSlots()
    {
        return maxInventorySlots - itemQueue.Count;
    }

    /// <summary>
    /// Server-only method to add item with verification
    /// Returns true if successfully added
    /// </summary>
    public bool TryAddItemServer(int itemID)
    {
        if (!IsServer)
        {
            Debug.LogError("[PlayerInventory] TryAddItemServer can only be called on server!");
            return false;
        }

        if (IsInventoryFull())
        {
            Debug.LogWarning($"[PlayerInventory - SERVER] Inventory full! Cannot add {itemID}");
            return false;
        }

        if (itemID <= 0)
        {
            Debug.LogError($"[PlayerInventory - SERVER] Invalid item ID: {itemID}");
            return false;
        }

        try
        {
            itemQueue.Enqueue(itemID);
            networkItems.Add(itemID);
            Debug.Log($"[PlayerInventory - SERVER] Successfully added item ID {itemID}. Now holding {itemQueue.Count}/{maxInventorySlots}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayerInventory - SERVER] Failed to add item {itemID}: {e.Message}");
            // Rollback if network list was modified but queue wasn't
            if (networkItems.Count > itemQueue.Count)
            {
                networkItems.RemoveAt(networkItems.Count - 1);
            }
            return false;
        }
    }

    /// <summary>
    /// Legacy ServerRpc version - kept for compatibility but uses new method internally
    /// </summary>
    [ServerRpc]
    public void AddItemServerRpc(int itemID)
    {
        TryAddItemServer(itemID);
    }

    /// <summary>
    /// Server-only method to remove item with verification
    /// Returns the removed item ID, or -1 if failed
    /// </summary>
    public int TryPopItemServer()
    {
        if (!IsServer)
        {
            Debug.LogError("[PlayerInventory] TryPopItemServer can only be called on server!");
            return -1;
        }

        if (itemQueue.Count == 0)
        {
            Debug.LogWarning($"[PlayerInventory - SERVER] Tried to pop but inventory empty!");
            return -1;
        }

        try
        {
            int removed = itemQueue.Dequeue();
            if (networkItems.Count > 0)
            {
                networkItems.RemoveAt(0);
            }

            Debug.Log($"[PlayerInventory - SERVER] Removed item ID {removed}. Remaining {itemQueue.Count}");
            return removed;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayerInventory - SERVER] Failed to pop item: {e.Message}");
            return -1;
        }
    }

    [ServerRpc]
    public void PopItemServerRpc(ulong clientId)
    {
        TryPopItemServer();
    }

    public int PeekFrontItem()
    {
        return itemQueue.Count > 0 ? itemQueue.Peek() : -1;
    }

    public int GetItemCount()
    {
        return itemQueue.Count;
    }

    /// <summary>
    /// Get a snapshot of current inventory for debugging
    /// </summary>
    public int[] GetInventorySnapshot()
    {
        return itemQueue.ToArray();
    }

    // ============================================================
    // === INTERNAL SYNC HELPERS =================================
    // ============================================================
    private void SyncQueueWithNetworkList()
    {
        itemQueue.Clear();
        foreach (int id in networkItems)
        {
            itemQueue.Enqueue(id);
        }

        Debug.Log($"[PlayerInventory] Synced inventory. Count: {itemQueue.Count}");
    }

    // ============================================================
    // === DEBUG ==================================================
    // ============================================================

#if UNITY_EDITOR
    [ContextMenu("Print Inventory")]
    private void PrintInventory()
    {
        Debug.Log($"=== Inventory ({itemQueue.Count}/{maxInventorySlots}) ===");
        int index = 0;
        foreach (int id in itemQueue)
        {
            Debug.Log($"  [{index}]: Item ID {id}");
            index++;
        }
    }
#endif
}