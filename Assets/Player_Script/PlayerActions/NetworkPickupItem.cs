using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Unified pickup item component - attach to all pickable objects
/// Handles network synchronization and visual state
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
    [SerializeField] private float despawnDelay = 0.1f; // Quick despawn after pickup

    // Network state
    private NetworkVariable<bool> isPickedUp = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public string ItemName => itemName;
    public int ItemID => itemID;
    public bool IsPickedUp => isPickedUp.Value;

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

        // Subscribe to pickup state changes
        isPickedUp.OnValueChanged += OnPickupStateChanged;

        // Apply current state if joining late
        if (isPickedUp.Value)
        {
            HideItemImmediate();
        }

        // Validate setup
        ValidateSetup();

        Debug.Log($"[NetworkPickupItem] {itemName} (ID:{itemID}) spawned. NetworkObjectId: {NetworkObjectId}");
    }

    public override void OnNetworkDespawn()
    {
        isPickedUp.OnValueChanged -= OnPickupStateChanged;
        base.OnNetworkDespawn();
    }

    // =========================================================
    // === PICKUP LOGIC ========================================
    // =========================================================

    /// <summary>
    /// Server-only method to mark item as picked up
    /// </summary>
    public void PickupItem()
    {
        if (!IsServer)
        {
            Debug.LogError($"[NetworkPickupItem] PickupItem() called on client! This should only be called on server.");
            return;
        }

        if (isPickedUp.Value)
        {
            Debug.LogWarning($"[NetworkPickupItem] {itemName} already picked up!");
            return;
        }

        Debug.Log($"[NetworkPickupItem - SERVER] Picking up {itemName}");
        isPickedUp.Value = true;
    }

    /// <summary>
    /// Called when pickup state changes on any client
    /// </summary>
    private void OnPickupStateChanged(bool oldValue, bool newValue)
    {
        if (!newValue) return; // Only handle pickup, not un-pickup

        Debug.Log($"[NetworkPickupItem] {itemName} picked up - hiding immediately");

        HideItemImmediate();

        // Server despawns after very short delay
        if (IsServer)
        {
            Invoke(nameof(DespawnItem), despawnDelay);
        }
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

        // Play pickup effects
        PlayPickupEffects();
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
    // === DESPAWN =============================================
    // =========================================================

    private void DespawnItem()
    {
        if (!IsServer) return;

        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            Debug.Log($"[NetworkPickupItem - SERVER] Despawning {itemName}");
            NetworkObject.Despawn(true);
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
            Gizmos.color = Color.red;
        }
        else
        {
            Gizmos.color = Color.green;
        }

        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}