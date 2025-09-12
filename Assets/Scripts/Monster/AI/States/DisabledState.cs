public class DisabledState : BaseState
{
    private readonly Timer timer = new();

    public DisabledState(Monster monster) : base(monster)
    {
    }

    public override bool CanExit(StateMachine stateMachine)
    {
        return timer.IsComplete || !CanTransition(stateMachine);
    }

    public override bool CanTransition(StateMachine stateMachine)
    {
        return true;
    }

    public override void EnterState(StateMachine stateMachine)
    {
        monster.Enable(false);
        float disabledDuration = 6.0f;
        timer.StartTimer(disabledDuration);
    }

    public override void ExitState(StateMachine stateMachine)
    {
        monster.Enable(true);
    }

    public override void UpdateState(StateMachine stateMachine)
    {
        timer.RunTimer();
    }
}
