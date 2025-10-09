using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

// these could probably be instances
public abstract class MazeGamePhase
{
    protected PhaseID id; // laze
    public float timeRemaining;
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

    int cost;
    float currTime = 0f;
    Player player;
    PlayerMovement movement;

    public TrapPhase()
    {
        timeRemaining = 10f;
        cost = 20;
    }

    public TrapPhase(float timeLimit, int initCost)
    {
        this.timeRemaining = timeLimit;
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

    public override void UpdatePhase()
    {
        base.UpdatePhase();
        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0)
        {
            ChangePhase(PhaseID.Run);
        }
    }
}

public class RunPhase : MazeGamePhase
{


    public RunPhase()
    {
        this.timeRemaining = 500f;
    }

    public RunPhase(float timeLimit)
    {
        this.timeRemaining = timeLimit;
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

    public override void UpdatePhase()
    {
        base.UpdatePhase();
        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0)
        {
            ChangePhase(PhaseID.Score); // or Results
        }
    }
}

public class ScorePhase : MazeGamePhase
{
    public ScorePhase() {
        timeRemaining = 5f;

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
        // close score ui
        // respawn everyone at spawn point
        // reset player states (health etc) if needed
    }

    public override void UpdatePhase()
    {
        base.UpdatePhase();
        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0)
        {
            ChangePhase(PhaseID.Traps); 
        }
    }
}



