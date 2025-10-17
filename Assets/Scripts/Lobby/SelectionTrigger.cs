using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class SelectorTrigger : NetworkBehaviour
{
    public enum GameType
    {
        Demo,
        Selected,
    }

    private int triggerCount = 0;
    [SerializeField] private GameType gameType = GameType.Demo;
    [SerializeField] private string demoSceneName = "HospitalScene";
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

        Debug.Log($"Changing scene cuz {playerCount}, {PlayerManager.Instance.players.Count}");
            
        await Task.Delay(500); // Wait just incase;
        
        string playSceneName = GetPlaySceneName();
        await SceneLifetimeManager.Instance.LeaveLobby();
        await SceneLifetimeManager.Instance.LoadSceneNetworked(new string[] { playSceneName });
        SceneLifetimeManager.Instance.SetActiveScene(playSceneName);
    }

    private string GetPlaySceneName()
    {
        string sceneName = gameType == GameType.Demo ? demoSceneName : selectedSceneName;
        return sceneName;
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