using UnityEngine;
using Unity.Cinemachine;

// these could probably be instances
public abstract class MazeGamePhase
{
    protected PhaseID id; // laze
    public virtual void Enter()
    {
        Debug.Log($"Entering {GetType().Name}");
    }

    public virtual void Exit()
    {
        Debug.Log($"Exiting {GetType().Name}");
    }

    public virtual void UpdatePhase() { }

    public virtual void ChangePhase(PhaseID next)
    {
        MazeGameManager.Instance.ChangePhase(id, next);
    }
}


public enum PhaseID
{
    Default = 0,
    Traps = 1,
    Run = 2,
    Score = 3
}

public class DefaultPhase : MazeGamePhase
{
    public DefaultPhase() { id = PhaseID.Default; }
}

public class TrapPhase : MazeGamePhase
{

    float timeLimit;
    float currTime = 0f;
    public TrapPhase()
    {
        timeLimit = 5f;
    }

    public TrapPhase(float timeLimit)
    {
        this.timeLimit = timeLimit;
    }

    public override void Enter()
    {
        // todo disbale fps input , change ui
        base.Enter();
        MazeCameraManager.Instance.SetToTopDownView();
        UIManager.Instance.SwitchUIView<TrapsPhaseView>();
        
    }
    public override void Exit()
    {
        
        base.Exit();

    }

    public override void UpdatePhase()
    {
        base.UpdatePhase();
        currTime += Time.deltaTime;
        if (currTime >= timeLimit)
        {
            ChangePhase(PhaseID.Run);
        }
    }
}

public class RunPhase : MazeGamePhase
{
    float timeLimit;
    float currTime = 0f;

    public RunPhase()
    {
        this.timeLimit = 5f;
    }

    public RunPhase(float timeLimit)
    {
        this.timeLimit = timeLimit;
    }

    public override void Enter()
    {
        base.Enter();
        // todo enable fps input, change ui
        MazeCameraManager.Instance.SetToPlayerView();
        UIManager.Instance.SwitchUIView<RunPhaseView>();
    }
    public override void Exit()
    {

        base.Exit();
    }

    public override void UpdatePhase()
    {
        base.UpdatePhase();
        currTime += Time.deltaTime;
        if (currTime >= timeLimit)
        {
            ChangePhase(PhaseID.Traps); // test
        }
    }
}

public class ScorePhase : MazeGamePhase
{
    public ScorePhase() { }

    public override void Enter()
    {
        base.Enter();
        // stop player inputs?
        // display score ui
        
    }

    public override void Exit()
    {
        base.Exit();
        // close score ui
    }
}



