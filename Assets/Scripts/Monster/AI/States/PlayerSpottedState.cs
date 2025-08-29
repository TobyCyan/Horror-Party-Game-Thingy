using UnityEngine;

public class PlayerSpottedState : BaseState
{
    private Transform player;
    private readonly LookAt lookAt = new();
    private readonly Timer timer = new();
    private readonly float searchRadius = 10.0f;

    public override void EnterState(StateMachine stateMachine, Monster monster)
    {
        if (player == null)
        {
            Debug.LogWarning("Player reference is null in PlayerSpottedState.");
        }

        this.monster = monster;
        timer.StartTimer(3.0f);
    }

    public override void ExitState(StateMachine stateMachine)
    {
        return;
    }

    public override void UpdateState(StateMachine stateMachine)
    {
        float lookAtSpeed = 2.5f;
        bool isFinished = lookAt.LookAtTarget(stateMachine.transform, player, lookAtSpeed);
        if (!isFinished)
        {
            return;
        }

        bool _ = timer.RunTimer();
    }

    public override bool CanTransition(StateMachine stateMachine)
    {
        // Check for player within search radius
        Collider[] hits = Physics.OverlapSphere(monster.transform.position, searchRadius);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                player = hit.transform;
                return true;
            }
        }
        return false;
    }

    public override bool CanExit(StateMachine stateMachine)
    {
        return timer.RunTimer() || !CanTransition(stateMachine);
    }
}
