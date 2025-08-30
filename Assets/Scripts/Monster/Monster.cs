using System.Collections.Generic;
using UnityEngine;

public class Monster : MonoBehaviour
{
    protected JumpScare jumpScare;
    protected StateMachine stateMachine;
    protected PlayerRadar playerRadar;
    private AnimatorController animatorController;
    public Vector3 initialPosition;
    protected Vector3 outOfBoundsPosition = new(0, -500, 0);
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

    public void Initialize()
    {
        InitializeStateMachine();
        animatorController.Initialize();
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
}
