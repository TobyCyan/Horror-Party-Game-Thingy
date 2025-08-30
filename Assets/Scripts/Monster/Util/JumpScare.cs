using UnityEngine;

public class JumpScare : MonoBehaviour
{
    [SerializeField] private AudioClip jumpScareSfx;
    [Header("Additional Offset")]
    [SerializeField] private Vector3 offset;
    private AudioSource audioSource;

    private void OnValidate()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                Debug.LogWarning("AudioSource component is missing from the JumpScare GameObject.");
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
        if (jumpScareSfx != null)
        {
            audioSource.PlayOneShot(jumpScareSfx);
        }
        else
        {
            Debug.LogWarning("JumpScare audio clip is not assigned.");
        }
    }
}
