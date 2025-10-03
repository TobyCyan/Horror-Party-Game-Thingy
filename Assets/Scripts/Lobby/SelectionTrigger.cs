using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class SelectorTrigger : NetworkBehaviour
{
    private int triggerCount = 0;
    [SerializeField] private string selectedSceneName = "HospitalMapScene";

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
        triggerCount++;

        if (triggerCount == PlayerManager.Instance.players.Count)
        {
            ChangeGame();
        }
    }

    private async void ChangeGame()
    {
        int playerCount = PlayerManager.Instance.players.Count;
        // Despawn everyone
        for (int i = 0; i < playerCount; i++)
        {
            SpawnManager.Instance.DespawnPlayerServerRpc(PlayerManager.Instance.FindPlayerByClientId((ulong)i).Id);
        }
        //UnloadSceneNotServerRPC("PersistentSessionScene");
            
        Debug.Log($"Changing scene cuz {playerCount}, {PlayerManager.Instance.players.Count}");
            
        await Task.Delay(500); // Wait just incase;
        
        await SceneLifetimeManager.Instance.UnloadSceneNetworked("PersistentSessionScene");
        await SceneLifetimeManager.Instance.LoadSceneNetworked(new string[] { selectedSceneName });
        SceneLifetimeManager.Instance.SetActiveScene(selectedSceneName);
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
        triggerCount--;
    }
    
}