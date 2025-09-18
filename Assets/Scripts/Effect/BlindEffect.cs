using UnityEngine;

public class BlindEffect : EffectBase
{
    [SerializeField] private GameObject blindEffect;
    [Min(0.0f)]
    [SerializeField] private float blindDuration = 10.0f;
    [SerializeField] private Camera targetCam;
    private bool isBlinded = false;
    private float blindTimer = 0.0f;

    protected override void ApplyEffect(Player target)
    {
        Blind(blindDuration);
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

    private void Blind(float duration)
    {
        if (blindEffect == null) 
        { 
            Debug.LogWarning("[Blind] No blindEffect assigned."); 
            return; 
        }

        isBlinded = true;
        blindTimer = Mathf.Max(0f, duration);
        blindEffect.SetActive(true);

        if (!targetCam)
        {
            Debug.LogWarning($"No camera assigned to {name}!");
            return;
        }

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
