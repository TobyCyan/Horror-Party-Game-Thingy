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

    // SFX
    [SerializeField] private AudioSamples gameWinAudioSamples;
    [SerializeField] private AudioSamples gameLoseAudioSamples;
    [SerializeField] private AudioSource audioSource;

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
            markManager.OnMarkedPlayerEliminated += MarkManager_OnMarkedPlayerEliminated;
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

    private void MarkManager_OnMarkedPlayerEliminated()
    {
        // Pause timer so won't accidentally eliminate next player during cool down
        hotPotatoTimer.StopTimer();
    }

    private void MarkManager_OnGameStarted()
    {
        isGameActive.Value = true;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;

        if (markManager != null)
        {
            markManager.OnMarkedPlayerEliminated -= HandleMarkedPlayerEliminated;
            markManager.OnMarkedPlayerEliminated -= MarkManager_OnMarkedPlayerEliminated;
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
        Debug.Log("Hot Potato game ended.");
        markManager.StopHPGame();

        if (IsServer)
        {
            isGameActive.Value = false;

            // Play SFX for all players
            PlayGameEndSfxRpc();

            // TODO: Show game end UI

            // Wait a bit before despawning to let players see the final state
            await Task.Delay(5000);

            // Despawn everyone
            DespawnPlayerRpc();

            // ScoreUiManager.Instance.ShowFinalScore();
            await Task.Delay(1000);
            
            GetComponent<NetworkObject>().Despawn();
            await SceneLifetimeManager.Instance.UnloadSceneNetworked(SceneManager.GetActiveScene().name);
            await SceneLifetimeManager.Instance.ReturnToLobby();
        }
    }

    [Rpc(SendTo.Everyone)]
    private void DespawnPlayerRpc()
    {
        Player localPlayer = PlayerManager.Instance.FindPlayerByClientId(NetworkManager.Singleton.LocalClientId);
        if (localPlayer != null)
        {
            SpawnManager.Instance.DespawnPlayerServerRpc(localPlayer.Id);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void PlayGameEndSfxRpc()
    {
        bool isWin = PlayerManager.Instance.IsPlayerAlive(NetworkManager.Singleton.LocalClientId);
        AudioSamples samples = isWin ? gameWinAudioSamples : gameLoseAudioSamples;
        
        if (audioSource != null && samples != null && samples.Count > 0)
        {
            AudioClip clip = samples.PickRandom();
            audioSource.loop = false;
            audioSource.PlayOneShot(clip);
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
