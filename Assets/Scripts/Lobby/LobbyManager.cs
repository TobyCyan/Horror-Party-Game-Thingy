using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance;
    void Awake()
    {
        if (!Instance)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        NetworkManager.OnClientConnectedCallback -= OnClientConnected;
    }

    public void OnClientConnected(ulong cId)
    {
        SpawnManager.Instance.SpawnPlayerByCId(cId);
        
        // Just in case
        UnloadMainMenuClientRpc(cId);
    }

    [Rpc(SendTo.NotMe)]
    private void UnloadMainMenuClientRpc(ulong cId)
    {
        if (cId != NetworkManager.Singleton.LocalClientId) return;

        if (SceneManager.GetSceneByName("NewMainMenu").isLoaded)
        {
            SceneLifetimeManager.Instance.clientSceneLoader.UnloadSceneAsync("NewMainMenu");
        }
    }
}
