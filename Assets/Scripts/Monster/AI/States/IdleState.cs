using UnityEngine;

public class IdleState : BaseState
{
    public IdleState(Monster monster) : base(monster)
    {
    }

    public override bool CanExit(StateMachine stateMachine)
    {
        return true;
    }

    public override bool CanTransition(StateMachine stateMachine)
    {
        return true;
    }

    public override void EnterState(StateMachine stateMachine)
    {
        if (monster == null)
        {
            Debug.LogError("Monster reference is null in IdleState.");
            return;
        }
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
