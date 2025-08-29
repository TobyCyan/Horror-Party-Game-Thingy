using System.Collections.Generic;

public class GhostChild : Monster
{
    public override void InitializeStateMachine()
    {
        BaseState initialState = new IdleState();
        Dictionary<BaseState, BaseState[]> transitions = new()
        {
            { initialState, new BaseState[] { new PlayerSpottedState() } },
            { new PlayerSpottedState(), new BaseState[] { new JumpScareState(), new IdleState() } },
            { new JumpScareState(), new BaseState[] { initialState } }
        };
        stateMachine.Initialize(initialState, this, transitions);
    }
}
