using UnityEngine;

public class BlindEffect : EffectBase
{
    [Min(0.0f)]
    [SerializeField] private float blindDuration = 10.0f;

    protected override void ApplyEffect(Player target)
    {
        Debug.Log($"Blinded {target.name}!");
        target.Blind(blindDuration);
    }

    protected override void ApplySubscriptions()
    {
        return;
    }
}
