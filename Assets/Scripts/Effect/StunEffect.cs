using UnityEngine;

public class StunEffect : EffectBase
{
    [Min(0.0f)]
    [SerializeField] private float stunDuration = 5.0f;
    private bool isStunned = false;
    private float stunTimer = 0f;

    protected override void ApplyEffect(Player target)
    {
        Debug.Log($"Target {target.name} stunned!");
        Stun(target, stunDuration);
    }

    protected override void ApplySubscriptions()
    {
        return;
    }

    private void Update()
    {
        if (isStunned)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f) Unstun();
        }
    }

    private void Stun(Player player, float duration)
    {
        isStunned = true;
        stunTimer = Mathf.Max(0f, duration);

        Rigidbody rb = player.GetComponentInChildren<Rigidbody>();
        Animator anim = player.GetComponentInChildren<Animator>();

        rb.linearVelocity = Vector3.zero; //  immediate stop
        anim.SetBool("IsWalking", false);
        anim.SetFloat("MoveX", 0f);
        anim.SetFloat("MoveZ", 0f);
        Debug.Log($"[PlayerMovement] Frozen for {duration:0.00}s");
    }

    /// <summary>
    /// Unfreeze the player immediately.
    /// </summary>
    private void Unstun()
    {
        isStunned = false;
        stunTimer = 0f;
    }
}
