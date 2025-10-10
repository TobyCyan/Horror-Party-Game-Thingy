using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Queue-based player inventory for PvP games.
/// Only stores item IDs (FIFO order). Designed for simple pickup-place mechanics.
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
        networkItems.OnListChanged -= _ => SyncQueueWithNetworkList();
        base.OnDestroy();
    }

    // ============================================================
    // === PUBLIC METHODS =========================================
    // ============================================================

    public bool IsInventoryFull()
    {
        return itemQueue.Count >= maxInventorySlots;
    }

    [ServerRpc]
    public void AddItemServerRpc(int itemID)
    {
        if (!IsServer) return;

        if (IsInventoryFull())
        {
            Debug.LogWarning($"[PlayerInventory] Inventory full! Cannot add {itemID}");
            return;
        }

        itemQueue.Enqueue(itemID);
        networkItems.Add(itemID);
        Debug.Log($"[PlayerInventory - SERVER] Added item ID {itemID}. Now holding {itemQueue.Count}/{maxInventorySlots}");
    }

    [ServerRpc]
    public void PopItemServerRpc(ulong clientId)
    {
        if (!IsServer) return;

        if (itemQueue.Count == 0)
        {
            Debug.LogWarning($"[PlayerInventory - SERVER] Tried to pop but inventory empty!");
            return;
        }

        int removed = itemQueue.Dequeue();
        if (networkItems.Count > 0)
            networkItems.RemoveAt(0);

        Debug.Log($"[PlayerInventory - SERVER] Removed item ID {removed} for client {clientId}. Remaining {itemQueue.Count}");
    }

    public int PeekFrontItem()
    {
        return itemQueue.Count > 0 ? itemQueue.Peek() : -1;
    }

    public int GetItemCount()
    {
        return itemQueue.Count;
    }

    // ============================================================
    // === INTERNAL SYNC HELPERS =================================
    // ============================================================
    private void SyncQueueWithNetworkList()
    {
        itemQueue.Clear();
        foreach (int id in networkItems)
            itemQueue.Enqueue(id);
    }
}
