using System.Collections.Generic;
using UnityEngine;

public class Monster : MonoBehaviour
{
    protected JumpScare jumpScare;
    protected StateMachine stateMachine;
    protected PlayerRadar playerRadar;
    private AnimatorController animatorController;
    [SerializeField] protected Vector3 initialPosition;
    protected Vector3 outOfBoundsPosition = new(0, -500, 0);
    [HideInInspector] public Transform targetPlayer;
    [SerializeField] private float searchRadius = 2.5f;
    [SerializeField] private AudioClip playerSpottedSfx;
    [SerializeField] private AudioClip idleSfx;

    private void Awake()
    {
        Initialize();
    }

    protected virtual void OnValidate()
    {
        if (jumpScare == null)
        {
            jumpScare = GetComponent<JumpScare>();
            if (jumpScare == null)
            {
                Debug.LogWarning("JumpScare component is missing from the Monster GameObject.");
            }
        }

        if (stateMachine == null)
        {
            stateMachine = GetComponent<StateMachine>();
            if (stateMachine == null)
            {
                Debug.LogWarning("StateMachine component is missing from the Monster GameObject.");
            }
        }

        if (playerRadar == null)
        {
            playerRadar = new PlayerRadar(searchRadius);
            if (playerRadar == null)
            {
                Debug.LogWarning("PlayerRadar component is missing from the Monster GameObject.");
            }
        }

        if (animatorController == null)
        {
            animatorController = GetComponent<AnimatorController>();
            if (animatorController == null)
            {
                Debug.LogWarning("Animator Controller component is missing from the Monster GameObject.");
            }
        }
    }

    private void ResetAnimator()
    {
        animatorController.ResetAnimator();
    }

    public bool SearchForPlayer(out Transform player)
    {
        bool isPlayerInRange = playerRadar.IsPlayerInRange(transform.position, out player);
        targetPlayer = player;
        return isPlayerInRange;
    }

    public void JumpScare()
    {
        if (targetPlayer == null)
        {
            Debug.LogWarning("No target player found for JumpScare.");
            return;
        }

        if (animatorController == null)
        {
            Debug.LogWarning("Animator Controller component is missing. Cannot perform JumpScare.");
            return;
        }

        jumpScare.TriggerJumpScare(animatorController.Animator, targetPlayer.GetComponent<Camera>().transform);
    }

    public void SpotPlayer()
    {
        animatorController.SetAnimatorBool("PlayerSpotted", true);
    }

    public void LosePlayer()
    {
        animatorController.SetAnimatorBool("PlayerSpotted", false);
        targetPlayer = null;
    }

    public bool IsJumpScareComplete()
    {
        if (animatorController == null)
        {
            Debug.LogWarning("Animator Controller component is missing. Cannot check JumpScare completion.");
            return true;
        }
        return jumpScare.IsJumpScareComplete(animatorController.Animator);
    }

    public virtual void Idle(bool enable)
    {
        if (enable)
        {
            animatorController.PlayAnimatorState("Idle", 0, 0.0f);
        }
    }

    public virtual void Initialize()
    {
        InitializeStateMachine();
        animatorController.Initialize();
        ResetAnimator();
    }

    protected virtual void OnEnable()
    {
        PlaySfx(idleSfx);
    }

    protected virtual void OnDisable()
    {
        ResetAnimator();
        PlaySfx(null);
    }

    public void Enable(bool enable)
    {
        if (enable)
        {
            OnEnable();
        }
        else
        {
            OnDisable();
        }
    }

    public void PlaySfx(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("AudioClip is null. Cannot play sound effect.");
            return;
        }
        AudioSource.PlayClipAtPoint(clip, transform.position);
    }

    protected virtual void InitializeStateMachine()
    {
        BaseState initialState = new IdleState(this);
        Dictionary<BaseState, BaseState[]> transitions = new()
        {
            { initialState, new BaseState[] { } }
        };
        stateMachine.Initialize(initialState, transitions);
    }
}
