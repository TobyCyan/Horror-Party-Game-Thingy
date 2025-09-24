using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using System;
using Random = UnityEngine.Random;

public class MarkManager : NetworkBehaviour
{
    [SerializeField] private GameObject markSymbol;
    
    public static MarkManager Instance;
    public Player currentMarkedPlayer;

    public event Action<ulong> OnMarkPassed;
    public event Action OnMarkedPlayerEliminated;

    void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        OnMarkPassed += UpdateMarkedPlayerAllRpc;
        // TODO: Should assign next player by least sabotage scores.
        OnMarkedPlayerEliminated += AssignRandomPlayerWithMark;
    }


    public override void OnDestroy()
    {
        base.OnDestroy();
        OnMarkPassed -= UpdateMarkedPlayerAllRpc;
        OnMarkedPlayerEliminated -= AssignRandomPlayerWithMark;
    }

    public async void StartHPGame()
    {
        await Task.Delay(500);
        
        AssignRandomPlayerWithMark();
        AddHpComponentClientRpc();
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

    public void AssignRandomPlayerWithMark()
    {
        List<Player> players = PlayerManager.Instance.players;
        int randomIndex = Random.Range(0, players.Count);
        currentMarkedPlayer = players[randomIndex];
        
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
            currentMarkedPlayer.OnPlayerEliminated -= OnMarkedPlayerEliminated;
        }

        Player player = PlayerManager.Instance.FindPlayerByNetId(id);
        Debug.Log($"Passing mark to {player} with id {id}");
        markSymbol.transform.SetParent(player.transform);
        markSymbol.transform.position = player.transform.position + 2*Vector3.up;

        OnMarkPassed?.Invoke(id);
    }

    [Rpc(SendTo.Everyone)]
    private void UpdateMarkedPlayerAllRpc(ulong id)
    {
        Player player = PlayerManager.Instance.FindPlayerByNetId(id);
        currentMarkedPlayer = player;
        currentMarkedPlayer.OnPlayerEliminated += OnMarkedPlayerEliminated;
    }
}
