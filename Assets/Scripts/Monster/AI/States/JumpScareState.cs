using UnityEngine;

public class JumpScareState : BaseState
{
    public override bool CanExit(StateMachine stateMachine)
    {
        throw new System.NotImplementedException();
    }

    public override bool CanTransition(StateMachine stateMachine)
    {
        return true;
    }

    public override void EnterState(StateMachine stateMachine, Monster monster)
    {
        if (monster == null)
        {
            Debug.LogError("Monster reference is null in JumpScareState.");
            return;
        }

        Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null)
        {
            Debug.LogError("Player not found in the scene. Ensure the player has the 'Player' tag.");
            return;
        }

        monster.JumpScare(player);
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
