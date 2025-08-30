using System.Collections.Generic;
using UnityEngine;

public class GhostChild : Monster
{
    private Reveal reveal;

    public override void Initialize()
    {
        base.Initialize();
        reveal = GetComponent<Reveal>();
        reveal.Initialize(initialPosition, outOfBoundsPosition);
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        if (reveal == null)
        {
            reveal = GetComponent<Reveal>();
            if (reveal == null)
            {
                Debug.LogWarning("Reveal component is missing from the GhostChild GameObject.");
            }
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        reveal.RevealSelf();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        reveal.HideSelf();
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
