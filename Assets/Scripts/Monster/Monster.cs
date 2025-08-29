using System.Collections.Generic;
using UnityEngine;

public class Monster : MonoBehaviour
{
    protected JumpScare jumpScare;
    protected StateMachine stateMachine;
    protected PlayerRadar playerRadar;
    private Animator animator;
    [HideInInspector] public Transform targetPlayer;

    private void Awake()
    {
        OnValidate();
        InitializeStateMachine();
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
        animator.SetTrigger("PlayerSpotted");
    }

    public void Idle(bool enable)
    {

    }

    public virtual void InitializeStateMachine()
    {
        BaseState initialState = new IdleState(this);
        Dictionary<BaseState, BaseState[]> transitions = new()
        {
            { initialState, new BaseState[] { } }
        };
        stateMachine.Initialize(initialState, this, transitions);
    }
}
