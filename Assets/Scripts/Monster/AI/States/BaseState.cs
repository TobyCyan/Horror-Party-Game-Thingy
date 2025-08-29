public abstract class BaseState
{
    protected Monster monster;

    public abstract void EnterState(StateMachine stateMachine, Monster monster);
    public abstract void UpdateState(StateMachine stateMachine);
    public abstract void ExitState(StateMachine stateMachine);
    public abstract bool CanTransition(StateMachine stateMachine);
    public abstract bool CanExit(StateMachine stateMachine);

    public bool IsEqual(BaseState other)
    {
        if (other == null)
        {
            return false;
        }
        return GetType() == other.GetType();
    }
}
