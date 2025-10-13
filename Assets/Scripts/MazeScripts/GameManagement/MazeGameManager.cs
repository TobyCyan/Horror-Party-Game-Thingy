using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;

public class MazeGameManager : NetworkBehaviour
{
    public static MazeGameManager Instance;

    private NetworkVariable<PhaseID> currPhaseId = new(
       PhaseID.Default,
       NetworkVariableReadPermission.Everyone,
       NetworkVariableWritePermission.Server
    );

    private NetworkVariable<float> currPhaseTimer = new(
        0f,
        NetworkVariableReadPermission.Everyone,
       NetworkVariableWritePermission.Server
    );

    public float GetTimeRemaining()
    {
        return currPhaseTimer.Value;
    }

    public MazeGamePhase currPhase;


    void Awake()
    {
        // enforce singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        NetworkManager.SceneManager.OnUnloadEventCompleted += OnSceneUnloaded;
    }

    private void Start()
    {
        StartCoroutine(DelayedStart());
    }


    private IEnumerator DelayedStart()
    {
        // sorry
        yield return new WaitUntil(() =>
            MazeCameraManager.Instance.localPlayerCam != null &&
            PlayerManager.Instance.localPlayer != null
        );


        if (IsServer)
        {
            currPhaseId.Value = PhaseID.Traps;
        }

        currPhaseId.OnValueChanged += ChangePhase;

        // latejoin client
        if (currPhaseId.Value != PhaseID.Default)
        {
            ChangePhase(PhaseID.Default, currPhaseId.Value);
        }
    }

    // SERVER-ONLY: call this to change the phase
    public void ServerChangePhase(PhaseID next)
    {
        if (!IsServer) return; 
        currPhaseId.Value = next;
    }

    // id change -> actual phase change
    private void ChangePhase(PhaseID prev, PhaseID next)
    {
        if (prev == next) return;
        currPhase?.Exit();
        switch(next)
        {
            case PhaseID.Traps:
                currPhase = new TrapPhase();
            break;
            case PhaseID.Run:
                currPhase = new RunPhase();
            break;
            case PhaseID.Score:
                currPhase = new ScorePhase();
            break;
        }
        if (IsServer) currPhaseTimer.Value = currPhase.TimeLimit;
        currPhase.Enter();
    }

    private void Update()
    {
        if (currPhase == null) return;

        currPhase.UpdatePhase();

        if (IsServer)
        {
            currPhaseTimer.Value -= Time.deltaTime;

            if (currPhaseTimer.Value <= 0)
            {
                currPhaseId.Value = currPhase.GetNextPhase();
            }
        }

    }

    private void OnSceneUnloaded(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        NetworkManager.SceneManager.OnUnloadEventCompleted -= OnSceneUnloaded;
        currPhaseId.OnValueChanged -= ChangePhase;
    }
}
