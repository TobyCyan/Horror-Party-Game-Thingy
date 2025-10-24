using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Unified pickup item component - attach to all pickable objects
/// Handles network synchronization and visual state
/// FIXED: Removed auto-despawn, added explicit server-controlled lifecycle
/// </summary>
public class NetworkPickupItem : NetworkBehaviour
{
    [Header("Item Settings")]
    [SerializeField] private string itemName;
    [SerializeField] private int itemID;

    [Header("Visual References")]
    [SerializeField] private GameObject visualObject;
    [SerializeField] private Collider itemCollider;

    [Header("Effects (Optional)")]
    [SerializeField] private ParticleSystem pickupEffect;
    [SerializeField] private AudioClip pickupSound;

    // Network state - these are the source of truth
    private NetworkVariable<bool> isPickedUp = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // Network variable for deployed state (for traps)
    private NetworkVariable<bool> isDeployed = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // Track if we're in the process of being picked up (prevents double-pickup)
    private bool pickupInProgress = false;

    public string ItemName => itemName;
    public int ItemID => itemID;
    public bool IsPickedUp => isPickedUp.Value;
    public bool IsDeployed => isDeployed.Value;
    public bool IsPickupInProgress => pickupInProgress;

    // =========================================================
    // === LIFECYCLE ===========================================
    // =========================================================

    private void Awake()
    {
        // Ensure we have required components
        if (itemCollider == null)
        {
            itemCollider = GetComponent<Collider>();
        }

        if (visualObject == null && transform.childCount > 0)
        {
            visualObject = transform.GetChild(0).gameObject;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Subscribe to state changes
        isPickedUp.OnValueChanged += OnPickupStateChanged;
        isDeployed.OnValueChanged += OnDeployedStateChanged;

        // Apply current state if joining late
        if (isPickedUp.Value)
        {
            HideItemImmediate();
        }

        // Validate setup
        ValidateSetup();

        Debug.Log($"[NetworkPickupItem] {itemName} (ID:{itemID}) spawned. NetworkObjectId: {NetworkObjectId}, IsPickedUp: {isPickedUp.Value}");
    }

    public override void OnNetworkDespawn()
    {
        isPickedUp.OnValueChanged -= OnPickupStateChanged;
        isDeployed.OnValueChanged -= OnDeployedStateChanged;
        base.OnNetworkDespawn();
    }

    // =========================================================
    // === PICKUP LOGIC ========================================
    // =========================================================

    /// <summary>
    /// Check if this item can be picked up
    /// </summary>
    public bool CanBePickedUp()
    {
        return !isPickedUp.Value && !isDeployed.Value && !pickupInProgress;
    }

    /// <summary>
    /// Server-only: Start the pickup process (mark as in-progress)
    /// This prevents double-pickup while we verify inventory
    /// </summary>
    public bool StartPickupProcess()
    {
        if (!IsServer)
        {
            Debug.LogError($"[NetworkPickupItem] StartPickupProcess() called on client!");
            return false;
        }

        if (!CanBePickedUp())
        {
            Debug.LogWarning($"[NetworkPickupItem] {itemName} cannot start pickup - already picked up or deployed");
            return false;
        }

        pickupInProgress = true;
        Debug.Log($"[NetworkPickupItem - SERVER] {itemName} pickup process started");
        return true;
    }

    /// <summary>
    /// Server-only: Complete the pickup process (mark as picked up)
    /// Only call this AFTER inventory has confirmed receipt
    /// </summary>
    public void CompletePickup()
    {
        if (!IsServer)
        {
            Debug.LogError($"[NetworkPickupItem] CompletePickup() called on client!");
            return;
        }

        if (isPickedUp.Value)
        {
            Debug.LogWarning($"[NetworkPickupItem] {itemName} already marked as picked up");
            return;
        }

        Debug.Log($"[NetworkPickupItem - SERVER] {itemName} pickup completed - setting network variable");
        isPickedUp.Value = true;
        pickupInProgress = false;
    }

    /// <summary>
    /// Server-only: Cancel the pickup process (rollback)
    /// Call this if inventory add failed
    /// </summary>
    public void CancelPickup()
    {
        if (!IsServer)
        {
            Debug.LogError($"[NetworkPickupItem] CancelPickup() called on client!");
            return;
        }

        Debug.LogWarning($"[NetworkPickupItem - SERVER] {itemName} pickup cancelled (rollback)");
        pickupInProgress = false;
    }

    /// <summary>
    /// Legacy method - now calls the new secure method
    /// </summary>
    public void PickupItem()
    {
        CompletePickup();
    }

    /// <summary>
    /// Server-only: Explicitly despawn this item
    /// Should only be called AFTER pickup is complete
    /// </summary>
    public void DespawnItem()
    {
        if (!IsServer)
        {
            Debug.LogError($"[NetworkPickupItem] DespawnItem() called on client!");
            return;
        }

        if (!isPickedUp.Value)
        {
            Debug.LogWarning($"[NetworkPickupItem] Attempting to despawn {itemName} but it's not marked as picked up!");
        }

        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            Debug.Log($"[NetworkPickupItem - SERVER] Despawning {itemName}");
            NetworkObject.Despawn(true);
        }
    }

