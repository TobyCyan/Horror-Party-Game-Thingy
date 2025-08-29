using UnityEngine;

public class JumpScare : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource jumpScareAudio;

    private void OnValidate()
    {
        if (animator == null)
        {
            Debug.LogWarning("Animator is not assigned in JumpScare script.");
        }

        if (jumpScareAudio == null)
        {
            Debug.LogWarning("AudioSource is not assigned in JumpScare script.");
        }
    }

    public void TriggerJumpScare(Transform target)
    {
        // Jump to target.
        JumpToTarget(target);

        // TODO: Force target to look at the monster.

        // JumpScare the target.
        animator.SetTrigger("JumpScare");

        PlayJumpScareAudio();
    }

    private void JumpToTarget(Transform target)
    {
        Vector3 targetForward = target.forward;
        Vector3 offsetPosition = target.position - targetForward * 0.5f; // Offset by 0.5 units in front of the target
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
