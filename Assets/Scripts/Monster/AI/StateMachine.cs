using System.Collections.Generic;
using UnityEngine;

public class StateMachine : MonoBehaviour
{
    public BaseState currentState;
    private Monster monster;

    public Dictionary<BaseState, BaseState[]> stateTransitions = new();

    public void Initialize(BaseState initialState, Monster monster, Dictionary<BaseState, BaseState[]> transitions)
    {
        this.monster = monster;
        stateTransitions = transitions;
        currentState = initialState;
        currentState.EnterState(this, monster);
    }

    void Update()
    {
        currentState.UpdateState(this);
        foreach (var transition in stateTransitions[currentState])
        {
            // Check if the transition conditions are met and if the current state can exit
            if (transition.CanTransition(this) && currentState.CanExit(this))
            {
                GoNextState(transition);
            }
        }
    }

    public void GoNextState(BaseState nextState)
    {
        if (nextState == null)
        {
            Debug.LogWarning("Next state is null. State transition aborted.");
            return;
        }

        if (currentState.IsEqual(nextState))
        {
            Debug.Log("Already in the desired state. No transition needed.");
            return;
        }

        currentState.ExitState(this);
        currentState = nextState;
        currentState.EnterState(this, monster);
    }
}