    /// <summary>
    /// Server-only method to mark item as deployed (for traps)
    /// </summary>
    public void SetDeployed(bool deployed)
    {
        if (!IsServer)
        {
            Debug.LogError($"[NetworkPickupItem] SetDeployed() called on client!");
            return;
        }

        isDeployed.Value = deployed;
        Debug.Log($"[NetworkPickupItem - SERVER] {itemName} deployed state set to: {deployed}");
    }

    /// <summary>
    /// Called when pickup state changes on any client
    /// </summary>
    private void OnPickupStateChanged(bool oldValue, bool newValue)
    {
        if (!newValue) return; // Only handle true (picked up)

        Debug.Log($"[NetworkPickupItem] {itemName} pickup state changed to TRUE - hiding visuals");
        HideItemImmediate();
        PlayPickupEffects();
    }

    /// <summary>
    /// Called when deployed state changes
    /// </summary>
    private void OnDeployedStateChanged(bool oldValue, bool newValue)
    {
        Debug.Log($"[NetworkPickupItem] {itemName} deployed state changed: {oldValue} -> {newValue}");
    }

    /// <summary>
    /// Immediately hide the item visually
    /// </summary>
    private void HideItemImmediate()
    {
        // Hide visuals IMMEDIATELY
        if (visualObject != null)
        {
            visualObject.SetActive(false);
        }
        else
        {
            // If no specific visual object, hide all renderers
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                r.enabled = false;
            }
        }

        // Disable collider IMMEDIATELY
        if (itemCollider != null)
        {
            itemCollider.enabled = false;
        }
        else
        {
            // Disable all colliders if no specific one assigned
            Collider[] colliders = GetComponentsInChildren<Collider>();
            foreach (var c in colliders)
            {
                c.enabled = false;
            }
        }
    }

    // =========================================================
    // === EFFECTS & FEEDBACK ==================================
    // =========================================================

    private void PlayPickupEffects()
    {
        // Play particle effect
        if (pickupEffect != null)
        {
            pickupEffect.Play();
        }

        // Play sound (for all clients to hear)
        if (pickupSound != null)
        {
            // Create temporary audio source for 3D sound
            GameObject audioObj = new GameObject("PickupSound");
            audioObj.transform.position = transform.position;
            AudioSource audioSource = audioObj.AddComponent<AudioSource>();
            audioSource.clip = pickupSound;
            audioSource.spatialBlend = 1f; // Full 3D sound
            audioSource.maxDistance = 10f;
            audioSource.Play();

            // Destroy audio object after clip finishes
            Destroy(audioObj, pickupSound.length);
        }
    }

    // =========================================================
    // === VALIDATION ==========================================
    // =========================================================

    private void ValidateSetup()
    {
        if (visualObject == null)
        {
            Debug.LogWarning($"[NetworkPickupItem] {itemName} - No visual object assigned! Will hide all renderers on pickup.");
        }

        if (itemCollider == null)
        {
            Debug.LogWarning($"[NetworkPickupItem] {itemName} - No collider assigned! Will disable all colliders on pickup.");
        }

        if (itemID <= 0)
        {
            Debug.LogError($"[NetworkPickupItem] {itemName} has invalid item ID: {itemID}");
        }

        // Ensure NetworkObject exists
        if (GetComponent<NetworkObject>() == null)
        {
            Debug.LogError($"[NetworkPickupItem] {itemName} - Missing NetworkObject component! Item won't work properly!");
        }
    }

    // =========================================================
    // === DEBUG ===============================================
    // =========================================================

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            return;
        }

        // Show different colors based on state
        if (isPickedUp.Value)
        {
            Gizmos.color = Color.red; // Red = picked up
        }
        else if (pickupInProgress)
        {
            Gizmos.color = Color.magenta; // Magenta = pickup in progress
        }
        else if (isDeployed.Value)
        {
            Gizmos.color = Color.blue; // Blue = deployed
        }
        else
        {
            Gizmos.color = Color.green; // Green = available
        }

        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }

#if UNITY_EDITOR
    [ContextMenu("Force Reset State")]
    private void ForceResetState()
    {
        if (IsServer)
        {
            isPickedUp.Value = false;
            isDeployed.Value = false;
            pickupInProgress = false;

            // Show visuals
            if (visualObject != null)
                visualObject.SetActive(true);
            if (itemCollider != null)
                itemCollider.enabled = true;

            Debug.Log($"[NetworkPickupItem] {itemName} state reset");
        }
        else
        {
            Debug.LogWarning("Can only reset state on server");
        }
    }
#endif
}