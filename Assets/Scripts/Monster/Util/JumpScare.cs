using System;
using System.Collections.Generic;
using UnityEngine;

public class JumpScare : MonoBehaviour
{
    [SerializeField] private AudioClip jumpScareSfx;
    [Header("Additional Offset")]
    [SerializeField] private Vector3 offset;
    [SerializeField] private List<EffectBase> effects;
    private AudioSource audioSource;
    private Vector3 referencePosition;
    private Transform jumpScareTarget;

    public event Action OnJumpScareStart;
    // Apply any effect after jumpscaring (e.g. stun, death etc.)
    public event Action<Transform> AfterJumpScarePlayer;
    public event Action OnJumpScareCleanUp;

    private void Awake()
    {
        foreach (var effect in effects)
        {
            AfterJumpScarePlayer += effect.Apply;
            Debug.Log($"Effect {effect.name} subscribed");
        }
    }

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
        OnJumpScareStart?.Invoke();
        jumpScareTarget = target;

        // Jump to target.
        JumpToTarget(target);

#if UNITY_EDITOR
        referencePosition = transform.position;
#endif

        // Force monster to look at target.
        FaceTarget(target);

        // Disable player camera movement.
        if (target.TryGetComponent(out Player player))
        {
            player.EnablePlayer(false);
        }

        // JumpScare the target.
        animator.SetTrigger("JumpScare");
        animator.Play("JumpScare", 0, 0.0f);

        PlayJumpScareAudio();
    }

    private void JumpToTarget(Transform target)
    {
        Vector3 targetForward = target.forward;
        Vector3 offsetPosition = target.position + (targetForward * 0.5f) + offset; // Offset by 0.5 units in front of the target
        transform.position = offsetPosition;
    }

    private void FaceTarget(Transform target)
    {
        transform.LookAt(target.position);
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

    /// <summary>
    /// Invokes any methods subscribed to the jumpscare end event.
    /// This is called from the Unity animation event system.
    /// </summary>
    public void OnJumpScareEnd()
    {
        Debug.Log($"{name} Jumpscare ended");
        AfterJumpScarePlayer?.Invoke(jumpScareTarget);
        OnJumpScareCleanUp?.Invoke();
    }
}
