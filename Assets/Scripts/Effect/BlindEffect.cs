using UnityEngine;

public class BlindEffect : EffectBase
{
    [SerializeField] private GameObject blindEffect;
    [SerializeField] private float blindDuration = 10f;
    private bool isBlinded = false;
    private float blindTimer = 0.0f;
    private Camera targetCam;

    protected override void ApplyEffect(Player target)
    {
        Blind(target, blindDuration);
    }

    protected override void ApplySubscriptions()
    {
        return;
    }

    private void Update()
    {
        // Blind timer
        if (isBlinded)
        {
            blindTimer -= Time.deltaTime;
            if (blindTimer <= 0f) Unblind();
        }
    }

    private void Blind(Player target, float duration)
    {
        if (blindEffect == null) 
        { 
            Debug.LogWarning("[Blind] No blindEffect assigned."); 
            return; 
        }

        isBlinded = true;
        blindTimer = Mathf.Max(0f, duration);
        blindEffect.SetActive(true);

        if (!target.TryGetComponent(out Camera targetCamera))
        {
            Debug.LogWarning($"No camera at target {target.name}!");
            return;
        }

        targetCam = targetCamera;
        int localBarrierLayer = LayerMask.NameToLayer("LocalBarrier");
        if (localBarrierLayer != -1)
        {
            targetCam.cullingMask |= (1 << localBarrierLayer);
        }
    }

    private void Unblind()
    {
        isBlinded = false;
        blindTimer = 0f;
        if (blindEffect)
        {
            blindEffect.SetActive(false);
        }

        int localBarrierLayer = LayerMask.NameToLayer("LocalBarrier");
        if (targetCam && localBarrierLayer != -1)
        {
            targetCam.cullingMask &= ~(1 << localBarrierLayer);
        }
    }
}
