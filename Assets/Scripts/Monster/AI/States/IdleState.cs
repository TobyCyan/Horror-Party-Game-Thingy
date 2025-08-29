using UnityEngine;

public class IdleState : BaseState
{
    public override bool CanExit(StateMachine stateMachine)
    {
        return true;
    }

    public override bool CanTransition(StateMachine stateMachine)
    {
        return true;
    }

    public override void EnterState(StateMachine stateMachine, Monster monster)
    {
        if (monster == null)
        {
            Debug.LogError("Monster reference is null in IdleState.");
            return;
        }

        this.monster = monster;
        monster.Idle(true);
    }

    public override void ExitState(StateMachine stateMachine)
    {
        if (monster == null)
        {
            Debug.LogError("Monster reference is null in IdleState.");
            return;
        }

        monster.Idle(false);
    }

    public override void UpdateState(StateMachine stateMachine)
    {
        return;
    }
}
