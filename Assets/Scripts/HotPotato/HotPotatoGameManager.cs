using Unity.Netcode;
using UnityEngine;

public class HotPotatoGameManager : NetworkBehaviour
{
    [SerializeField] private MarkManager markManager;
    [Min(0.0f)]
    [SerializeField] private float hotPotatoDuration = 30.0f;
    [SerializeField] private Timer hotPotatoTimer;
    private bool isGameActive = true;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        if (markManager != null)
        {
            markManager.OnMarkedPlayerEliminated += HandleMarkedPlayerEliminated;
            markManager.OnMarkPassed += HandleMarkPassed;
        }

        if (hotPotatoTimer != null)
        {
            hotPotatoTimer.OnTimeUp += HotPotatoTimer_OnTimeUp;
            hotPotatoTimer.StartTimer(hotPotatoDuration);
        }

        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnAllPlayersEliminated += EndGame;
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
            markManager.OnMarkPassed -= HandleMarkPassed;
            markManager.StopHPGame();
        }

        if (hotPotatoTimer != null)
        {
            hotPotatoTimer.OnTimeUp -= HotPotatoTimer_OnTimeUp;
            hotPotatoTimer.StopTimer();
        }

        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnAllPlayersEliminated -= EndGame;
        }
    }

    private void EndGame()
    {
        isGameActive = false;
        OnNetworkDespawn();
        Debug.Log("Hot Potato game ended.");
    }

    private void HandleMarkPassed(ulong _)
    {
        // Reset the timer when the mark is passed
        if (hotPotatoTimer != null)
        {
            hotPotatoTimer.StartTimer(hotPotatoDuration);
        }
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
