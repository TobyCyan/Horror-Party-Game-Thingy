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
    private readonly NetworkVariable<bool> isGameActive = new(false);

    void Awake()
    {
        Instance = this;
        PlayerManager.OnLocalPlayerSet += PassHpComponentToPlayer;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        if (markManager != null)
        {
            markManager.OnMarkedPlayerEliminated += HandleMarkedPlayerEliminated;
            markManager.OnGameStarted += StartHPTimer;
            PlayerManager.OnAllPlayersLoaded += markManager.StartHPGame;
            markManager.OnGameStarted += MarkManager_OnGameStarted;
        }

        PlayerManager.OnLastPlayerStanding += EndGame;

        if (hotPotatoTimer != null)
        {
            hotPotatoTimer.OnTimeUp += HotPotatoTimer_OnTimeUp;
        }

        if (markManager != null && hotPotatoTimer != null)
        {
            markManager.PostEliminationCoolDownTimer.OnTimeUp += StartHPTimer;
        }


        if (markManager == null)
        {
            Debug.LogError("MarkManager reference is missing in HotPotatoGameManager.");
            return;
        }
    }

    private void MarkManager_OnGameStarted()
    {
        isGameActive.Value = true;
    }

    public override void OnNetworkDespawn()
    {
        if (markManager != null)
        {
            markManager.OnMarkedPlayerEliminated -= HandleMarkedPlayerEliminated;
            markManager.OnGameStarted -= StartHPTimer;
            markManager.StopHPGame();
            PlayerManager.OnAllPlayersLoaded -= markManager.StartHPGame;
            markManager.OnGameStarted -= MarkManager_OnGameStarted;
        }

        if (hotPotatoTimer != null)
        {
            hotPotatoTimer.OnTimeUp -= HotPotatoTimer_OnTimeUp;
            hotPotatoTimer.StopTimer();
        }

        if (markManager != null && hotPotatoTimer != null)
        {
            markManager.PostEliminationCoolDownTimer.OnTimeUp -= StartHPTimer;
        }

        PlayerManager.OnLocalPlayerSet -= PassHpComponentToPlayer;
        PlayerManager.OnLastPlayerStanding -= EndGame;
    }

    public async void EndGame()
    {
        isGameActive.Value = false;
        Debug.Log("Hot Potato game ended.");
        GetComponent<NetworkObject>().Despawn();
        markManager.StopHPGame();

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

    private void PassHpComponentToPlayer(Player player)
    {
        if (player == null)
        {
            Debug.LogWarning("Cannot pass Hot Potato component to a null local player.");
            return;
        }
        if (!player.TryGetComponent<HPPassingLogic>(out var _))
        {
            Debug.Log($"Adding HPPassingLogic component to player {player}.");
            player.gameObject.AddComponent<HPPassingLogic>();
        }
    }

    private void Update()
    {
        if (!isGameActive.Value) return;

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

    private void StartHPTimer()
    {
        hotPotatoTimer.StartTimer(hotPotatoDuration);
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
