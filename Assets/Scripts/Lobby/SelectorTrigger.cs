using System;
using Unity.Netcode;
using UnityEngine;

public class SelectorTrigger : NetworkBehaviour
{
    private int playerCount = 0;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) OnGameSelectedServerRpc();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) OnGameDeselectedServerRpc();
    }
    [Rpc(SendTo.Server)]
    private void OnGameSelectedServerRpc()
    {
        // Update on server side
        playerCount++;

        if (playerCount == PlayerManager.Instance.players.Count)
        {
            ChangeToHpScene();
        }
    }

    private async void ChangeToHpScene()
    {
        await SceneLifetimeManager.Instance.UnloadSceneNetworked("PersistentSessionScene");
        await SceneLifetimeManager.Instance.LoadSceneNetworked("MazeScene");
    }
    
    [Rpc(SendTo.Server)]
    private void OnGameDeselectedServerRpc()
    {
        // Update on server side
        playerCount--;
    }
    
}
