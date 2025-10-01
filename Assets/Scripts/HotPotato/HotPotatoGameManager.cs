using Unity.Netcode;
using UnityEngine;

public class HotPotatoGameManager : NetworkBehaviour
{
    public static HotPotatoGameManager Instance;
    
    [SerializeField] private MarkManager markManager;
    [Min(0.0f)]
    [SerializeField] private float hotPotatoDuration = 30.0f;
    [SerializeField] private Timer hotPotatoTimer;
    public NetworkVariable<float> timer = new();
    private bool isGameActive = true;

    public void Awake()
    {
        Instance = this;
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
            hotPotatoTimer.StartTimer(hotPotatoDuration);
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

        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnLastPlayerStanding -= EndGame;
        }
    }

    private void EndGame()
    {
        isGameActive = false;
        Debug.Log("Hot Potato game ended.");
        GetComponent<NetworkObject>().Despawn();
    }

    private void HotPotatoTimer_OnTimeUp()
    {
        if (markManager != null)
        {
            markManager.EliminateMarkedPlayer();
        }
    }

    private void Update()
    {
        if (!isGameActive) return;

        if (IsServer)
        {
            timer.Value = hotPotatoTimer.CurrentTime;
        }
        // For testing purposes
        if (Input.GetKeyDown(KeyCode.K))
        {
            if (markManager != null)
            {
                markManager.EliminateMarkedPlayer();
            }
        }
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
