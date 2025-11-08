using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class MarkManager : NetworkBehaviour
{
    public static MarkManager Instance;
    private static readonly ulong INVALID_CLIENT_ID = ulong.MaxValue;
    public static NetworkVariable<ulong> CurrentMarkedPlayerClientId = new(
        INVALID_CLIENT_ID,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
        );

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

    // Track the last marked player to properly unsubscribe
    private Player lastMarkedPlayer = null;

    void Awake()
    {
        Instance = this;
        auraLayer = LayerMask.NameToLayer(auraLayerName);
    }

    public override void OnNetworkSpawn()
    {
        // All clients should observe changes to the marked player
        CurrentMarkedPlayerClientId.OnValueChanged += HandleMarkedPlayerChanged;

        if (!IsServer) return;
        OnMarkedPlayerEliminated += HandleMarkedPlayerEliminated;
        postEliminationCoolDownTimer.OnTimeUp += AssignNextPlayerWithMark;
    }

    public override void OnNetworkDespawn()
    {
        CurrentMarkedPlayerClientId.OnValueChanged -= HandleMarkedPlayerChanged;

        // Clean up last marked player subscription
        if (lastMarkedPlayer != null)
        {
            lastMarkedPlayer.OnPlayerEliminated -= InvokeOnMarkedPlayerEliminated;
            lastMarkedPlayer = null;
        }

        if (!IsServer) return;
        OnMarkedPlayerEliminated -= HandleMarkedPlayerEliminated;
        postEliminationCoolDownTimer.OnTimeUp -= AssignNextPlayerWithMark;
    }

    public void EliminateMarkedPlayer()
    {
        if (!IsServer)
        {
            Debug.LogWarning("[MarkManager] Only server can eliminate marked player.");
            return;
        }

        if (CurrentMarkedPlayerClientId.Value == INVALID_CLIENT_ID)
        {
            Debug.LogWarning("[MarkManager] No marked player to eliminate.");
            return;
        }

        Player currentMarkedPlayer = GetMarkedPlayer();
        if (currentMarkedPlayer != null)
        {
            Debug.Log($"[MarkManager] Eliminating marked player {currentMarkedPlayer.clientId}");
            currentMarkedPlayer.EliminatePlayerServerRpc();
        }
    }

    public bool IsMarkedPlayerValid()
    {
        return CurrentMarkedPlayerClientId.Value != INVALID_CLIENT_ID;
    }

    public void StartHPGame()
    {
        // Set position again anyway
        PlayerManager.Instance?.players.ForEach(
            player => player.transform.SetPositionAndRotation(
                SpawnManager.Instance.spawnPositions[(int)player.clientId].position, player.transform.rotation));
        
        AssignRandomPlayerWithMark();
        OnGameStarted?.Invoke();
    }

    public void StopHPGame()
    {
        if (!IsServer) return;

        if (lastMarkedPlayer != null)
        {
            lastMarkedPlayer.OnPlayerEliminated -= InvokeOnMarkedPlayerEliminated;
            lastMarkedPlayer = null;
        }

        postEliminationCoolDownTimer.StopTimer();

        if (IsServer)
        {
            CurrentMarkedPlayerClientId.Value = INVALID_CLIENT_ID;
        }
    }

    private void HandleMarkedPlayerEliminated()
    {
        Debug.Log("[MarkManager - Server] Handling marked player elimination.");
        if (!IsServer) return;

        // Play sound and reset visuals before checking game state
        audioBroadcaster.PlaySfxLocalToAll(markedPlayerEliminatedSfxSettings);
        ResetMarkedPlayerVisualsRpc();

        // Wait a frame to ensure PlayerManager has processed the elimination
        StartCoroutine(ProcessEliminationAfterDelay());
    }

    private IEnumerator ProcessEliminationAfterDelay()
    {
        yield return null; // Wait one frame

        int aliveCount = PlayerManager.Instance.GetAlivePlayers().Count;
        Debug.Log($"[MarkManager] Alive players after elimination: {aliveCount}");

        if (aliveCount <= 1)
        {
            Debug.Log("[MarkManager] Game ending - 1 or fewer players remaining.");
            HotPotatoGameManager.Instance.EndGame();
            yield break;
        }

        Debug.Log("[MarkManager] Starting cooldown timer before assigning new marked player.");
        postEliminationCoolDownTimer.StartTimer(postEliminateMarkPassingCooldown);
    }

    [Rpc(SendTo.Everyone)]
    private void ResetMarkedPlayerVisualsRpc()
    {
        Player previousMarkedPlayer = GetMarkedPlayer();

        if (previousMarkedPlayer == null)
        {
            Debug.LogWarning("[MarkManager] No marked player to reset visuals for.");
            return;
        }

        Debug.Log($"[MarkManager] Resetting visuals for player {previousMarkedPlayer.clientId}");

        // Reset movement speed
        if (previousMarkedPlayer.TryGetComponent(out PlayerMovement prevPm))
        {
            prevPm.ResetMovementSpeed();
        }

        // Reset layer
        previousMarkedPlayer.ResetLayerRpc();

        // Server clears the marked player ID after visuals are reset
        if (IsServer)
        {
            // Unsubscribe from the eliminated player
            if (lastMarkedPlayer != null)
            {
                lastMarkedPlayer.OnPlayerEliminated -= InvokeOnMarkedPlayerEliminated;
                lastMarkedPlayer = null;
            }

            CurrentMarkedPlayerClientId.Value = INVALID_CLIENT_ID;
        }
    }

    private void AssignNextPlayerWithMark()
    {
        if (!IsServer) return;

        int aliveCount = PlayerManager.Instance.GetAlivePlayers().Count;
        Debug.Log($"[MarkManager] Assigning next mark. Alive players: {aliveCount}");

        if (aliveCount <= 1)
        {
            Debug.Log("[MarkManager] Cannot assign mark - game should be ending.");
            HotPotatoGameManager.Instance.EndGame();
            return;
        }

        // Find the player with the lowest trap score
        ulong nextMarkClientId = 0;
        int lowestScore = int.MaxValue;
        List<ulong> tiedClientIds = new();

        foreach (var score in TrapScoreManager.Instance.GetAllPlayerScores())
        {
            // Only consider alive players
            Player player = PlayerManager.Instance.FindPlayerByClientId(score.clientId);
            if (player == null || player.IsEliminated)
                continue;

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
        else
        {
            Debug.LogWarning("[MarkManager] No valid players found in trap scores. Selecting random alive player.");
            Player randomPlayer = PlayerManager.Instance.GetRandomAlivePlayer();
            if (randomPlayer != null)
            {
                nextMarkClientId = randomPlayer.clientId;
            }
        }

        // Assign the mark to the next player
        Player nextPlayer = PlayerManager.Instance.FindPlayerByClientId(nextMarkClientId);

        if (nextPlayer == null)
        {
            Debug.LogWarning("[MarkManager] Unable to find player to assign the mark to.");
            return;
        }

        PassMarkToPlayerServerRpc(nextPlayer.clientId);
        Debug.Log($"[MarkManager] Assigned mark to next player {nextPlayer.name} (clientId: {nextPlayer.clientId})");
    }

    public void AssignRandomPlayerWithMark()
    {
        Player randomPlayer = PlayerManager.Instance.GetRandomAlivePlayer();

        if (randomPlayer == null)
        {
            Debug.LogWarning("[MarkManager] No alive players to assign the mark to.");
            return;
        }

        int aliveCount = PlayerManager.Instance.GetAlivePlayers().Count;
        if (aliveCount <= 1)
        {
            Debug.Log("[MarkManager] Only 1 player - cannot start game.");
            HotPotatoGameManager.Instance.EndGame();
            return;
        }

        PassMarkToPlayerServerRpc(randomPlayer.clientId);
        Debug.Log($"[MarkManager] Assigned mark to random player {randomPlayer.name} (clientId: {randomPlayer.clientId})");
    }

    public void PassMarkToPlayer(ulong fromClientId, ulong toClientId)
    {
        audioBroadcaster.PlaySfxLocal(markPassedSfxSettings, fromClientId);
        PassMarkToPlayerServerRpc(toClientId);
    }

    [Rpc(SendTo.Server)]
    private void PassMarkToPlayerServerRpc(ulong clientId)
    {
        if (Time.time - lastMarkPassTime < playerToPlayerMarkPassingCooldown)
        {
            Debug.LogWarning("[MarkManager] Mark passing is on cooldown.");
            return;
        }

        Player markedPlayer = PlayerManager.Instance.FindPlayerByClientId(clientId);

        if (markedPlayer == null)
        {
            Debug.LogWarning($"[MarkManager] Player with clientId {clientId} not found.");
            return;
        }

        // Verify player is alive
        if (markedPlayer.IsEliminated)
        {
            Debug.LogWarning($"[MarkManager] Cannot mark eliminated player {clientId}.");
            return;
        }

        Debug.Log($"[MarkManager] Passing mark to player {markedPlayer.name} (clientId: {clientId})");

        lastMarkPassTime = Time.time;

        // Update state � NGO will sync it automatically to all clients
        CurrentMarkedPlayerClientId.Value = clientId;

        // Apply server-side effects
        if (markedPlayer.TryGetComponent(out PlayerMovement pm))
        {
            pm.SetMovementSpeedByModifier(markedPlayerSpeedModifier);
        }

        Debug.Log($"[MarkManager] Updated marked player to {markedPlayer.name} (clientId: {clientId})");
    }

    private void HandleMarkedPlayerChanged(ulong oldId, ulong newId)
    {
        Debug.Log($"[MarkManager] Marked player changed from {oldId} -> {newId}");

        // Unsubscribe from previous marked player
        if (lastMarkedPlayer != null)
        {
            lastMarkedPlayer.OnPlayerEliminated -= InvokeOnMarkedPlayerEliminated;
            Debug.Log($"[MarkManager] Unsubscribed from previous marked player {lastMarkedPlayer.clientId}");
        }

        // Clear visuals on old marked player (if different from new)
        if (oldId != INVALID_CLIENT_ID && oldId != newId)
        {
            Player oldPlayer = PlayerManager.Instance.FindPlayerByClientId(oldId);
            if (oldPlayer != null)
            {
                if (oldPlayer.TryGetComponent(out PlayerMovement oldPm))
                {
                    oldPm.ResetMovementSpeed();
                }
                oldPlayer.ResetLayerRpc();
            }
        }

        if (newId == INVALID_CLIENT_ID)
        {
            lastMarkedPlayer = null;
            return;
        }

        Player newMarkedPlayer = PlayerManager.Instance.FindPlayerByClientId(newId);
        if (newMarkedPlayer == null)
        {
            Debug.LogWarning($"[MarkManager] Could not find player {newId}");
            lastMarkedPlayer = null;
            return;
        }

        // Subscribe to new marked player's elimination
        lastMarkedPlayer = newMarkedPlayer;
        lastMarkedPlayer.OnPlayerEliminated += InvokeOnMarkedPlayerEliminated;
        Debug.Log($"[MarkManager] Subscribed to new marked player {lastMarkedPlayer.clientId}");

        // Apply client-side visuals
        newMarkedPlayer.SetMeshRootLayerRpc(auraLayer);
        audioBroadcaster.PlaySfxLocal(markReceivedSfxSettings, newId);
        OnMarkPassed?.Invoke(newId);
    }

    private void InvokeOnMarkedPlayerEliminated()
    {
        Debug.Log("[MarkManager] Marked player eliminated event triggered.");
        OnMarkedPlayerEliminated?.Invoke();
    }

    public static bool IsPlayerMarked(ulong clientId)
    {
        return CurrentMarkedPlayerClientId.Value == clientId;
    }

    public Player GetMarkedPlayer()
    {
        if (CurrentMarkedPlayerClientId.Value == INVALID_CLIENT_ID)
            return null;
        return PlayerManager.Instance.FindPlayerByClientId(CurrentMarkedPlayerClientId.Value);
    }

    private void Update()
    {
        Player currentMarkedPlayer = GetMarkedPlayer();
        if (currentMarkedPlayer != null)
        {
            currentMarkedPlayer.float0 += Time.deltaTime;
        }
    }
}