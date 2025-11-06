using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;

public class MazeGameManager : NetworkBehaviour
{
    public static MazeGameManager Instance;

    private NetworkVariable<PhaseID> currPhaseId = new(
       PhaseID.Default,
       NetworkVariableReadPermission.Everyone,
       NetworkVariableWritePermission.Server
    );


    private NetworkVariable<int> currRound = new(
       3,
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

    public void StartNextRound()
    {
        if (IsServer) currRound.Value--;
        if (currRound.Value <= 0)
        {
            EndGame();

        } else
        {

            // Despawn if not dead, and respawn
            if (IsServer)
            {
                int playerCount = PlayerManager.Instance.players.Count;
                // Despawn everyone
                for (int i = 0; i < playerCount; i++)
                {
                    if (PlayerManager.Instance.FindPlayerByClientId((ulong)i))
                        SpawnManager.Instance.DespawnPlayerServerRpc(PlayerManager.Instance.FindPlayerByClientId((ulong)i).Id);
                }
            }

            // run on all client..?
            SpawnManager.Instance.SpawnPlayersServerRpc();

        }
    }



    private async void EndGame()
    {
        Debug.Log("Maze game ended.");
        GetComponent<NetworkObject>().Despawn();

        if (IsServer)
        {
            int playerCount = PlayerManager.Instance.players.Count;
            // Despawn everyone
            for (int i = 0; i < playerCount; i++)
            {
                if (PlayerManager.Instance.FindPlayerByClientId((ulong)i))
                    SpawnManager.Instance.DespawnPlayerServerRpc(PlayerManager.Instance.FindPlayerByClientId((ulong)i).Id);
            }
            await Task.Delay(1000);

            await SceneLifetimeManager.Instance.UnloadSceneNetworked(SceneManager.GetActiveScene().name);
            await SceneLifetimeManager.Instance.ReturnToLobby();
        }
    }

    private void Update()
    {
        
        if (currPhase == null || currRound.Value <= 0) return;

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
