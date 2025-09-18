using System;
using UnityEngine;

public abstract class EffectBase : MonoBehaviour
{
    public event Action OnEffectApplied;

    private void Start()
    {
        ApplySubscriptions();
    }

    public void Apply(Transform target)
    {
        if (!target)
        {
            return;
        }

        ApplyEffect(target);
        OnEffectApplied?.Invoke();
    }

    protected abstract void ApplySubscriptions();
    protected abstract void ApplyEffect(Transform target);
}
