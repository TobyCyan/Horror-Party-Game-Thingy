using Unity.Netcode;
using UnityEngine;

public abstract class TrapBase : NetworkBehaviour, ITrap
{
    [Header("Trap")]
    [SerializeField] TrapPlacementKind placement = TrapPlacementKind.Default;
    [SerializeField] float cooldown = 0.5f;
    [SerializeField] bool oneShot = false;

    [Header("Who can trigger")]
    [SerializeField] protected LayerMask triggerMask;

    [Header("Base Visuals")]
    [SerializeField] protected GameObject armedVisuals; 
    [SerializeField] protected GameObject disarmedVisuals; 
    [SerializeField] protected Renderer trapRenderer;
    [SerializeField] protected Color armedColor = Color.red;
    [SerializeField] protected Color disarmedColor = Color.gray;

    protected Material trapMaterial;

    public NetworkVariable<ulong> ownerClientId = new NetworkVariable<ulong>();

    // Network variables for state sync
    private NetworkVariable<bool> netIsDeployed = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> netIsArmed = new NetworkVariable<bool>(false);

    protected GameObject owner;
    protected float lastTriggerTime = -999f;

    [Header("SFX")]
    [SerializeField] private AudioSettings deployedSfxSettings;
    [SerializeField] private AudioSettings triggeredSfxSettings;

    // Public properties (ITrap interface)
    public TrapPlacementKind Placement => placement;
    public bool IsDeployed => netIsDeployed.Value;
    public bool IsArmed => netIsArmed.Value;
    public float Cooldown => cooldown;
    public bool OneShot => oneShot;

    // Events (ITrap interface)
    public event System.Action<ITrap> OnDeployed, OnArmed, OnDisarmed;
    public event System.Action<ITrap, TrapTriggerContext> OnTriggered;
    public static event System.Action<ITrap, TrapTriggerContext> StaticOnTriggered;

    // Lifecycle methods
    protected virtual void Awake()
    {
        if (trapRenderer != null)
        {
            trapMaterial = trapRenderer.material;
        }
        // Subscribe will happen in OnNetworkSpawn
    }

