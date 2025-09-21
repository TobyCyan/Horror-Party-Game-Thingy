using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Unity.Cinemachine;
using System;
public class MazeGameManager : NetworkBehaviour
{
    public static MazeGameManager Instance;

    private NetworkVariable<PhaseID> currPhaseId = new(
       PhaseID.Default,
       NetworkVariableReadPermission.Everyone,
       NetworkVariableWritePermission.Server
   );

    private MazeGamePhase currPhase;
    // TESTING
    private float phaseTimer = 0f;
    void Awake()
    {
        // enforce singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        NetworkManager.SceneManager.OnLoadEventCompleted += OnSceneLoaded;
        NetworkManager.SceneManager.OnUnloadEventCompleted += OnSceneUnloaded;
    }


    private void OnSceneLoaded(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        NetworkManager.SceneManager.OnLoadEventCompleted -= OnSceneLoaded;

        if (IsServer)
        {
            currPhaseId.Value = PhaseID.Traps;
        }

        currPhaseId.OnValueChanged += ChangePhase;

        if (currPhaseId.Value != PhaseID.Default)
        {
            ChangePhase(PhaseID.Default, currPhaseId.Value);
        }
    }

    // id change -> actual phase change
    public void ChangePhase(PhaseID prev, PhaseID next)
    {
        currPhase?.Exit();
        switch(next)
        {
            case PhaseID.Default:
                currPhase = new DefaultPhase();
            break;
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

        currPhase.Enter();
    }

    private void Update()
    {
        currPhase?.UpdatePhase();
        // uncomment below to test for now

        //if (!IsServer) return;

        //phaseTimer += Time.deltaTime;
        //if (phaseTimer >= 3f)
        //{
        //    phaseTimer = 0f;
        //    ToggleTrapRun();
        //}
    }

    // test, del later
    private void ToggleTrapRun()
    {
        if (currPhaseId.Value == PhaseID.Traps)
        {
            currPhaseId.Value = PhaseID.Run;
        }
        else if (currPhaseId.Value == PhaseID.Run)
        {
            currPhaseId.Value = PhaseID.Traps;
        }
    }


    private void OnSceneUnloaded(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        NetworkManager.SceneManager.OnUnloadEventCompleted -= OnSceneUnloaded;
        currPhaseId.OnValueChanged -= ChangePhase;
    }

    private void EndMinigame()
    {
        // send scores and win info to persistent game manager?
    }
}
