using System.Threading.Tasks;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using System;

public class MarkManager : NetworkBehaviour
{
    [SerializeField] private GameObject markSymbol;

    public static MarkManager Instance;
    public Player currentMarkedPlayer;

    public event Action<ulong> OnMarkPassed;
    public event Action OnMarkedPlayerEliminated;

    [SerializeField] private Timer postEliminationCoolDownTimer;
    [Min(0.0f)]
    [SerializeField] private float markPassingCooldown = 4.0f;

    void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        OnMarkPassed += UpdateMarkedPlayerAllRpc;
        OnMarkedPlayerEliminated += HandleMarkedPlayerEliminated;
        postEliminationCoolDownTimer.OnTimeUp += AssignNextPlayerWithMark;
    }

    public override void OnNetworkDespawn()
    {
        OnMarkPassed -= UpdateMarkedPlayerAllRpc;
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

    public async void StartHPGame()
    {
        await Task.Delay(500);
        
        AssignRandomPlayerWithMark();
        AddHpComponentClientRpc();
    }

    public void StopHPGame()
    {
        if (currentMarkedPlayer != null)
        {
            currentMarkedPlayer.OnPlayerEliminated -= InvokeOnMarkedPlayerEliminated;
            currentMarkedPlayer = null;
        }
        
        postEliminationCoolDownTimer.StopTimer();
        markSymbol.SetActive(false);
    }

    private void HandleMarkedPlayerEliminated()
    {
        // Remove the marked player from the alive players pool
        PlayerManager.Instance.EliminatePlayer(currentMarkedPlayer);
        currentMarkedPlayer = null;

        // Start cooldown timer before assigning the new marked player
        postEliminationCoolDownTimer.StartTimer(markPassingCooldown);
    }

    [Rpc(SendTo.Everyone)]
    private void AddHpComponentClientRpc()
    {
        if (PlayerManager.Instance == null || PlayerManager.Instance.localPlayer == null)
        {
            Debug.Log("PlayerManager or localPlayer is null, cannot add HPPassingLogic component.");
            return;
        }
        
        Debug.Log($"Adding hp component to {PlayerManager.Instance.localPlayer} with {PlayerManager.Instance.localPlayer.Id}");
        PlayerManager.Instance.localPlayer.AddComponent<HPPassingLogic>();
    }

    private void AssignNextPlayerWithMark()
    {
        // TODO: Should assign next player by least sabotage scores.
        AssignRandomPlayerWithMark();
    }

    public void AssignRandomPlayerWithMark()
    {
        Player randomPlayer = PlayerManager.Instance.GetRandomAlivePlayer();
        currentMarkedPlayer = randomPlayer;

        if (currentMarkedPlayer == null)
        {
            Debug.LogWarning("No alive players to assign the mark to.");
            return;
        }

        PassMarkToPlayerServerRpc(currentMarkedPlayer.Id);
    }

    public void PassMarkToPlayer(ulong id)
    {
        PassMarkToPlayerServerRpc(id);
    }

    [Rpc(SendTo.Server)]
    private void PassMarkToPlayerServerRpc(ulong id, RpcParams rpcParams = default)
    {
        if (currentMarkedPlayer != null)
        {
            // Unsubscribe from previous marked player's elimination event
            currentMarkedPlayer.OnPlayerEliminated -= InvokeOnMarkedPlayerEliminated;
        }

        Player player = PlayerManager.Instance.FindPlayerByNetId(id);
        // Debug.Log($"Passing mark to {player} with id {id}");
        markSymbol.transform.SetParent(player.transform);
        markSymbol.transform.position = player.transform.position + 2*Vector3.up;
        markSymbol.GetComponent<NetworkObject>().ChangeOwnership(player.clientId); // Disable if causing issues

        OnMarkPassed?.Invoke(id);
    }

    [Rpc(SendTo.Everyone)]
    private void UpdateMarkedPlayerAllRpc(ulong id)
    {
        Player player = PlayerManager.Instance.FindPlayerByNetId(id);
        currentMarkedPlayer = player;
        currentMarkedPlayer.OnPlayerEliminated += InvokeOnMarkedPlayerEliminated;
        Debug.Log($"Updated marked player to {currentMarkedPlayer} with id {id}");
    }

    private void InvokeOnMarkedPlayerEliminated()
    {
        OnMarkedPlayerEliminated?.Invoke();
    }
}