    protected virtual void Start()
    {
        // Subscribe to our own events to update visuals.
        OnArmed += HandleArmed;
        OnDisarmed += HandleDisarmed;

        if (NetworkObject == null || !NetworkObject.IsSpawned)
        {
            SetVisualState(IsArmed); //Set initial visual state
            return;
        }

        if (placement == TrapPlacementKind.Auto || placement == TrapPlacementKind.Default)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                netIsDeployed.Value = true;
                Arm();
            }
        }

        SetVisualState(IsArmed); //Set initial visual state
    }

    public override void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks when the object is destroyed
        OnArmed -= HandleArmed;
        OnDisarmed -= HandleDisarmed;
        Debug.Log($"[TrapBase] OnDestroy called");

        // Clean up the material instance we created in Awake()
        if (trapMaterial != null)
        {
            Destroy(trapMaterial);
        }
        base.OnDestroy();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Subscribe to value changes
        netIsDeployed.OnValueChanged += OnDeployedChanged;
        netIsArmed.OnValueChanged += OnArmedChanged;

        // Fire events for current state (for late-joining clients)
        if (netIsDeployed.Value)
        {
            OnDeployed?.Invoke(this);
            Debug.Log($"[TrapBase] OnNetworkSpawn - Already deployed");
        }

        if (netIsArmed.Value)
        {
            OnArmed?.Invoke(this);
            Debug.Log($"[TrapBase] OnNetworkSpawn - Already armed");
        }
        else
        {
            SetVisualState(false); // Ensure visuals are correct if disarmed
        }
    }

    public override void OnNetworkDespawn()
    {
        netIsDeployed.OnValueChanged -= OnDeployedChanged;
        netIsArmed.OnValueChanged -= OnArmedChanged;
        base.OnNetworkDespawn();
    }

    #region Visual Handlers (Armed/Disarmed)
    private void HandleArmed(ITrap trap)
    {
        SetVisualState(true);
        Debug.Log($"[TrapBase] HandleArmed - Visuals updated to ARMED");
    }

    private void HandleDisarmed(ITrap trap)
    {
        SetVisualState(false);
        Debug.Log($"[TrapBase] HandleDisarmed - Visuals updated to DISARMED");
    }

    protected virtual void SetVisualState(bool isArmed)
    {
        // 1. Handle GameObject toggling (from BlindTrap's SetTrapVisualState)
        if (armedVisuals != null)
        {
            armedVisuals.SetActive(isArmed);
        }
        if (disarmedVisuals != null)
        {
            disarmedVisuals.SetActive(!isArmed);
        }

        // 2. Handle color change (from BlindTrap's UpdateVisualColor)
        if (trapMaterial != null)
        {
            Color targetColor = isArmed ? armedColor : disarmedColor;
            trapMaterial.color = targetColor;
            Debug.Log($"[TrapBase] Color updated to {(isArmed ? armedColor.ToString() : disarmedColor.ToString())}");
        }
    }
    #endregion

    // Network variable change handlers
    private void OnDeployedChanged(bool oldValue, bool newValue)
    {
        if (newValue && !oldValue)
        {
            OnDeployed?.Invoke(this);
        }
    }

    private void OnArmedChanged(bool oldValue, bool newValue)
    {
        Debug.Log($"[TrapBase] Armed state changed from {oldValue} to {newValue}");

        if (newValue && !oldValue)
        {
            OnArmed?.Invoke(this);
        }
        else if (!newValue && oldValue)
        {
            OnDisarmed?.Invoke(this);
        }
    }

    // Public API: Deploy trap
    public virtual void Deploy(Vector3 pos, Quaternion rot, GameObject ownerGO)
    {
        if (placement != TrapPlacementKind.Manual) return;

        Debug.Log($"[TrapBase] Deploy called");

        transform.SetPositionAndRotation(pos, rot);
        owner = ownerGO;
        Player playerComponent = null;

        // Extract owner client ID
        ulong ownerClientIdValue = 0;
        if (owner != null && owner.TryGetComponent(out playerComponent))
        {
            ownerClientIdValue = playerComponent.OwnerClientId;
        }

        gameObject.SetActive(true);

        if (playerComponent != null && !deployedSfxSettings.IsNullOrEmpty())
        {
            playerComponent.PlayLocalAudio(deployedSfxSettings);
        }

        // Check if we're server
        bool isServer = NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;

        if (isServer)
        {
            ownerClientId.Value = ownerClientIdValue;
            netIsDeployed.Value = true;

            NetworkPickupItem pickupItem = GetComponent<NetworkPickupItem>();
            if (pickupItem != null)
            {
                pickupItem.SetDeployed(true);
            }

            // TrapScoreManager integration (backward compatible)
            try
            {
                if (TrapScoreManager.Instance != null)
                {
                    TrapScoreManager.Instance.UpdateDebugScores();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[TrapBase] TrapScoreManager not available: {e.Message}");
            }

            Debug.Log("[TrapBase] About to arm trap...");
            Arm();
            Debug.Log($"[TrapBase] Trap Armed - IsArmed: {netIsArmed.Value}");
        }
        else
        {
            Debug.Log($"[TrapBase] Client requesting server deploy");
            RequestDeployServerRpc(pos, rot, ownerClientIdValue);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestDeployServerRpc(Vector3 pos, Quaternion rot, ulong ownerId)
    {
        bool isServer = NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
        if (!isServer) return;

        transform.SetPositionAndRotation(pos, rot);
        gameObject.SetActive(true);

        ownerClientId.Value = ownerId;
        netIsDeployed.Value = true;

        NetworkPickupItem pickupItem = GetComponent<NetworkPickupItem>();
        if (pickupItem != null)
        {
            pickupItem.SetDeployed(true);
        }

        // TrapScoreManager integration (backward compatible)
        try
        {
            if (TrapScoreManager.Instance != null)
            {
                TrapScoreManager.Instance.UpdateDebugScores();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[TrapBase] TrapScoreManager not available: {e.Message}");
        }

        Debug.Log($"[TrapBase] About to arm trap...");
        Arm();
        Debug.Log($"[TrapBase] After Arm() - IsArmed: {netIsArmed.Value}");
    }

    // --- MOVED FROM BLINDTRAP (AND GENERALIZED) ---
    // This RPC is now generic. It doesn't know about "Player",
    // it just gets an instigator's NetworkObject ID.
    [ServerRpc(RequireOwnership = false)]
    private void RequestTriggerServerRpc(ulong instigatorNetworkId, Vector3 hitPoint)
    {
        bool isServer = NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
        Debug.Log($"[TrapBase - SERVER RPC] Received request for instigator {instigatorNetworkId}, IsServer: {isServer}");

        if (!isServer)
        {
            Debug.LogError("[TrapBase - SERVER RPC] Not on server!");
            return;
        }

        if (!CanTrigger())
        {
            Debug.LogWarning($"[TrapBase - SERVER RPC] Cannot trigger");
            return;
        }

        // Get the generic NetworkObject
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(instigatorNetworkId, out NetworkObject instigatorNetObj))
        {
            Debug.LogError($"[TrapBase - SERVER RPC] Instigator {instigatorNetworkId} not found!");
            return;
        }

        // NO MORE PLAYER CHECK HERE
        // The instigator is just the GameObject from the NetworkObject.
        // The child class's OnTriggerCore is responsible for
        // getting any specific components (like Player).

        var ctx = new TrapTriggerContext
        {
            source = TrapTriggerSource.Player, // Or a more generic source if you have one
            instigator = instigatorNetObj.gameObject, // Pass the GameObject
            hitPoint = hitPoint,
            hitNormal = Vector3.up
        };

        Debug.Log($"[TrapBase - SERVER RPC] Triggering for instigator {instigatorNetObj.OwnerClientId}");
        Trigger(ctx);
    }

    // Public API: Arm trap
    public virtual void Arm()
    {
        bool isServer = NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
        if (isServer)
        {
            Debug.Log($"[TrapBase] Arm() called - current armed state: {netIsArmed.Value}");
            netIsArmed.Value = true;
            Debug.Log($"[TrapBase] Arm() completed - new armed state: {netIsArmed.Value}");
        }
        else
        {
            Debug.LogWarning($"[TrapBase] Arm() called on client - ignored!");
        }
    }

    // Public API: Disarm trap
    public virtual void Disarm()
    {
        bool isServer = NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
        if (isServer)
        {
            netIsArmed.Value = false;
        }
    }

    // Public API: Check if trap can trigger
    public virtual bool CanTrigger() =>
        IsDeployed && IsArmed && (Time.time - lastTriggerTime) >= cooldown;

    //Public API: Handle trigger enter from TrapTriggerForwarder
    public virtual void HandleTriggerEnter(Collider other)
    {
        bool isSpawnedCheck = NetworkObject != null && NetworkObject.IsSpawned;
        Debug.Log($"[TrapBase] HandleTriggerEnter - IsSpawned: {isSpawnedCheck}, CanTrigger: {CanTrigger()}");

        if (!isSpawnedCheck)
        {
            Debug.LogWarning("[TrapBase] Not spawned yet");
            return;
        }

        if (!CanTrigger())
        {
            Debug.Log($"[TrapBase] Cannot trigger - IsDeployed: {IsDeployed}, IsArmed: {IsArmed}");
            return;
        }

        // Use the new virtual method to check for a valid instigator
        if (IsValidTrigger(other, out ulong instigatorNetworkId))
        {
            Debug.Log($"[TrapBase] Valid trigger by network object {instigatorNetworkId}");
            RequestTriggerServerRpc(instigatorNetworkId, other.ClosestPoint(transform.position));
        }
        else
        {
            Debug.Log($"[TrapBase] Invalid trigger: {other.gameObject.name}");
        }
    }

    // --- ADDED: VIRTUAL VALIDATION METHOD ---
    // Child classes can override this to be more specific.
    // The base implementation just checks the layer mask and finds *any* NetworkObject.
    protected virtual bool IsValidTrigger(Collider other, out ulong instigatorNetworkId)
    {
        instigatorNetworkId = 0;

        // 1. Check layer mask
        if ((triggerMask.value & (1 << other.gameObject.layer)) == 0)
        {
            Debug.Log($"[TrapBase] Wrong layer: {other.gameObject.layer}");
            return false;
        }

        // 2. Find the root NetworkObject
        var netObj = other.GetComponentInParent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogWarning($"[TrapBase] Trigger by {other.gameObject.name} on correct layer, but no NetworkObject found in parents.");
            return false;
        }

        instigatorNetworkId = netObj.NetworkObjectId;
        return true;
    }

    // Public API: Trigger trap
    public void Trigger(in TrapTriggerContext ctx)
    {
        if (!CanTrigger()) return;

        lastTriggerTime = Time.time;

        // Fire events
        OnTriggered?.Invoke(this, ctx);
        StaticOnTriggered?.Invoke(this, ctx);

        // Call derived class implementation
        OnTriggerCore(ctx);

        Debug.Log($"[TrapBase] Trap triggered by {ctx.instigator?.name ?? "unknown"}");
        if (!triggeredSfxSettings.IsNullOrEmpty())
        {
            if (ctx.instigator.TryGetComponent<Player>(out var playerComponent))
            {
                playerComponent.PlayLocalAudio(triggeredSfxSettings);
            }
        }

        bool isServer = NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
        if (isServer)
        {
            // TrapScoreManager integration - award score to trap owner (backward compatible)
            try
            {
                if (TrapScoreManager.Instance != null && placement == TrapPlacementKind.Manual)
                {
                    TrapScoreManager.Instance.AwardTrapScore(ownerClientId.Value);
                    TrapScoreManager.Instance.UpdateDebugScores();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[TrapBase] TrapScoreManager not available: {e.Message}");
            }

            // Handle one-shot traps
            if (oneShot)
            {
                if (NetworkObject != null && NetworkObject.IsSpawned)
                {
                    NetworkObject.Despawn(true);
                }
                else
                {
                    Destroy(gameObject);
                }
            }
        }
    }

    // Abstract method - must be implemented by derived classes
    protected abstract void OnTriggerCore(TrapTriggerContext ctx);
}