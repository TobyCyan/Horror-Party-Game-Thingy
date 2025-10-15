using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class SessionStartButton : NetworkBehaviour
{
    [Tooltip("For testing only please")]
    public string SceneToLoad;
    
    public async void StartGame()
    {
        // Let client start game? NAHHH
        if (!IsServer) return;
        
        UnloadMainMenuNotServerRPC();
        await Task.Delay(500); // Wait just incase;
        
        // Send to everyone but Host
        await SceneLifetimeManager.Instance.clientSceneLoader.UnloadSceneAsync("NewMainMenu");
        await SceneLifetimeManager.Instance.LoadSceneNetworked(new string[] { SceneToLoad });
        //SceneLifetimeManager.Instance.SetActiveScene("PersistentRunScene");
        //NetworkManager.SceneManager.LoadScene("PreGameScene",UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    [Rpc(SendTo.NotServer)]
    public void UnloadMainMenuNotServerRPC()
    {
        SceneLifetimeManager.Instance.clientSceneLoader.UnloadSceneAsync("NewMainMenu");
    }
}