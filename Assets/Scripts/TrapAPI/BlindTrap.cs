using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(BlindEffect))]
public class BlindTrap : TrapBase
{
    [Header("Who can trigger")]
    [SerializeField] private LayerMask playerMask;
    [SerializeField] private float silhouetteDuration = 5f;

    [Header("Visuals")]
    [SerializeField] private Renderer trapRenderer;
    [SerializeField] private Color armedColor = Color.red;
    [SerializeField] private Color disarmedColor = Color.gray;

    private BlindEffect blindEffect;
    private Material trapMaterial;

    protected override void Start()
    {
        base.Start();

        blindEffect = GetComponent<BlindEffect>();

        if (trapRenderer != null)
        {
            trapMaterial = trapRenderer.material;
        }

        // Subscribe to lifecycle events
        OnArmed += HandleArmed;
        OnDisarmed += HandleDisarmed;

        // IMPORTANT: Check current state and update visual immediately
        UpdateVisualColor();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Update visual when we spawn on network
        if (trapRenderer != null && trapMaterial == null)
        {
            trapMaterial = trapRenderer.material;
        }

        // Check state after network spawn
        UpdateVisualColor();
    }

    private void OnDestroy()
    {
        OnArmed -= HandleArmed;
        OnDisarmed -= HandleDisarmed;
    }

    private void UpdateVisualColor()
    {
        if (trapMaterial != null)
        {
            trapMaterial.color = IsArmed ? armedColor : disarmedColor;
            Debug.Log($"[BlindTrap] Visual updated - IsArmed: {IsArmed}, IsDeployed: {IsDeployed}, Color: {(IsArmed ? "Red" : "Gray")}");
        }
    }

    private void HandleArmed(ITrap trap)
    {
        UpdateVisualColor();
        Debug.Log("[BlindTrap] HandleArmed called - color should be red now");
    }

    private void HandleDisarmed(ITrap trap)
    {
        UpdateVisualColor();
        Debug.Log("[BlindTrap] HandleDisarmed called - color should be gray now");
    }

    // OnTriggerEnter fires on ALL clients where physics is simulated
    private void OnTriggerEnter(Collider other)
    {
        ProcessTrigger(other);
    }

    // PUBLIC method that can be called by TrapTriggerForwarder or OnTriggerEnter
    // This method RUNS ON EVERY CLIENT that detects the collision
    public void ProcessTrigger(Collider other)
    {
        // Check layer mask first (cheap check)
        if ((playerMask.value & (1 << other.gameObject.layer)) == 0)
        {
            Debug.Log($"[BlindTrap] {other.gameObject.name} not on player mask - ignoring");
            return;
        }

        Debug.Log($"[BlindTrap] Trigger detected by {other.gameObject.name} on {(IsServer ? "SERVER" : "CLIENT " + NetworkManager.Singleton.LocalClientId)}");

        // Get the NetworkObject from the colliding object
        NetworkObject playerNetObj = other.GetComponentInParent<NetworkObject>();
        if (playerNetObj == null)
        {
            Debug.LogWarning($"[BlindTrap] Colliding object {other.name} has no NetworkObject in parent!");
            return;
        }

        Debug.Log($"[BlindTrap] Found player NetworkObject {playerNetObj.NetworkObjectId}, IsOwner: {playerNetObj.IsOwner}");

        // IMPORTANT: Only the OWNER of the player should send the RPC
        // This prevents duplicate trigger requests from multiple clients
        if (playerNetObj.IsOwner)
        {
            Debug.Log($"[BlindTrap] Player owner detected collision - sending to server");

            if (IsServer)
            {
                // If we're already on the server, handle directly
                Debug.Log($"[BlindTrap] Processing on server directly");
                HandleTriggerOnServer(other.gameObject, playerNetObj.NetworkObjectId);
            }
            else
            {
                // If we're a client, notify the server via RPC
                Debug.Log($"[BlindTrap] Client sending RequestTriggerServerRpc for player {playerNetObj.NetworkObjectId}");
                RequestTriggerServerRpc(playerNetObj.NetworkObjectId);
            }
        }
        else
        {
            Debug.Log($"[BlindTrap] Not the owner of this player - ignoring to prevent duplicates");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestTriggerServerRpc(ulong playerNetworkObjectId, ServerRpcParams rpcParams = default)
    {
        Debug.Log($"[BlindTrap - SERVER] Received trigger request for player {playerNetworkObjectId} from client {rpcParams.Receive.SenderClientId}");

        // Find the player's NetworkObject
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerNetworkObjectId, out NetworkObject playerNetObj))
        {
            Debug.LogWarning($"[BlindTrap - SERVER] Could not find NetworkObject {playerNetworkObjectId}");
            return;
        }

        Debug.Log($"[BlindTrap - SERVER] Found player NetworkObject, processing trigger");

        // Process the trigger on server
        HandleTriggerOnServer(playerNetObj.gameObject, playerNetworkObjectId);
    }

    private void HandleTriggerOnServer(GameObject instigatorObject, ulong playerNetworkObjectId)
    {
        // Only process on server
        if (!IsServer)
        {
            Debug.LogWarning("[BlindTrap] HandleTriggerOnServer called on client - this shouldn't happen!");
            return;
        }

        Debug.Log($"[BlindTrap - SERVER] Processing trigger - IsDeployed: {IsDeployed}, IsArmed: {IsArmed}, CanTrigger: {CanTrigger()}");

        if (!CanTrigger())
        {
            Debug.Log($"[BlindTrap - SERVER] Cannot trigger - IsDeployed: {IsDeployed}, IsArmed: {IsArmed}, Cooldown ok: {(Time.time - lastTriggerTime) >= Cooldown}");
            return;
        }

        Debug.Log($"[BlindTrap - SERVER] ✓ Triggering trap for player {playerNetworkObjectId}!");

        var ctx = new TrapTriggerContext
        {
            source = TrapTriggerSource.Player,
            instigator = instigatorObject,
            hitPoint = transform.position,
            hitNormal = Vector3.up
        };

        Trigger(ctx);
    }

    protected override void OnTriggerCore(TrapTriggerContext ctx)
    {
        if (!IsServer)
        {
            Debug.LogWarning("[BlindTrap] OnTriggerCore called on client!");
            return;
        }

        var player = ctx.instigator.GetComponentInParent<Player>();
        if (player == null)
        {
            Debug.LogWarning("[BlindTrap] Could not find Player component!");
            return;
        }

        Debug.Log($"[BlindTrap - SERVER] Applying blind effect to player {player.clientId}");

        blindEffect.Apply(player);

        if (player.TryGetComponent(out PlayerSilhouette silhouette))
        {
            silhouette.ShowForSecondsRpc(silhouetteDuration);
        }

        if (OneShot)
        {
            Disarm();
        }
    }
}