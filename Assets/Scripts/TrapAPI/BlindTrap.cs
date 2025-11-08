// BlindTrap.cs - Updated to use player's BlindEffect
using UnityEngine;
using Unity.Netcode;

public class BlindTrap : TrapBase
{
    [Header("Who can trigger")]
    [SerializeField] private LayerMask playerMask;
    [SerializeField] private float blindDuration = 5f;
    [SerializeField] private float silhouetteDuration = 5f;

    [Header("Visuals")]
    [SerializeField] private Renderer trapRenderer;
    [SerializeField] private Material armedMaterial;
    [SerializeField] private Material disarmedMaterial;

    protected override void Awake()
    {
        base.Awake();

        if (trapRenderer != null)
        {
            trapRenderer.material = disarmedMaterial;
        }
    }

    protected override void Start()
    {
        base.Start();

        // Subscribe to lifecycle events
        OnArmed += HandleArmed;
        OnDisarmed += HandleDisarmed;

        // Initial color update
        UpdateVisualColor();

        Debug.Log($"[BlindTrap] Start complete");
    }

    private void OnDestroy()
    {
        OnArmed -= HandleArmed;
        OnDisarmed -= HandleDisarmed;
    }

    private void HandleArmed(ITrap trap)
    {
        UpdateVisualColor();
        Debug.Log($"[BlindTrap] HandleArmed - Updating color to RED");
    }

    private void HandleDisarmed(ITrap trap)
    {
        UpdateVisualColor();
        Debug.Log($"[BlindTrap] HandleDisarmed - Updating color to GRAY");
    }

    private void UpdateVisualColor()
    {
        if (trapRenderer != null)
        {
            Material targetMaterial = IsArmed ? armedMaterial : disarmedMaterial;
            trapRenderer.material = targetMaterial;
            Debug.Log($"[BlindTrap] Color updated to {(IsArmed ? "RED (armed)" : "GRAY (disarmed)")}");
        }
    }

    public override void HandleTriggerEnter(Collider other)
    {
        bool isSpawnedCheck = NetworkObject != null && NetworkObject.IsSpawned;

        Debug.Log($"[BlindTrap] HandleTriggerEnter - IsSpawned: {isSpawnedCheck}, CanTrigger: {CanTrigger()}");

        if (!isSpawnedCheck)
        {
            Debug.LogWarning("[BlindTrap] Not spawned yet");
            return;
        }

        if (!CanTrigger())
        {
            Debug.Log($"[BlindTrap] Cannot trigger - IsDeployed: {IsDeployed}, IsArmed: {IsArmed}");
            return;
        }

        if ((playerMask.value & (1 << other.gameObject.layer)) == 0)
        {
            Debug.Log($"[BlindTrap] Wrong layer: {other.gameObject.layer}");
            return;
        }

        var player = other.GetComponentInParent<Player>();
        if (player == null)
        {
            Debug.LogWarning($"[BlindTrap] No Player component on {other.gameObject.name}");
            return;
        }

        Debug.Log($"[BlindTrap] Valid trigger by player {player.OwnerClientId}");
        RequestTriggerServerRpc(player.NetworkObjectId, other.ClosestPoint(transform.position));
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestTriggerServerRpc(ulong playerNetworkId, Vector3 hitPoint)
    {
        bool isServer = NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;

        Debug.Log($"[BlindTrap - SERVER RPC] Received request for player {playerNetworkId}, IsServer: {isServer}");

        if (!isServer)
        {
            Debug.LogError("[BlindTrap - SERVER RPC] Not on server!");
            return;
        }

        if (!CanTrigger())
        {
            Debug.LogWarning($"[BlindTrap - SERVER RPC] Cannot trigger");
            return;
        }

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerNetworkId, out NetworkObject playerNetObj))
        {
            Debug.LogError($"[BlindTrap - SERVER RPC] Player {playerNetworkId} not found!");
            return;
        }

        Player player = playerNetObj.GetComponent<Player>();
        if (player == null)
        {
            Debug.LogError($"[BlindTrap - SERVER RPC] No Player component!");
            return;
        }

        var ctx = new TrapTriggerContext
        {
            source = TrapTriggerSource.Player,
            instigator = player.gameObject,
            hitPoint = hitPoint,
            hitNormal = Vector3.up
        };

        Debug.Log($"[BlindTrap - SERVER RPC] ✅ Triggering for player {player.OwnerClientId}");
        Trigger(ctx);
    }

    protected override void OnTriggerCore(TrapTriggerContext ctx)
    {
        bool isServer = NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
        Debug.Log($"[BlindTrap] OnTriggerCore - IsServer: {isServer}");

        if (!isServer)
        {
            Debug.LogError("[BlindTrap] OnTriggerCore called on client!");
            return;
        }

        var player = ctx.instigator.GetComponent<Player>();
        if (player == null)
        {
            Debug.LogError("[BlindTrap] No Player in context!");
            return;
        }

        Debug.Log($"[BlindTrap - SERVER] Got player {player.OwnerClientId}, checking for BlindEffect...");

        // Get BlindEffect from the PLAYER
        BlindEffect blindEffect = player.GetComponent<BlindEffect>();
        if (blindEffect != null)
        {
            Debug.Log($"[BlindTrap - SERVER] BlindEffect found, calling Apply({blindDuration})");
            blindEffect.Apply(blindDuration);
            Debug.Log($"[BlindTrap - SERVER] ✅ Blind effect applied");
        }
        else
        {
            Debug.LogError($"[BlindTrap - SERVER] ❌ No BlindEffect component on player! Did you add it to the player prefab?");
        }

        // Apply silhouette
        if (player.TryGetComponent(out PlayerSilhouette silhouette))
        {
            silhouette.ShowForSecondsRpc(silhouetteDuration);
            Debug.Log($"[BlindTrap - SERVER] ✅ Silhouette applied");
        }
    }

    public override void Deploy(Vector3 pos, Quaternion rot, GameObject ownerGO)
    {
        base.Deploy(pos, rot, ownerGO);

        // Update color after deployment
        UpdateVisualColor();
        Debug.Log($"[BlindTrap] Deploy complete - IsArmed: {IsArmed}");
    }
}