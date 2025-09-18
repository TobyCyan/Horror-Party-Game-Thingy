using UnityEngine;

public class PlayerSpottedState : BaseState
{
    private Player player;
    private readonly LookAt lookAt = new();
    private readonly Timer timer = new();

    public PlayerSpottedState(Monster monster) : base(monster)
    {
    }

    public override void EnterState(StateMachine stateMachine)
    {
        if (player == null)
        {
            Debug.LogWarning("Player reference is null in PlayerSpottedState.");
        }

        if (monster == null)
        {
            Debug.LogError("Monster reference is null in PlayerSpottedState.");
            return;
        }

        float countdown = 3.0f;
        monster.SpotPlayer();
        timer.StartTimer(countdown);
    }

    public override void ExitState(StateMachine stateMachine)
    {
        if (player == null)
        {
            monster.LosePlayer();
        }
    }

    public override void UpdateState(StateMachine stateMachine)
    {
        float lookAtSpeed = 4.0f;
        bool isFinished = lookAt.LookAtTarget(stateMachine.transform, player.transform, lookAtSpeed);
        if (!isFinished)
        {
            return;
        }

        // After finishing looking at the player, run the timer.
        timer.RunTimer();
    }

    public override bool CanTransition(StateMachine stateMachine)
    {
        return monster.SearchForPlayer(out player);
    }

    public override bool CanExit(StateMachine stateMachine)
    {
        // Exit if the timer is complete or if the player is lost.
        return timer.IsComplete || !monster.SearchForPlayer(out player);
    }
}
