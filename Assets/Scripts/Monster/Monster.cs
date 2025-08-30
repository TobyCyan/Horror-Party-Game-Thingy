using System.Collections.Generic;
using UnityEngine;

public class Monster : MonoBehaviour
{
    protected JumpScare jumpScare;
    protected StateMachine stateMachine;
    protected PlayerRadar playerRadar;
    private Animator animator;
    public Vector3 initialPosition;
    private readonly Vector3 outOfBoundsPosition = new(0, -500, 0);
    [HideInInspector] public Transform targetPlayer;

    private void Awake()
    {
        OnValidate();
        Initialize();
    }

    private void OnValidate()
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
            playerRadar = GetComponent<PlayerRadar>();
            if (playerRadar == null)
            {
                Debug.LogWarning("PlayerRadar component is missing from the Monster GameObject.");
            }
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogWarning("Animator component is missing from the Monster GameObject.");
            }
        }
    }

    private void ResetAnimator()
    {
        if (animator == null)
        {
            Debug.LogWarning("Animator component is missing. Cannot reset states.");
            return;
        }
        ResetAnimatorParams();
    }

    public bool SearchForPlayer(out Transform player)
    {
        bool isPlayerInRange = playerRadar.IsPlayerInRange(out player);
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

        if (animator == null)
        {
            Debug.LogWarning("Animator component is missing. Cannot perform JumpScare.");
            return;
        }

        jumpScare.TriggerJumpScare(animator, targetPlayer.GetComponent<Camera>().transform);
    }

    public void SpotPlayer()
    {
        animator.SetBool("PlayerSpotted", true);
    }

    public void LosePlayer()
    {
        animator.SetBool("PlayerSpotted", false);
        targetPlayer = null;
    }

    public bool IsJumpScareComplete()
    {
        if (animator == null)
        {
            Debug.LogWarning("Animator component is missing. Cannot check JumpScare completion.");
            return true;
        }
        return jumpScare.IsJumpScareComplete(animator);
    }

    public virtual void Idle(bool enable)
    {
        if (enable)
        {
            animator.Play("Idle", 0, 0.0f);
        }
    }

    public void Initialize()
    {
        InitializeStateMachine();
        ResetPosition();
        ResetAnimator();
    }

    public void ResetPosition()
    {
        transform.position = initialPosition;
    }

    public void MoveOutOfBounds()
    {
        transform.position = outOfBoundsPosition;
    }

    private void OnEnable()
    {
        ResetPosition();
    }

    private void OnDisable()
    {
        MoveOutOfBounds();
        ResetAnimator();
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

    protected virtual void InitializeStateMachine()
    {
        BaseState initialState = new IdleState(this);
        Dictionary<BaseState, BaseState[]> transitions = new()
        {
            { initialState, new BaseState[] { } }
        };
        stateMachine.Initialize(initialState, transitions);
    }

    private void ResetAnimatorParams()
    {
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            switch (param.type)
            {
                case AnimatorControllerParameterType.Float:
                    animator.SetFloat(param.name, 0f);
                    break;

                case AnimatorControllerParameterType.Int:
                    animator.SetInteger(param.name, 0);
                    break;

                case AnimatorControllerParameterType.Bool:
                    animator.SetBool(param.name, false);
                    break;

                case AnimatorControllerParameterType.Trigger:
                    animator.ResetTrigger(param.name);
                    break;
            }
        }
    }
}
