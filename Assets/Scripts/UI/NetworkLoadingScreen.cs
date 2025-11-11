using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Canvas))]
public class NetworkLoadingScreen : NetworkBehaviour
{
    [SerializeField] private float fallbackTime = 3f; // relative to servber
    [SerializeField] private List<string> ignoredScenes = new List<string>(); // scenes where load screens arent active

    private static NetworkLoadingScreen instance;
    private Canvas canvas;

    private int readyCount = 0;
    private HashSet<ulong> readyClients = new HashSet<ulong>();
    private Coroutine fallbackTimer;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        canvas = GetComponent<Canvas>();
        canvas.enabled = false;
    }

    // idk, following scenelifetimemanager
    private void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted += OnNetworkStarted;
            NetworkManager.Singleton.OnClientStarted += OnNetworkStarted;
            NetworkManager.Singleton.OnServerStopped += OnNetworkStopped;
            NetworkManager.Singleton.OnClientStopped += OnNetworkStopped;
        }
    }

    public override void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= OnNetworkStarted;
            NetworkManager.Singleton.OnClientStarted -= OnNetworkStarted;
            NetworkManager.Singleton.OnServerStopped -= OnNetworkStopped;
            NetworkManager.Singleton.OnClientStopped -= OnNetworkStopped;

            if (NetworkManager.Singleton.SceneManager != null)
                NetworkManager.Singleton.SceneManager.OnSceneEvent -= OnSceneEvent;
        }
    }

    private void OnNetworkStarted() => SubscribeSceneEvents();
    private void OnNetworkStopped(bool _ = false) => UnsubscribeSceneEvents();

    private void SubscribeSceneEvents()
    {
        if (NetworkManager.Singleton?.SceneManager != null)
            NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent;
    }

    private void UnsubscribeSceneEvents()
    {
        if (NetworkManager.Singleton?.SceneManager != null)
            NetworkManager.Singleton.SceneManager.OnSceneEvent -= OnSceneEvent;
    }


    private void OnSceneEvent(Unity.Netcode.SceneEvent sceneEvent)
    {
        // yeah frick me
        if (ignoredScenes.Contains(sceneEvent.SceneName)) return;

        if (sceneEvent.SceneEventType == Unity.Netcode.SceneEventType.Load)
        {
            Show();

            // start local fallback timer for this client
            if (fallbackTimer != null)
                StopCoroutine(fallbackTimer);
            fallbackTimer = StartCoroutine(LocalHideFallback());
        }

        if (IsServer && sceneEvent.SceneEventType == Unity.Netcode.SceneEventType.Load)
        {
            readyCount = 0;
            readyClients.Clear();

            if (fallbackTimer != null)
                StopCoroutine(fallbackTimer);

            // server fallback still exists to sync everyone if needed
            fallbackTimer = StartCoroutine(ServerHideFallback());
        }
    }

    // exceed x seconds, just tell everyone to hide the load screen (server version)
    private IEnumerator ServerHideFallback()
    {
        yield return new WaitForSeconds(fallbackTime);
        HideClientRpc();
        readyClients.Clear();
        readyCount = 0;
        fallbackTimer = null;
    }

    // local fallback for each client individually
    private IEnumerator LocalHideFallback()
    {
        yield return new WaitForSeconds(fallbackTime + 1);
        canvas.enabled = false;
        fallbackTimer = null;
    }
    [ServerRpc(RequireOwnership = false)]
    private void NotifyReadyServerRpc(ServerRpcParams rpcParams = default)
    {
        Debug.Log("All clients loaded hiding load screen");
        ulong clientId = rpcParams.Receive.SenderClientId;
        if (readyClients.Contains(clientId)) return;

        readyClients.Add(clientId);
        readyCount++;

        if (readyCount >= NetworkManager.Singleton.ConnectedClients.Count)
        {
            HideClientRpc();
            readyClients.Clear();
            readyCount = 0;

            if (fallbackTimer != null)
            {
                StopCoroutine(fallbackTimer);
                fallbackTimer = null;
            }
        }
    }

    [ClientRpc]
    private void HideClientRpc()
    {
        // when server decides all are ready, hide for everyone
        if (fallbackTimer != null)
        {
            StopCoroutine(fallbackTimer);
            fallbackTimer = null;
        }
        canvas.enabled = false;
    }

    private void Show() => canvas.enabled = true;

    // optional can call on some thing on clients, if all ready before x seconds the load will drop
    public static void SignalClientLoaded()
    {
        if (instance != null && (instance.IsClient || instance.IsHost))
        {
            instance.NotifyReadyServerRpc();
        }
    }
}
