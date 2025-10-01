using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SpawnManager : NetworkBehaviour
{
    public static SpawnManager Instance;
    
    [SerializeField] private NetworkObject spawnPrefab;
    [SerializeField] private List<Transform> spawnPositions;
    
    void Awake()
    {
        NetworkManager.SceneManager.OnLoadEventCompleted += OnSceneLoaded;
        // NetworkManager.SceneManager.OnUnloadEventCompleted += OnSceneUnload;
        
        if (!Instance)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void OnSceneLoaded(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        NetworkManager.SceneManager.OnLoadEventCompleted -= OnSceneLoaded;
        SpawnPlayersServerRpc();
    }

    private void OnSceneUnload(string scenename, LoadSceneMode loadscenemode, List<ulong> clientscompleted, List<ulong> clientstimedout)
    {
        NetworkManager.SceneManager.OnUnloadEventCompleted -= OnSceneUnload;
        DespawnPlayerServerRpc(PlayerManager.Instance.localPlayer.Id);
    }
    
    [Rpc(SendTo.Server)]
    private void SpawnPlayersServerRpc(RpcParams ctx = default)
    {
        // Don't spawn if exist already
        if (PlayerManager.Instance.FindPlayerByClientId(ctx.Receive.SenderClientId)) return;
        
        Debug.Log($"Spawning Player with id: {ctx.Receive.SenderClientId}");
        Player player = Instantiate(
            spawnPrefab, 
            spawnPositions[(int) ctx.Receive.SenderClientId].position, 
            Quaternion.identity).GetComponent<Player>();
        
        player.GetComponent<NetworkObject>().SpawnWithOwnership(ctx.Receive.SenderClientId);
    }
    
    [Rpc(SendTo.Server)]
    public void DespawnPlayerServerRpc(ulong id, RpcParams ctx = default)
    {
        // Destroy current player
        Debug.Log($"Despawning Player with id: {ctx.Receive.SenderClientId}");
        Player player = PlayerManager.Instance.FindPlayerByNetId(id);

        player.GetComponent<NetworkObject>().Despawn(); // Despawn Calls Camera Switch
    }
}
