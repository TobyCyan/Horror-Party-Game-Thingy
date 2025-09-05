using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SpawnManager : NetworkBehaviour
{
    [SerializeField] private NetworkObject spawnPrefab;
    [SerializeField] private List<Transform> spawnPositions;
    
    void Awake()
    {
        NetworkManager.SceneManager.OnLoadEventCompleted += OnSceneLoaded;
    }

    private void OnSceneLoaded(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        NetworkManager.SceneManager.OnLoadEventCompleted -= OnSceneLoaded;
        SpawnPlayerServerRpc();
    }

    [Rpc(SendTo.Server)]
    private void SpawnPlayerServerRpc(RpcParams ctx = default)
    {
        Debug.Log($"Spawning Player with id: {ctx.Receive.SenderClientId}");
        Player player = Instantiate(
            spawnPrefab, 
            spawnPositions[(int) ctx.Receive.SenderClientId].position, 
            Quaternion.identity).GetComponent<Player>();
        
        player.GetComponent<NetworkObject>().SpawnWithOwnership(ctx.Receive.SenderClientId);
        
    }
}
