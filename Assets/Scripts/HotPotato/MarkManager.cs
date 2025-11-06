using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class MarkManager : NetworkBehaviour
{
    public static MarkManager Instance;
    public static Player currentMarkedPlayer;

    public event Action<ulong> OnMarkPassed;
    public event Action OnMarkedPlayerEliminated;
    public event Action OnGameStarted;

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
        OnMarkedPlayerEliminated += HandleMarkedPlayerEliminated;
        postEliminationCoolDownTimer.OnTimeUp += AssignNextPlayerWithMark;
    }

    public override void OnNetworkDespawn()
    {
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

        postEliminationCoolDownTimer.StopTimer();
    }

    private void HandleMarkedPlayerEliminated()
    {
        currentMarkedPlayer = null;
        Debug.Log("Marked player eliminated. Preparing to assign new marked player after cooldown.");
        // Start cooldown timer before assigning the new marked player
        postEliminationCoolDownTimer.StartTimer(postEliminateMarkPassingCooldown);
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

        PassMarkToPlayerServerRpc(randomPlayer.Id);
        Debug.Log("Assigned mark to random player " + currentMarkedPlayer + " with id " + currentMarkedPlayer.Id);
    }

    public void PassMarkToPlayer(ulong id)
    {
        PassMarkToPlayerServerRpc(id);
    }

    [Rpc(SendTo.Server)]
    private void PassMarkToPlayerServerRpc(ulong id, RpcParams rpcParams = default)
    {
        if (Time.time - lastMarkPassTime < playerToPlayerMarkPassingCooldown)
        {
            Debug.LogWarning("Mark passing is on cooldown.");
            return;
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

        Player player = PlayerManager.Instance.FindPlayerByNetId(id);

        if (player == null)
        {
            Debug.LogWarning($"Player with id {id} not found.");
            return;
        }

        Debug.Log($"Passing mark to player {player} with id {id}");

        if (player.TryGetComponent(out PlayerMovement pm))
        {
            pm.SetMovementSpeedByModifier(markedPlayerSpeedModifier);
            Debug.Log($"Modified movement speed for new marked player {player}");
        }

        currentMarkedPlayer = player;
        currentMarkedPlayer.OnPlayerEliminated += InvokeOnMarkedPlayerEliminated;
        currentMarkedPlayer.SetMeshRootLayerRpc(auraLayer);
        lastMarkPassTime = Time.time;
        UpdateMarkedPlayerAllRpc(id);
    }

    [Rpc(SendTo.Everyone)]
    private void UpdateMarkedPlayerAllRpc(ulong id)
    {
        Player player = PlayerManager.Instance.FindPlayerByNetId(id);
        if (player == null)
        {
            Debug.LogWarning($"[UpdateMarkedPlayerAllRpc] Could not find player with id {id}");
            return;
        }

        currentMarkedPlayer = player;
        Debug.Log($"Updated marked player to {currentMarkedPlayer} with id {id}");
        OnMarkPassed?.Invoke(id);
    }

    private void InvokeOnMarkedPlayerEliminated()
    {
        OnMarkedPlayerEliminated?.Invoke();
    }

    private void Update()
    {
        if (currentMarkedPlayer)
        {
            currentMarkedPlayer.float0 += Time.deltaTime;
        }
    }
}
