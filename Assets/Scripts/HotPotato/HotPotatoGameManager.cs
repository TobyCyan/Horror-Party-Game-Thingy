using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HotPotatoGameManager : NetworkBehaviour
{
    public static HotPotatoGameManager Instance;
    
    [SerializeField] private MarkManager markManager;
    [Min(0.0f)]
    [SerializeField] private float hotPotatoDuration = 30.0f;
    [SerializeField] private Timer hotPotatoTimer;
    public NetworkVariable<float> timer = new();
    private bool isGameActive = true;

    void Awake()
    {
        NetworkManager.SceneManager.OnLoadEventCompleted += OnSceneLoaded;
        Instance = this;
    }

    private void OnSceneLoaded(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        NetworkManager.SceneManager.OnLoadEventCompleted -= OnSceneLoaded;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        if (markManager != null)
        {
            markManager.OnMarkedPlayerEliminated += HandleMarkedPlayerEliminated;
        }

        if (hotPotatoTimer != null)
        {
            hotPotatoTimer.OnTimeUp += HotPotatoTimer_OnTimeUp;
        }

        if (markManager != null && hotPotatoTimer != null)
        {
            markManager.OnMarkPassed += (_) => hotPotatoTimer.StartTimer(hotPotatoDuration);
        }

        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnLastPlayerStanding += EndGame;
        }

        if (markManager == null)
        {
            Debug.LogError("MarkManager reference is missing in HotPotatoGameManager.");
            return;
        }

        markManager.StartHPGame();
    }

    public override void OnNetworkDespawn()
    {
        if (markManager != null)
        {
            markManager.OnMarkedPlayerEliminated -= HandleMarkedPlayerEliminated;
            markManager.StopHPGame();
        }

        if (hotPotatoTimer != null)
        {
            hotPotatoTimer.OnTimeUp -= HotPotatoTimer_OnTimeUp;
            hotPotatoTimer.StopTimer();
        }

        if (markManager != null && hotPotatoTimer != null)
        {
            markManager.OnMarkPassed -= (_) => hotPotatoTimer.StartTimer(hotPotatoDuration);
        }

        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnLastPlayerStanding -= EndGame;
        }
    }

    private async void EndGame()
    {
        isGameActive = false;
        Debug.Log("Hot Potato game ended.");
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

            // ScoreUiManager.Instance.ShowFinalScore();
            await Task.Delay(1000);
            
            await SceneLifetimeManager.Instance.UnloadSceneNetworked(SceneManager.GetActiveScene().name);
            await SceneLifetimeManager.Instance.ReturnToLobby();
        }
    }

    private void HotPotatoTimer_OnTimeUp()
    {
        if (markManager != null)
        {
            markManager.EliminateMarkedPlayer();
        }
        // hotPotatoTimer.StartTimer(hotPotatoDuration);
    }

    private void Update()
    {
        if (!isGameActive) return;

        if (IsServer)
        {
            timer.Value = hotPotatoTimer.CurrentTime;
        }
        // For testing purposes
        /*if (Input.GetKeyDown(KeyCode.K))
        {
            if (markManager != null)
            {
                markManager.EliminateMarkedPlayer();
            }
        }*/
    }

    private void HandleMarkedPlayerEliminated()
    {
        Debug.Log("Marked player eliminated. Handling elimination logic.");
        // TODO: Notify players by ui, update scores, etc.
    }

    private void OnValidate()
    {
        if (markManager == null)
        {
            Debug.LogWarning($"MarkManager reference is missing in HotPotatoGameManager: {name}");
        }

        if (hotPotatoTimer == null)
        {
            Debug.LogWarning($"Timer reference is missing in HotPotatoGameManager: {name}");
        }
    }
}
