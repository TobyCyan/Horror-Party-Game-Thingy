using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class MarkManager : NetworkBehaviour
{
    public static MarkManager Instance;
    public static Player currentMarkedPlayer;

    // Events
    public event Action<ulong> OnMarkPassed;
    public event Action OnMarkedPlayerEliminated;
    public event Action OnGameStarted;

    [SerializeField] private AudioBroadcaster audioBroadcaster;
    // SFX settings
    [SerializeField] private AudioSettings markPassedSfxSettings;
    [SerializeField] private AudioSettings markReceivedSfxSettings;
    [SerializeField] private AudioSettings markedPlayerEliminatedSfxSettings;

    [SerializeField] private Timer postEliminationCoolDownTimer;
    public Timer PostEliminationCoolDownTimer => postEliminationCoolDownTimer;
    [Min(0.0f)]
    [SerializeField] private float postEliminateMarkPassingCooldown = 4.0f;
    [Min(0.0f)]
    [SerializeField] private float playerToPlayerMarkPassingCooldown = 2.0f;
    private float lastMarkPassTime = -Mathf.Infinity;
    [SerializeField] private float markedPlayerSpeedModifier = 1.25f;

    [SerializeField] private string auraLayerName = "Aura";
    int auraLayer;

    void Awake()
    {
        Instance = this;
        auraLayer = LayerMask.NameToLayer(auraLayerName);
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        OnMarkedPlayerEliminated += HandleMarkedPlayerEliminated;
        postEliminationCoolDownTimer.OnTimeUp += AssignNextPlayerWithMark;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;
        OnMarkedPlayerEliminated -= HandleMarkedPlayerEliminated;
        postEliminationCoolDownTimer.OnTimeUp -= AssignNextPlayerWithMark;
    }

    public void EliminateMarkedPlayer()
    {
        if (currentMarkedPlayer == null)
        {
            Debug.LogWarning("No marked player to eliminate.");
            return;
        }
        currentMarkedPlayer.EliminatePlayer();
    }

    public void StartHPGame()
    {
        AssignRandomPlayerWithMark();
        OnGameStarted?.Invoke();
    }

    public void StopHPGame()
    {
        if (currentMarkedPlayer != null)
        {
            currentMarkedPlayer.OnPlayerEliminated -= InvokeOnMarkedPlayerEliminated;
            currentMarkedPlayer = null;
        }

        // postEliminationCoolDownTimer.StopTimer();
    }

    private void HandleMarkedPlayerEliminated()
    {
        if (!IsServer) return;
        ResetMarkedPlayerRpc();
        Debug.Log("Marked player eliminated. Preparing to assign new marked player after cooldown.");

        if (PlayerManager.Instance.AlivePlayers.Count <= 1)
        {
            HotPotatoGameManager.Instance.EndGame();
            return;
        }

        // Start cooldown timer before assigning the new marked player
        postEliminationCoolDownTimer.StartTimer(postEliminateMarkPassingCooldown);
    }

    [Rpc(SendTo.Everyone)]
    private void ResetMarkedPlayerRpc()
    {
        if (currentMarkedPlayer != null)
        {
            currentMarkedPlayer.OnPlayerEliminated -= InvokeOnMarkedPlayerEliminated;
            currentMarkedPlayer = null;
        }
        audioBroadcaster.PlaySfxLocalToAll(markedPlayerEliminatedSfxSettings);
    }

    private void AssignNextPlayerWithMark()
    {
        // TODO: Should assign next player by least sabotage scores.
        if (!IsServer) return;

        // Find the player with the highest trap score
        ulong nextMarkClientId = 0;
        int lowestScore = int.MaxValue;
        List<ulong> tiedClientIds = new();

        foreach (var score in TrapScoreManager.Instance.GetAllPlayerScores())
        {
            if (score.trapScore < lowestScore)
            {
                lowestScore = score.trapScore;
                tiedClientIds.Clear();
                tiedClientIds.Add(score.clientId);
            }
            else if (score.trapScore == lowestScore)
            {
                tiedClientIds.Add(score.clientId);
            }
        }

        if (tiedClientIds.Count > 0)
        {
            int randomIndex = Random.Range(0, tiedClientIds.Count);
            nextMarkClientId = tiedClientIds[randomIndex];
        }

        // Assign the mark to the next player
        Player nextPlayer = PlayerManager.Instance.FindPlayerByClientId(nextMarkClientId);

        if (nextPlayer == null)
        {
            Debug.LogWarning("Unable to find players to assign the mark to.");
            return;
        }

        if (PlayerManager.Instance.AlivePlayers.Count <= 1)
        {
            StopHPGame();
            return;
        }
        
        PassMarkToPlayerServerRpc(nextPlayer.Id);
        Debug.Log($"Assigned mark to next player {nextPlayer} with id {nextPlayer.Id}");
    }

    public void AssignRandomPlayerWithMark()
    {
        Player randomPlayer = PlayerManager.Instance.GetRandomAlivePlayer();

        if (randomPlayer == null)
        {
            Debug.LogWarning("No alive players to assign the mark to.");
            return;
        }

        if (PlayerManager.Instance.AlivePlayers.Count <= 1)
        {
            HotPotatoGameManager.Instance.EndGame();
            return;
        }
        
        PassMarkToPlayerServerRpc(randomPlayer.clientId);
        Debug.Log("Assigned mark to random player " + randomPlayer + " with id " + randomPlayer.clientId);
    }

    public void PassMarkToPlayer(ulong fromClientId, ulong toClientId)
    {
        if (PlayerManager.Instance.AlivePlayers.Count <= 1)
        {
            HotPotatoGameManager.Instance.EndGame();
            return;
        }

        audioBroadcaster.PlaySfxLocal(markPassedSfxSettings, fromClientId);
        PassMarkToPlayerServerRpc(toClientId);
    }

    [Rpc(SendTo.Server)]
    private void PassMarkToPlayerServerRpc(ulong id, RpcParams rpcParams = default)
    {
        if (Time.time - lastMarkPassTime < playerToPlayerMarkPassingCooldown)
        {
            Debug.LogWarning("Mark passing is on cooldown.");
            return;
        }

        lastMarkPassTime = Time.time;
        UpdateMarkedPlayerAllRpc(id);
    }

    [Rpc(SendTo.Everyone)]
    private void UpdateMarkedPlayerAllRpc(ulong clientId)
    {
        Player player = PlayerManager.Instance.FindPlayerByClientId(clientId);
        if (player == null)
        {
            Debug.LogWarning($"[UpdateMarkedPlayerAllRpc] Could not find player with id {clientId}");
            return;
        }

        Debug.Log($"Passing mark to player {player} with id {clientId}");

        if (player.TryGetComponent(out PlayerMovement pm))
        {
            pm.SetMovementSpeedByModifier(markedPlayerSpeedModifier);
            Debug.Log($"Modified movement speed for new marked player {player}");
        }

        if (currentMarkedPlayer)
        {
            // Unsubscribe from previous marked player's elimination event
            currentMarkedPlayer.OnPlayerEliminated -= InvokeOnMarkedPlayerEliminated;
            if (currentMarkedPlayer.TryGetComponent(out PlayerMovement prevPm))
            {
                prevPm.ResetMovementSpeed();
                Debug.Log($"Resetting movement speed for previous marked player {currentMarkedPlayer}");
            }
            currentMarkedPlayer.ResetLayerRpc();
        }

        audioBroadcaster.PlaySfxLocal(markReceivedSfxSettings, clientId);
        currentMarkedPlayer = player;
        currentMarkedPlayer.OnPlayerEliminated += InvokeOnMarkedPlayerEliminated;
        currentMarkedPlayer.SetMeshRootLayerRpc(auraLayer);

        Debug.Log($"Updated marked player to {currentMarkedPlayer} with id {clientId}");
        OnMarkPassed?.Invoke(clientId);
    }

    private void InvokeOnMarkedPlayerEliminated()
    {
        OnMarkedPlayerEliminated?.Invoke();
    }

    public static bool IsPlayerMarked(ulong clientId)
    {
        return currentMarkedPlayer != null && currentMarkedPlayer.clientId == clientId;
    }

    private void Update()
    {
        if (currentMarkedPlayer)
        {
            currentMarkedPlayer.float0 += Time.deltaTime;
        }
    }
}
