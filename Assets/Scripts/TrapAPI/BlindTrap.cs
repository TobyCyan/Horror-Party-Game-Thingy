// BlindTrap.cs - Updated to use player's BlindEffect
using UnityEngine;
using Unity.Netcode;

public class BlindTrap : TrapBase
{
    [Header("Trap Settings")]
    [SerializeField] private float blindDuration = 5f;
    [SerializeField] private float silhouetteDuration = 5f;


    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();
        Debug.Log($"[BlindTrap] Start complete");
    }

    protected override bool IsValidTrigger(Collider other, out ulong instigatorNetworkId)
    {
        instigatorNetworkId = 0;

        // 1. Check layer mask (using the 'triggerMask' field from TrapBase)
        if ((triggerMask.value & (1 << other.gameObject.layer)) == 0)
        {
            Debug.Log($"[BlindTrap] Wrong layer: {other.gameObject.layer}");
            return false;
        }

        // 2. Add our specific check for a 'Player' component
        var player = other.GetComponentInParent<Player>();
        if (player == null)
        {
            Debug.LogWarning($"[BlindTrap] Valid layer but no Player component on {other.gameObject.name}");
            return false;
        }

        // 3. We found a valid Player, return its ID
        instigatorNetworkId = player.NetworkObjectId;
        return true;
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
        Debug.Log($"[BlindTrap] Deploy complete - IsArmed: {IsArmed}");
    }
}