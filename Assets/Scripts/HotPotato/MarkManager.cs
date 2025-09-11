using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class MarkManager : NetworkBehaviour
{
    [SerializeField] private GameObject markSymbol;
    
    public static MarkManager Instance;
    public Player currentMarkedPlayer;

    void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        
        
        StartHPGame();
    }

    private async void StartHPGame()
    {
        await Task.Delay(500);
        
        AssignRandomPlayerWithMark();
        AddHpComponentClientRpc();
    }
    
    [Rpc(SendTo.Everyone)]
    private void AddHpComponentClientRpc()
    {
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
        Player player = PlayerManager.Instance.FindPlayerByNetId(id);
        UpdateMarkedPlayerAllRpc(id);
        Debug.Log($"Passing mark to {player} with id {id}");
        markSymbol.transform.SetParent(player.transform);
        markSymbol.transform.position = player.transform.position + 2*Vector3.up;
    }

    [Rpc(SendTo.Everyone)]
    private void UpdateMarkedPlayerAllRpc(ulong id)
    {
        Player player = PlayerManager.Instance.FindPlayerByNetId(id);
        currentMarkedPlayer = player;
    }
}
