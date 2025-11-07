using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

public class AutoTrapActivator : NetworkBehaviour
{
    private List<ITrap> autoTraps = new();
    private TrapTriggerContext ctx;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        base.OnNetworkSpawn();
        autoTraps = FindObjectsByType<NetworkBehaviour>(FindObjectsSortMode.None)
            .OfType<ITrap>()
            .Where(trap => trap.Placement == TrapPlacementKind.Auto)
            .ToList();

        Debug.Log($"Auto Traps Found: {autoTraps}");

        // Create a context for game-triggered traps
        ctx = new TrapTriggerContext
        {
            source = TrapTriggerSource.Game,
            instigator = gameObject,
            hitPoint = gameObject.transform.position,
            hitNormal = Vector3.up
        };
    }

    void Update()
    {
        if (!IsServer) return;
        foreach (var trap in autoTraps)
        {
            trap.Trigger(ctx);
        }
    }
}
