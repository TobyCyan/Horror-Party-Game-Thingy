using Unity.VisualScripting;

public class DisabledState : BaseState
{
    private readonly Timer timer;

    public DisabledState(Monster monster) : base(monster)
    {
        if (timer == null)
        {
            timer = monster.AddComponent<Timer>();
        }
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
        timer.OnTimeUp += () => ExitState(stateMachine);
        float disabledDuration = 6.0f;
        timer.StartTimer(disabledDuration);
    }

    public override void ExitState(StateMachine stateMachine)
    {
        monster.Enable(true);
        timer.OnTimeUp -= () => ExitState(stateMachine);
    }

    public override void UpdateState(StateMachine stateMachine)
    {
        return;
    }
}
