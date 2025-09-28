using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AutoTrapActivator : MonoBehaviour
{
    private List<ITrap> autoTraps = new();
    private TrapTriggerContext ctx;

    void Start()
    {
        autoTraps = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
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
        foreach (var trap in autoTraps)
        {
            trap.Trigger(ctx);
        }
    }
}
