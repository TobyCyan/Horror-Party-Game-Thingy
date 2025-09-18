using UnityEngine;

public class JumpScare : MonoBehaviour
{
    [SerializeField] private AudioClip jumpScareSfx;
    [Header("Additional Offset")]
    [SerializeField] private Vector3 offset;
    private AudioSource audioSource;
    private Vector3 referencePosition;

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

        // For adjusting offset in the editor
#if UNITY_EDITOR
        transform.position = referencePosition + offset;
#endif
    }

    public void TriggerJumpScare(Transform target)
    {
        // Assumes animator is on this game object.
        Animator animator = GetComponent<Animator>();
        if (!animator)
        {
            return;
        }
        TriggerJumpScare(animator, target);
    }

    public void TriggerJumpScare(Animator animator, Transform target)
    {
        // Jump to target.
        JumpToTarget(target);

#if UNITY_EDITOR
        referencePosition = transform.position;
#endif

        // TODO: Force target to look at the monster.

        // JumpScare the target.
        animator.SetTrigger("JumpScare");
        animator.Play("JumpScare", 0, 0.0f);

        PlayJumpScareAudio();
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
