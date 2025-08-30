using UnityEngine;

public class JumpScare : MonoBehaviour
{
    private AudioSource jumpScareAudio;
    [Header("Additional Offset")]
    [SerializeField] private Vector3 offset;

    private void OnValidate()
    {
        if (jumpScareAudio == null)
        {
            jumpScareAudio = GetComponent<AudioSource>();
            if (jumpScareAudio == null)
            {
                Debug.LogWarning("AudioSource is not assigned in JumpScare script.");
            }
        }
    }

    public void TriggerJumpScare(Animator animator, Transform target)
    {
        // Jump to target.
        JumpToTarget(target);

        // TODO: Force target to look at the monster.

        // JumpScare the target.
        animator.SetTrigger("JumpScare");
        animator.Play("JumpScare", 0, 0.0f);

        PlayJumpScareAudio();
    }

    public bool IsJumpScareComplete(Animator animator)
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (animator.IsInTransition(0))
        {
            AnimatorStateInfo nextInfo = animator.GetNextAnimatorStateInfo(0);

            // JumpScare finished and transitioning out
            if (stateInfo.IsName("JumpScare") && stateInfo.normalizedTime >= 1.0f)
            {
                return true;
            }

            // Already moved to a non-JumpScare state
            if (!nextInfo.IsName("JumpScare"))
            {
                return true;
            }

            return false;
        }

        return stateInfo.IsName("JumpScare") && stateInfo.normalizedTime >= 1.0f;
    }

    private void JumpToTarget(Transform target)
    {
        Vector3 targetForward = target.forward;
        Vector3 offsetPosition = target.position + (targetForward * 0.5f) + offset; // Offset by 0.5 units in front of the target
        gameObject.transform.position = offsetPosition;
    }

    private void PlayJumpScareAudio()
    {
        if (jumpScareAudio.isPlaying)
        {
            jumpScareAudio.Stop();
        }

        jumpScareAudio.Play();
    }
}
