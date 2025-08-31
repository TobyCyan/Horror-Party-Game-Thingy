using UnityEngine;

public class JumpSMB : StateMachineBehaviour
{
    [Tooltip("14/25 for your Mixamo jump")]
    public float takeoffNormTime = 0.56f;

    [Tooltip("Impulse applied at takeoff")]
    public float jumpForce = 5f;

    bool applied;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        applied = false;
        Debug.Log($"[JumpSMB] ENTER Jump. normTime={stateInfo.normalizedTime:0.00}");
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // If the clip loops, unity keeps increasing normalizedTime; usually we don't loop jump,
        // but % 1f is harmless:
        float t = stateInfo.normalizedTime % 1f;

        // Find the RB (parent is common when Animator is on the mesh child)
        var rb = animator.GetComponentInParent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogWarning("[JumpSMB] No Rigidbody found in parents of Animator!");
            return;
        }

        if (!applied && t >= takeoffNormTime)
        {
            // Use rb.velocity (not linearVelocity) unless you're on Unity 6 where both exist
            Vector3 v = rb.linearVelocity;
            rb.linearVelocity = new Vector3(v.x, Mathf.Max(0f, v.y), v.z);

            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            applied = true;

            Debug.Log($"[JumpSMB] APPLY FORCE at t={t:0.00}, impulse={jumpForce}");
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Debug.Log($"[JumpSMB] EXIT Jump. applied={applied}");
    }
}
