using UnityEngine;
using Unity.Netcode;

/// Reveals the touching player as a red silhouette for a short time whenever this trap fires.
[DisallowMultipleComponent]
public class TrapRevealOnTrigger : MonoBehaviour
{
    [SerializeField] TrapBase trap;
    [SerializeField] float silhouetteSeconds = 2f;

    // Optional AoE reveal (set radius > 0 to enable)
    [Header("Optional AOE")]
    [SerializeField] float aoeRadius = 0f;
    [SerializeField] LayerMask actorMask = ~0;

    void Reset() => trap = GetComponent<TrapBase>();

    void OnEnable()
    {
        if (!trap) trap = GetComponent<TrapBase>();
        if (trap != null) trap.OnTriggered += HandleTriggered;
    }

    void OnDisable()
    {
        if (trap != null) trap.OnTriggered -= HandleTriggered;
    }

    void HandleTriggered(ITrap _, TrapTriggerContext ctx)
    {
        Debug.Log($"TrapRevealOnTrigger: revealing {ctx.instigator} for {silhouetteSeconds} seconds");
        // 1) Reveal the instigator (the object that touched the trap)
        Reveal(ctx.instigator);

        // 2) Optional: reveal others in a radius
        if (aoeRadius > 0f)
        {
            var hits = Physics.OverlapSphere(transform.position, aoeRadius, actorMask, QueryTriggerInteraction.Ignore);
            foreach (var h in hits) Reveal(h.gameObject);
        }
    }

    void Reveal(GameObject go)
    {
        if (!go) return;
        var sil = go.GetComponentInParent<PlayerSilhouette>();
        Debug.Log($"TrapRevealOnTrigger: Reveal {go} sil={sil}");
        if (sil != null) sil.ShowForSeconds_Server(silhouetteSeconds);
    }
}
