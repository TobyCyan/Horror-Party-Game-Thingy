using Unity.Netcode;
using UnityEngine.SceneManagement;

public class LobbyJoinLogic : NetworkBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnClientConnected(ulong obj)
    {
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;

        InitLobbySceneClientRpc();
    }

    [Rpc(SendTo.NotMe)]
    private void InitLobbySceneClientRpc()
    {
        SpawnManager.Instance.SpawnPlayersServerRpc();

        if (SceneManager.GetSceneByName("MainMenu").isLoaded)
        {
            SceneLifetimeManager.Instance.clientSceneLoader.UnloadSceneAsync("MainMenu");
        }
    }
}
