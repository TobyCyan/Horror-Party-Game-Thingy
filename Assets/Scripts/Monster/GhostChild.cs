using System.Collections.Generic;
using System.Numerics;

public class GhostChild : Monster
{
    protected new Vector3 outOfBoundsPosition = new(0, -1000, 0);

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
