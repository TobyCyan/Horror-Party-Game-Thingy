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
    [SerializeField] private string[] sceneList = { "HospitalMapScene", "TestMazeScene" };
    [SerializeField] private int selectedIndex = 0;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioSamples selectionAudioSamples;

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
        PlaySfxRpc();

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
            SpawnManager.Instance.DespawnPlayerServerRpc((ulong) i);
            
        }
        
        Debug.Log($"Changing scene cuz {playerCount}, {PlayerManager.Instance.players.Count}");
            
        await Task.Delay(500); // Wait just incase;
        
        await SceneLifetimeManager.Instance.LeaveLobby();
        await SceneLifetimeManager.Instance.LoadSceneNetworked(new string[] { sceneList[selectedIndex] });
        SceneLifetimeManager.Instance.SetActiveScene(sceneList[selectedIndex]);
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

    [Rpc(SendTo.Everyone)]
    private void PlaySfxRpc()
    {
        if (audioSource != null && selectionAudioSamples != null && selectionAudioSamples.Count > 0)
        {
            AudioClip clip = selectionAudioSamples.PickRandom();
            audioSource.PlayOneShot(clip);
        }
    }
}
