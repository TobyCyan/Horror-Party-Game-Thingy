using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

// these could probably be instances
public abstract class MazeGamePhase
{
    protected PhaseID id; // laze
    public float TimeLimit { get; protected set; }
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
        MazeGameManager.Instance.ServerChangePhase(next);
    }

    public abstract PhaseID GetNextPhase();
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
    public override PhaseID GetNextPhase()
    {
        throw new System.NotImplementedException();
    }
}

public class TrapPhase : MazeGamePhase
{

    int cost;
    Player player;
    PlayerMovement movement;

    public TrapPhase()
    {
        TimeLimit = 5f;
        cost = 20;
    }

    public TrapPhase(float timeLimit, int initCost)
    {
        this.TimeLimit = timeLimit;
        this.cost = initCost;
    }

    public override void Enter()
    {
        base.Enter();


        MazeCameraManager.Instance.SetToTopDownView();
        UIManager.Instance.SwitchUIView<TrapsPhaseView>();

        MazeTrapPlacer.Instance.EnablePlacing(true, cost);

        player = PlayerManager.Instance.localPlayer; // handle for local
        movement = player.gameObject.GetComponent<PlayerMovement>();
        movement.enabled = false;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;



    }

    public override void Exit()
    {

        base.Exit();
        UIManager.Instance.HideCurrentView();

        MazeTrapPlacer.Instance.FinaliseTraps();
        MazeTrapPlacer.Instance.EnablePlacing(false);

        movement.enabled = true;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public override PhaseID GetNextPhase()
    {
        return PhaseID.Run;
    }
}

public class RunPhase : MazeGamePhase
{


    public RunPhase()
    {
        this.TimeLimit = 5f;
    }

    public RunPhase(float timeLimit)
    {
        this.TimeLimit = timeLimit;
    }

    public override void Enter()
    {
        base.Enter();
        MazeCameraManager.Instance.SetToPlayerView();
        UIManager.Instance.SwitchUIView<RunPhaseView>();
    }
    public override void Exit()
    {
        UIManager.Instance.HideCurrentView();
        base.Exit();
    }

    public override PhaseID GetNextPhase()
    {
        return PhaseID.Score;
    }

}

public class ScorePhase : MazeGamePhase
{
    bool isFinal; // transition to some teardown phase for ending the minigame maybe
    
    public ScorePhase() {
        TimeLimit = 2f;
    }

    public override void Enter()
    {
        base.Enter();
        if (NetworkManager.Singleton.IsServer)
        {
            MazeScoreManager.Instance.CalculateBonusesAndBroadcast();
        }
        // stop player inputs?
        // display score ui
    }

    public override void Exit()
    {
        base.Exit();
        MazeGameManager.Instance.StartNextRound();
        // close score ui
        // respawn everyone at spawn point
        // reset player states (health etc) if needed
    }

    public override PhaseID GetNextPhase()
    {
        return PhaseID.Traps;
    }

}



