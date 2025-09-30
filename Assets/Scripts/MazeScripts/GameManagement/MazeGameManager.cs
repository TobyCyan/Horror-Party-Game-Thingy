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

        currPhaseId.OnValueChanged += ChangePhase;

        if (currPhaseId.Value != PhaseID.Default)
        {
            ChangePhase(PhaseID.Default, currPhaseId.Value);
        }
    }

    private void Start()
    {
        if (IsServer)
        {
            currPhaseId.Value = PhaseID.Run;
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
