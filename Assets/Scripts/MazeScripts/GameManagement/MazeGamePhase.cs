using UnityEngine;
public abstract class MazeGamePhase
{
    public int phaseId;

    public virtual void Enter()
    {
        Debug.Log(string.Format("Entering phase {0}", phaseId));
    }

    public virtual void Exit()
    {
        Debug.Log(string.Format("Exiting phase {0}", phaseId));
    }

    public virtual void UpdatePhase() { }
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

}

public class TrapPhase : MazeGamePhase
{
    private Camera fpsCam;
    private Camera topdownCam;
    public TrapPhase(Camera topdown)
    {
        topdownCam = topdown;
    }

    public override void Enter()
    {
        base.Enter();
        fpsCam = Camera.main;
        fpsCam.enabled = false;
        topdownCam.enabled = true;
        
    }

    public override void Exit()
    {
        fpsCam.enabled = true;
        topdownCam.enabled = false;
        base.Exit();
    }
}

public class RunPhase : MazeGamePhase
{

}

public class ScorePhase : MazeGamePhase
{

}



