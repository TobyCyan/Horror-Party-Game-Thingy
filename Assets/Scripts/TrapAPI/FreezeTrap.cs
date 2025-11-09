using UnityEngine;
using Unity.Netcode;

public class FreezeTrap : TrapBase
{
    [Header("Trap Settings")]
    [SerializeField] private float freezeDuration = 3f;
    [SerializeField] private float silhouetteDuration = 3f;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();
        Debug.Log($"[FreezeTrap] Start complete");
    }

    protected override bool IsValidTrigger(Collider other, out ulong instigatorNetworkId)
    {
        instigatorNetworkId = 0;

        // 1. Check layer mask (using the 'triggerMask' field from TrapBase)
        if ((triggerMask.value & (1 << other.gameObject.layer)) == 0)
        {
            Debug.Log($"[FreezeTrap] Wrong layer: {other.gameObject.layer}");
            return false;
        }

        // 2. Add our specific check for a 'Player' component
        var player = other.GetComponentInParent<Player>();
        if (player == null)
        {
            Debug.LogWarning($"[FreezeTrap] Valid layer but no Player component on {other.gameObject.name}");
            return false;
        }

        // 3. We found a valid Player, return its ID
        instigatorNetworkId = player.NetworkObjectId;
        return true;
    }

    protected override void OnTriggerCore(TrapTriggerContext ctx)
    {
        bool isServer = NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
        Debug.Log($"[FreezeTrap] OnTriggerCore - IsServer: {isServer}");

        if (!isServer)
        {
            Debug.LogError("[FreezeTrap] OnTriggerCore called on client!");
            return;
        }

        var player = ctx.instigator.GetComponent<Player>();
        if (player == null)
        {
            Debug.LogError("[FreezeTrap] No Player in context!");
            return;
        }

        Debug.Log($"[FreezeTrap - SERVER] Got player {player.OwnerClientId}, applying freeze effect...");

        // Call the Player's Stun method (internally called "Stun" but represents freezing)
        player.Stun(freezeDuration);
        Debug.Log($"[FreezeTrap - SERVER] ✅ Freeze effect applied for {freezeDuration} seconds");

        // Apply silhouette
        if (player.TryGetComponent(out PlayerSilhouette silhouette))
        {
            silhouette.ShowForSecondsRpc(silhouetteDuration);
            Debug.Log($"[FreezeTrap - SERVER] ✅ Silhouette applied");
        }
    }

    public override void Deploy(Vector3 pos, Quaternion rot, GameObject ownerGO)
    {
        base.Deploy(pos, rot, ownerGO);
        Debug.Log($"[FreezeTrap] Deploy complete - IsArmed: {IsArmed}");
    }
}
