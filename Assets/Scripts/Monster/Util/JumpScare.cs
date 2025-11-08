using System;
using UnityEngine;
using Unity.Netcode;

public class JumpScare : NetworkBehaviour
{
    [SerializeField] private AudioClip jumpScareSfx;
    [Header("Additional Offset")]
    [SerializeField] private Vector3 offset;
    // [SerializeField] private List<EffectBase> effects; // Problematic, manually apply effects for now.
    [SerializeField] private bool shouldResumeCameraMovementAfter = true;
    private AudioSource audioSource;
    private Vector3 referencePosition;
    private readonly Vector3 defaultPosition = new(0, -10000, 0);
    private readonly NetworkVariable<ulong> jumpScarePlayerClientId = new();

    public event Action OnJumpScareStart;
    // Apply any effect after jumpscaring (e.g. stun, death etc.)
    public event Action<ulong> AfterJumpScarePlayer;
    public event Action OnJumpScareCleanUp;

    // The animation name used as state and trigger.
    private static readonly string AN_JUMPSCARE = "JumpScare";

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

    [Rpc(SendTo.Everyone)]
    public void TriggerJumpScareRpc(ulong playerClientId)
    {
        // Assumes animator is on this game object.
        Animator animator = GetComponent<Animator>();
        if (!animator)
        {
            return;
        }
        OnJumpScareStart?.Invoke();
        Player player = PlayerManager.Instance.FindPlayerByClientId(playerClientId);
        if (player == null)
        {
            Debug.LogWarning($"JumpScare: Player with ClientId {playerClientId} not found.");
            return;
        }
        player.LockPlayerInPlace();

        if (IsServer)
            jumpScarePlayerClientId.Value = playerClientId;
        
        Transform targetTransform = player.PlayerCam.transform;
        JumpToTarget(targetTransform);

        // Force monster to look at target.
        FaceTarget(targetTransform);

        // Minus offset to get accurate reference position for adjusting offset
#if UNITY_EDITOR
        referencePosition = transform.position - offset;
#endif

        // JumpScare the target.
        animator.SetTrigger(AN_JUMPSCARE);
        animator.Play(AN_JUMPSCARE, 0, 0.0f);

        PlayJumpScareAudio();
    }

    public void ResetPosition()
    {
        transform.position = defaultPosition;
    }

    private void JumpToTarget(Transform target)
    {
        Vector3 targetForward = target.forward;
        Vector3 offsetPosition = target.position + (targetForward * 0.5f) + offset;
        transform.position = offsetPosition;
    }

    private void FaceTarget(Transform target)
    {
        transform.rotation = Quaternion.LookRotation(-target.forward, Vector3.up);
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
        OnJumpScareCleanUp?.Invoke();
        if (NetworkManager.Singleton.LocalClientId != jumpScarePlayerClientId.Value)
            return;
        Debug.Log($"{name} Jumpscare ended");
        AfterJumpScarePlayer?.Invoke(jumpScarePlayerClientId.Value);

        Player jumpScarePlayer = PlayerManager.Instance.FindPlayerByClientId(jumpScarePlayerClientId.Value);
        if (shouldResumeCameraMovementAfter && jumpScarePlayer)
        {
            jumpScarePlayer.EnablePlayer(true);
        }
    }
}
