using UnityEngine;

public class StunEffect : EffectBase
{
    [Min(0.0f)]
    [SerializeField] private float stunDuration = 5.0f;

    protected override void ApplyEffect(Player target)
    {
        Debug.Log($"Target {target.name} stunned!");
        target.Stun(stunDuration);
    }

    protected override void ApplySubscriptions()
    {
        return;
    }
}
