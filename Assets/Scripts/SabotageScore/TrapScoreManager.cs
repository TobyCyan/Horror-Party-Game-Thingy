using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TrapScoreManager : NetworkBehaviour
{
    public static TrapScoreManager Instance;

    [Header("Scoring Settings")]
    [SerializeField] private int pointsPerTrapTriggered = 1;

    [SerializeField] private List<PlayerTrapScore> debugPlayerScores;

    // Dictionary to track scores for each player (clientId -> score)
    private NetworkList<PlayerTrapScore> playerScores;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Initialize the NetworkList
        playerScores = new NetworkList<PlayerTrapScore>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Subscribe to player join/leave events if you have them
        PlayerManager.OnPlayerAdded += OnPlayerJoined;
        PlayerManager.OnPlayerRemoved += OnPlayerLeft;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        PlayerManager.OnPlayerAdded -= OnPlayerJoined;
        PlayerManager.OnPlayerRemoved -= OnPlayerLeft;
    }

    /// <summary>
    /// Called when a player joins the game to initialize their score
    /// </summary>
    private void OnPlayerJoined(Player player)
    {
        if (!IsServer) return;

        // Check if player already has a score entry
        for (int i = 0; i < playerScores.Count; i++)
        {
            if (playerScores[i].clientId == player.clientId)
                return; // Player already exists
        }

        // Add new player with 0 score
        playerScores.Add(new PlayerTrapScore
        {
            clientId = player.clientId,
            trapScore = 0
        });

        Debug.Log($"TrapScoreManager: Added player {player.clientId} to scoring system");
    }

    /// <summary>
    /// Called when a player leaves the game
    /// </summary>
    private void OnPlayerLeft(Player player)
    {
        if (!IsServer) return;

        // Remove player from score tracking
        for (int i = 0; i < playerScores.Count; i++)
        {
            if (playerScores[i].clientId == player.clientId)
            {
                playerScores.RemoveAt(i);
                Debug.Log($"TrapScoreManager: Removed player {player.clientId} from scoring system");
                break;
            }
        }
    }

    /// <summary>
    /// Awards points to a player when their trap is triggered
    /// Should only be called on the server
    /// </summary>
    public void AwardTrapScore(ulong ownerClientId)
    {
        if (!IsServer)
        {
            Debug.LogWarning("AwardTrapScore can only be called on the server!");
            return;
        }

        // Find and update the player's score
        for (int i = 0; i < playerScores.Count; i++)
        {
            var playerScore = playerScores[i];
            if (playerScore.clientId == ownerClientId)
            {
                playerScore.trapScore += pointsPerTrapTriggered;
                playerScores[i] = playerScore; // Update the NetworkList

                Debug.Log($"TrapScoreManager: Awarded {pointsPerTrapTriggered} points to player {ownerClientId}. New score: {playerScore.trapScore}");

                // Update the UI score system
                //UpdatePlayerTrapScoreRpc(ownerClientId, playerScore.trapScore);
                break;
            }
        }
    }

    /// <summary>
    /// Get the current trap score for a specific player
    /// </summary>
    public int GetPlayerTrapScore(ulong clientId)
    {
        for (int i = 0; i < playerScores.Count; i++)
        {
            if (playerScores[i].clientId == clientId)
                return playerScores[i].trapScore;
        }
        return 0;
    }


    /// <summary>
    /// Get all player scores (for leaderboards, etc.)
    /// </summary>
    public List<PlayerTrapScore> GetAllPlayerScores()
    {
        var scores = new List<PlayerTrapScore>();
        for (int i = 0; i < playerScores.Count; i++)
        {
            scores.Add(playerScores[i]);
        }
        return scores;
    }

    public void UpdateDebugScores()
    {
        debugPlayerScores.Clear();
        foreach (var score in playerScores)
        {
            debugPlayerScores.Add(score);
        }
    }
}
