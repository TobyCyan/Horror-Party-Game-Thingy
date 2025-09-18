using System;
using UnityEngine;

public abstract class EffectBase : MonoBehaviour
{
    public event Action OnEffectApplied;

    private void Start()
    {
        ApplySubscriptions();
    }

    public void Apply(Player target)
    {
        if (!target)
        {
            return;
        }

        ApplyEffect(target);
        OnEffectApplied?.Invoke();
    }

    /// <summary>
    /// Applies any subscriptions to events related to this effect.
    /// </summary>
    protected abstract void ApplySubscriptions();
    protected abstract void ApplyEffect(Player target);
}
