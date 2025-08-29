using System.Collections.Generic;
using UnityEngine;

public class Monster : MonoBehaviour
{
    protected JumpScare jumpScare;
    protected StateMachine stateMachine;

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
    }

    public void JumpScare(Transform target)
    {
        jumpScare.TriggerJumpScare(target);
    }

    public void Idle(bool enable)
    {

    }

    public virtual void InitializeStateMachine()
    {
        BaseState initialState = new IdleState();
        Dictionary<BaseState, BaseState[]> transitions = new()
        {
            { initialState, new BaseState[] { } }
        };
        stateMachine.Initialize(initialState, this, transitions);
    }
}
