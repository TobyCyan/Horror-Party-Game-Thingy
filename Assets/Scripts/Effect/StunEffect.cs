using UnityEngine;

public class StunEffect : EffectBase
{
    protected override void ApplyEffect(Transform target)
    {
        Debug.Log($"Target {target.name} stunned!");
    }

    protected override void ApplySubscriptions()
    {
        return;
    }
}
