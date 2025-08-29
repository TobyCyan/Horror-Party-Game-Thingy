using System.Collections.Generic;

public class GhostChild : Monster
{
    public override void InitializeStateMachine()
    {
        BaseState idleState = new IdleState(this);
        BaseState playerSpottedState = new PlayerSpottedState(this);
        BaseState jumpScareState = new JumpScareState(this);

        BaseState initialState = idleState;

        Dictionary<BaseState, BaseState[]> transitions = new()
        {
            { idleState, new BaseState[] { playerSpottedState } },
            { playerSpottedState, new BaseState[] { jumpScareState, idleState } },
            { jumpScareState, new BaseState[] { idleState } }
        };
        stateMachine.Initialize(initialState, this, transitions);
    }
}
