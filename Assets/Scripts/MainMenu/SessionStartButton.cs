using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;

public class SessionStartButton : NetworkBehaviour
{
    private void Awake()
    {
        //NetworkManager.Singleton.OnServerStarted += StartGame;
    }

    public async void StartGame()
    {
        // Let client start game? NAHHH
        if (!IsServer) return;
        
        UnloadMainMenuNotServerRPC();
        
        // Send to everyone but Host
        await SceneLifetimeManager.Instance.clientSceneLoader.UnloadSceneAsync("MainMenu");
        await SceneLifetimeManager.Instance.LoadSceneNetworked(new string[] { SceneLifetimeManager.LobbyScene });
    }
    public void SaveSession(ISession activeSession)
    {
        // Send to everyone but Host
        MySessionManager.Instance.activeSession = activeSession;
        SceneLifetimeManager.Instance.activeSession = activeSession;
        Debug.Log(activeSession.Code);
    }

    [Rpc(SendTo.NotServer)]
    public void UnloadMainMenuNotServerRPC()
    {
        SceneLifetimeManager.Instance.clientSceneLoader.UnloadSceneAsync("MainMenu");
    }
    
    public void UnloadMainMenu()
    {
        SceneLifetimeManager.Instance.clientSceneLoader.UnloadSceneAsync("MainMenu");
    }
}