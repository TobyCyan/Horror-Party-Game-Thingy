using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent (typeof(BlindEffect))]
public class BlindTrap : TrapBase
{
    [Header("Who can trigger")]
    [SerializeField] private LayerMask playerMask;   // set to "Player" in Inspector

    private BlindEffect blindEffect;

    protected override void Start()
    {
        base.Start();
        blindEffect = GetComponent<BlindEffect>();
    }

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;   // we want trigger overlaps
    }

    void OnTriggerEnter(Collider other)
    {
        if (!CanTrigger()) return;
        if ((playerMask.value & (1 << other.gameObject.layer)) == 0) return;
        Debug.Log($"BlindTrap: triggered by {other.gameObject}");
        // Build context and fire TrapBase logic (cooldown, oneShot, events)
        var ctx = new TrapTriggerContext
        {
            source = TrapTriggerSource.Player,
            instigator = other.gameObject,
            hitPoint = other.ClosestPoint(transform.position),
            hitNormal = Vector3.up
        };
        Trigger(ctx);
    }

    // Trap effect
    protected override void OnTriggerCore(TrapTriggerContext ctx)
    {
        var player = ctx.instigator.GetComponentInParent<Player>();
        if (player == null) return;

        // Call the method your PlayerMovement exposes:
        // If your signature is OnBlind(float):
        // pm.OnBlind(blindSeconds);

        blindEffect.Apply(player);
        Debug.Log($"BlindTrap: blinded {ctx.instigator}");
    }
}
