using System.Collections.Generic;
using UnityEngine;

public class StateMachine : MonoBehaviour
{
    public BaseState currentState;

    public Dictionary<BaseState, BaseState[]> stateTransitions = new();

    public void Initialize(BaseState initialState, Dictionary<BaseState, BaseState[]> transitions)
    {
        stateTransitions = transitions;
        currentState = initialState;
        currentState.EnterState(this);
    }

    void Update()
    {
        currentState.UpdateState(this);
        if (!stateTransitions.ContainsKey(currentState))
        {
            Debug.LogWarning("No transitions defined for the current state: " + currentState.GetType().Name);
            return;
        }

        if (!currentState.CanExit(this))
        {
            return;
        }

        foreach (var transition in stateTransitions[currentState])
        {
            // Check if the transition conditions are met
            if (transition.CanTransition(this))
            {
                GoNextState(transition);
                break;
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

        if (currentState.Equals(nextState))
        {
            Debug.Log("Already in the desired state. No transition needed.");
            return;
        }

        currentState.ExitState(this);
        currentState = nextState;
        currentState.EnterState(this);
    }
}
