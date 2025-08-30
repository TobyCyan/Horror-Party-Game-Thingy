using System.Collections.Generic;
using UnityEngine;

public class GhostChild : Monster
{
    public override void Initialize()
    {
        base.Initialize();
        idleSfxMaxDistance = 5.5f;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
    }

    protected override void InitializeStateMachine()
    {
        BaseState idleState = new IdleState(this);
        BaseState playerSpottedState = new PlayerSpottedState(this);
        BaseState jumpScareState = new JumpScareState(this);
        BaseState disabledState = new DisabledState(this);

        // Graph representation of state transitions
        Dictionary<BaseState, BaseState[]> transitions = new()
        {
            { idleState, new BaseState[] { playerSpottedState } },
            { playerSpottedState, new BaseState[] { jumpScareState, idleState } },
            { jumpScareState, new BaseState[] { disabledState } },
            { disabledState, new BaseState[] { idleState }   }
        };

        BaseState initialState = idleState;
        stateMachine.Initialize(initialState, transitions);
    }
}
