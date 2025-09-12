public abstract class BaseState
{
    protected Monster monster;

    public BaseState(Monster monster)
    {
        this.monster = monster;
    }

    public abstract void EnterState(StateMachine stateMachine);
    public abstract void UpdateState(StateMachine stateMachine);
    public abstract void ExitState(StateMachine stateMachine);
    public abstract bool CanTransition(StateMachine stateMachine);
    public abstract bool CanExit(StateMachine stateMachine);

    public override bool Equals(object other)
    {
        if (other == null)
        {
            return false;
        }
        return GetType() == other.GetType();
    }

    public override int GetHashCode()
    {
        // For dictionary key comparison.
        return GetType().GetHashCode();
    }
}
