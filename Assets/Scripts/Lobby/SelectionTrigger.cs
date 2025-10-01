using System;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class SelectorTrigger : NetworkBehaviour
{
    private int playerCount = 0;
    private void OnTriggerEnter(Collider other)
    {
        if (!IsOwner) return;
        
        if (other.CompareTag("Player")) OnGameSelectedServerRpc();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsOwner) return;
        
        if (other.CompareTag("Player")) OnGameDeselectedServerRpc();
    }
    [Rpc(SendTo.Server)]
    private void OnGameSelectedServerRpc()
    {
        // Update on server side
        playerCount++;

        if (playerCount == PlayerManager.Instance.players.Count)
        {
            ChangeGame();
        }
    }

    private async void ChangeGame()
    {
        UnloadSceneNotServerRPC("PersistentSessionScene");
            
        Debug.Log($"Changing scene cuz {playerCount}, {PlayerManager.Instance.players.Count}");
            
        await Task.Delay(500); // Wait just incase;
        
        await SceneLifetimeManager.Instance.clientSceneLoader.UnloadSceneAsync("PersistentSessionScene");
        await SceneLifetimeManager.Instance.LoadSceneNetworked(new string[] { "HospitalScene" });
    }
    
    [Rpc(SendTo.NotServer)]
    private void UnloadSceneNotServerRPC(string sceneName)
    {
        SceneLifetimeManager.Instance.clientSceneLoader.UnloadSceneAsync(sceneName);
    }
    
    [Rpc(SendTo.Server)]
    private void OnGameDeselectedServerRpc()
    {
        // Update on server side
        playerCount--;
    }
    
}