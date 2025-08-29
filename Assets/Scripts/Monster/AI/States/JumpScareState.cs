using UnityEngine;

public class JumpScareState : BaseState
{
    public JumpScareState(Monster monster) : base(monster)
    {
    }

    public override bool CanExit(StateMachine stateMachine)
    {
        return !CanTransition(stateMachine);
    }

    public override bool CanTransition(StateMachine stateMachine)
    {
        return monster.targetPlayer != null;
    }

    public override void EnterState(StateMachine stateMachine)
    {
        if (monster == null)
        {
            Debug.LogError("Monster reference is null in JumpScareState.");
            return;
        }

        monster.JumpScare();
    }

    public override void ExitState(StateMachine stateMachine)
    {
        return;
    }

    public override void UpdateState(StateMachine stateMachine)
    {
        return;
    }
}
