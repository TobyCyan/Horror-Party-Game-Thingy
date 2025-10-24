using Unity.Netcode;
using UnityEngine;

public abstract class TrapBase : NetworkBehaviour, ITrap
{
    [Header("Trap")]
    [SerializeField] TrapPlacementKind placement = TrapPlacementKind.Default;
    [SerializeField] float cooldown = 0.5f;
    [SerializeField] bool oneShot = false;

    public NetworkVariable<ulong> ownerClientId = new NetworkVariable<ulong>();

    // Network variables for state sync
    private NetworkVariable<bool> netIsDeployed = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> netIsArmed = new NetworkVariable<bool>(false);

    protected GameObject owner;
    protected float lastTriggerTime = -999f;

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
        // Subscribe will happen in OnNetworkSpawn
    }

    protected virtual void Start()
    {
        if (NetworkObject == null || !NetworkObject.IsSpawned) return;

        if (placement == TrapPlacementKind.Auto || placement == TrapPlacementKind.Default)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                netIsDeployed.Value = true;
                Arm();
            }
        }
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
    }

    public override void OnNetworkDespawn()
    {
        netIsDeployed.OnValueChanged -= OnDeployedChanged;
        netIsArmed.OnValueChanged -= OnArmedChanged;
        base.OnNetworkDespawn();
    }

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

        // Extract owner client ID
        ulong ownerClientIdValue = 0;
        if (owner != null && owner.TryGetComponent<Player>(out var playerComponent))
        {
            ownerClientIdValue = playerComponent.OwnerClientId;
        }

        gameObject.SetActive(true);

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

        bool isServer = NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
        if (isServer)
        {
            // TrapScoreManager integration - award score to trap owner (backward compatible)
            try
            {
                if (TrapScoreManager.Instance != null)
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