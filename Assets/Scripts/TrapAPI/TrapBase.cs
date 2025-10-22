using UnityEngine;
using Unity.Netcode;
using UnityEngine;

public abstract class TrapBase : NetworkBehaviour, ITrap
{
    [Header("Trap")]
    [SerializeField] TrapPlacementKind placement = TrapPlacementKind.Default;
    [SerializeField] float cooldown = 0.5f;
    [SerializeField] bool oneShot = false;

    public NetworkVariable<ulong> ownerClientId = new();

    // Make these NetworkVariables so they sync across network
    private NetworkVariable<bool> netIsDeployed = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> netIsArmed = new NetworkVariable<bool>(false);

    protected GameObject owner;
    protected float lastTriggerTime = -999f;

    public TrapPlacementKind Placement => placement;
    public bool IsDeployed => netIsDeployed.Value;
    public bool IsArmed => netIsArmed.Value;
    public float Cooldown => cooldown;
    public bool OneShot => oneShot;

    public event System.Action<ITrap> OnDeployed, OnArmed, OnDisarmed;
    public event System.Action<ITrap, TrapTriggerContext> OnTriggered;
    public static event System.Action<ITrap, TrapTriggerContext> StaticOnTriggered;

    // ----- lifecycle -----
    protected virtual void Start()
    {
        // Wait for network spawn
        if (!IsSpawned) return;

        if (placement == TrapPlacementKind.Auto || placement == TrapPlacementKind.Default)
        {
            if (IsServer)
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
            Debug.Log($"[TrapBase] OnNetworkSpawn - Already deployed, firing OnDeployed event");
        }

        if (netIsArmed.Value)
        {
            OnArmed?.Invoke(this);
            Debug.Log($"[TrapBase] OnNetworkSpawn - Already armed, firing OnArmed event");
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
    public override void OnNetworkDespawn()
    {
        netIsDeployed.OnValueChanged -= OnDeployedChanged;
        netIsArmed.OnValueChanged -= OnArmedChanged;
        base.OnNetworkDespawn();
    }

    private void OnDeployedChanged(bool oldValue, bool newValue)
    {
        if (newValue && !oldValue)
        {
            OnDeployed?.Invoke(this);
        }
    }

    public virtual void Deploy(Vector3 pos, Quaternion rot, GameObject ownerGO)
    {
        if (placement != TrapPlacementKind.Manual) return;

        Debug.Log($"[TrapBase] Deploy called - IsServer: {IsServer}");

        transform.SetPositionAndRotation(pos, rot);
        owner = ownerGO;

        // Extract owner client ID
        ulong ownerClientIdValue = 0;
        if (owner != null && owner.TryGetComponent<Player>(out var playerComponent))
        {
            ownerClientIdValue = playerComponent.clientId;
        }

        gameObject.SetActive(true);

        // If we're the server, deploy directly
        if (IsServer)
        {
            ownerClientId.Value = ownerClientIdValue;
            netIsDeployed.Value = true;

            NetworkPickupItem pickupItem = GetComponent<NetworkPickupItem>();
            if (pickupItem != null)
            {
                pickupItem.SetDeployed(true);
            }

            // Wrap in try-catch so it doesn't break Arm()
            try
            {
                if (TrapScoreManager.Instance != null)
                {
                    TrapScoreManager.Instance.UpdateDebugScores();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[TrapBase] TrapScoreManager error: {e.Message}");
            }

            // ARM THE TRAP - This will now be reached!
            Debug.Log("About to arm trap...");
            Arm();
            Debug.Log("Trap Armed");
        }
        else
        {
            // If we're client, request server to deploy
            Debug.Log($"[TrapBase] Client requesting server deploy");
            RequestDeployServerRpc(pos, rot, ownerClientIdValue);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestDeployServerRpc(Vector3 pos, Quaternion rot, ulong ownerId)
    {
        if (!IsServer) return;

        transform.SetPositionAndRotation(pos, rot);
        gameObject.SetActive(true);

        // Set owner
        ownerClientId.Value = ownerId;

        // Deploy on server
        netIsDeployed.Value = true;
        Debug.Log($"[TrapBase] Deploy - Set IsDeployed to true");

        // Mark NetworkPickupItem as deployed
        NetworkPickupItem pickupItem = GetComponent<NetworkPickupItem>();
        if (pickupItem != null)
        {
            pickupItem.SetDeployed(true);
        }

        // Wrap in try-catch
        try
        {
            if (TrapScoreManager.Instance != null)
            {
                TrapScoreManager.Instance.UpdateDebugScores();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[TrapBase] TrapScoreManager error in RPC: {e.Message}");
        }

        // THIS WILL NOW BE REACHED!
        Debug.Log($"[TrapBase] About to arm trap...");
        Arm();
        Debug.Log($"[TrapBase] After Arm() - IsArmed should be true: {netIsArmed.Value}");
    }

    public virtual void Arm()
    {
        if (IsServer)
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

    public virtual void Disarm()
    {
        if (IsServer)
        {
            netIsArmed.Value = false;
        }
    }

    public virtual bool CanTrigger() =>
        IsDeployed && IsArmed && (Time.time - lastTriggerTime) >= cooldown;

    public void Trigger(in TrapTriggerContext ctx)
    {
        if (!CanTrigger()) return;

        lastTriggerTime = Time.time;
        OnTriggered?.Invoke(this, ctx);
        StaticOnTriggered?.Invoke(this, ctx);
        OnTriggerCore(ctx);

        if (IsServer)
        {
            if (TrapScoreManager.Instance != null)
            {
                TrapScoreManager.Instance.AwardTrapScore(ownerClientId.Value);
                TrapScoreManager.Instance.UpdateDebugScores();
            }

            if (oneShot)
            {
                // Properly despawn networked object
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

    // implement trap-specific effect (damage, VFX, etc.)
    protected abstract void OnTriggerCore(TrapTriggerContext ctx);
}