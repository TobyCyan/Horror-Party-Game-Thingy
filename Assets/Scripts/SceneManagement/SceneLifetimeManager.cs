                                                                                             using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLifetimeManager : MonoBehaviour
{
    public static SceneLifetimeManager Instance;
    public ClientSceneLoader clientSceneLoader;
    private TaskCompletionSource<bool> _isCurrentlyPendingSceneEvent;
    private Queue<ProcessingScene> _sceneQueue;

    
    private HashSet<string> networkedSceneNames = new HashSet<string>();

    private bool isCurrentlyProcessingScenes => _sceneQueue != null;
    private bool isServerUp;

    private struct ProcessingScene
    {
        public string SceneName;
        public bool ToLoad;
    }
    private void Awake()
    {
        if (Instance)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        clientSceneLoader = GetComponent<ClientSceneLoader>();
        
        DontDestroyOnLoad(gameObject);
    }
    private void Start()
    {
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        NetworkManager.Singleton.OnServerStopped += OnServerStopped;
        NetworkManager.Singleton.OnClientStarted += OnClientStarted;
        NetworkManager.Singleton.OnClientStopped += OnClientStopped;
    }
    private void OnServerStarted()
    {
        Debug.Log("Server started");
        isServerUp = true;
        NetworkManager.Singleton.SceneManager.ActiveSceneSynchronizationEnabled = true;
        NetworkManager.Singleton.SceneManager.SetClientSynchronizationMode(LoadSceneMode.Additive);
        NetworkManager.Singleton.SceneManager.OnSceneEvent += OnServerSceneEvent;
        NetworkManager.Singleton.SceneManager.VerifySceneBeforeLoading += VerifySceneBeforeLoading;
        NetworkManager.Singleton.SceneManager.VerifySceneBeforeUnloading += VerifySceneBeforeUnloading;
    }

    private void OnServerStopped(bool obj)
    {
        isServerUp = false;
        if (NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnSceneEvent -= OnServerSceneEvent;
            NetworkManager.Singleton.SceneManager.VerifySceneBeforeLoading -= VerifySceneBeforeLoading;
            NetworkManager.Singleton.SceneManager.VerifySceneBeforeUnloading -= VerifySceneBeforeUnloading;
        }
        StopAllCoroutines();
        StopProcessing();
        //LocallyUnloadSynchedScenes();
    }

    private void OnClientStarted()
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            NetworkManager.Singleton.SceneManager.OnSceneEvent += OnClientSceneEvent;
        }
        NetworkManager.Singleton.SceneManager.VerifySceneBeforeLoading += VerifySceneBeforeLoading;
        NetworkManager.Singleton.SceneManager.VerifySceneBeforeUnloading += VerifySceneBeforeUnloading;
    }
    private void OnClientStopped(Boolean isHost)
    {
        if (isHost) return;

        if (NetworkManager.Singleton.SceneManager == null) return;
        
        NetworkManager.Singleton.SceneManager.OnSceneEvent -= OnClientSceneEvent;
        NetworkManager.Singleton.SceneManager.VerifySceneBeforeLoading -= VerifySceneBeforeLoading;
        NetworkManager.Singleton.SceneManager.VerifySceneBeforeUnloading -= VerifySceneBeforeUnloading;
    }
    
    // Never double-load our 'global' scene
    private Boolean VerifySceneBeforeUnloading(Scene scene) => scene.buildIndex != 0;

    // Never double-load our 'global' scene
    private Boolean VerifySceneBeforeLoading(Int32 sceneIndex, String sceneName, LoadSceneMode loadMode) =>
        sceneIndex != 0; 

    private void StopProcessing()
    {
        _sceneQueue = null;

        // Stop all scene loading
        if (_isCurrentlyPendingSceneEvent != null)
        {
            _isCurrentlyPendingSceneEvent.SetCanceled();
            _isCurrentlyPendingSceneEvent = null;
        }
    }


    public async Task LocallyUnloadSynchedScenes()
    {
        if (networkedSceneNames != null)
        {
            foreach (string sceneName in networkedSceneNames)
            {
                Scene scene = SceneManager.GetSceneByName(sceneName);
                if (scene.IsValid())
                {
                    Debug.Log($"Unloading scene {sceneName}");
                    await SceneManager.UnloadSceneAsync(sceneName);
                    //use sceneManager to manually unload as they werent loaded with networkscenemanager
                    //await clientSceneLoader.UnloadSceneAsync(sceneName);
                }
            }
            networkedSceneNames.Clear();
        }
        
    }

    private void OnClientSceneEvent(SceneEvent sceneEvent)
    {
        if (clientSceneLoader.IsSceneLoaded(sceneEvent.SceneName))
        {
            return;
        }

        switch (sceneEvent.SceneEventType)
        {
            case SceneEventType.LoadComplete:
                Debug.Log($"Adding {sceneEvent.SceneName} to networkedSceneNames after clientSceneEventLoadComplete");
                networkedSceneNames.Add(sceneEvent.SceneName);
                if (sceneEvent.SceneName == "PersistentSessionScene")
                {
                    SetActiveScene("PersistentSessionScene");
                }
                else if (sceneEvent.SceneName == "MazeScene")
                {
                    SetActiveScene("MazeScene");
                }
                break;
            case SceneEventType.UnloadComplete:
                networkedSceneNames.Remove(sceneEvent.SceneName);
                if (sceneEvent.SceneName == "PersistentSessionScene")
                {
                    SetActiveScene("InitScene");
                }
                else if (sceneEvent.SceneName == "MazeScene")
                {
                    SetActiveScene("PersistentSessionScene");
                }
                break;
            case SceneEventType.LoadEventCompleted:

                break;
            case SceneEventType.UnloadEventCompleted:

                break;
        }
    }

    private void OnServerSceneEvent(SceneEvent sceneEvent)
    {
        switch (sceneEvent.SceneEventType)
        {
            case SceneEventType.Load:
            case SceneEventType.Unload:
                sceneEvent.AsyncOperation.completed += OnSceneEventCompletion;
                break;
            case SceneEventType.LoadComplete:
                if (sceneEvent.SceneName == "PersistentSessionScene")
                {
                    SetActiveScene("PersistentSessionScene");
                }
                else if (sceneEvent.SceneName == "MazeScene")
                {
                    SetActiveScene("MazeScene");
                }
                break;
            case SceneEventType.UnloadComplete:
                if (sceneEvent.SceneName == "PersistentSessionScene")
                {
                    SetActiveScene("InitScene");
                }
                else if (sceneEvent.SceneName == "MazeScene")
                {
                    SetActiveScene("PersistentSessionScene");
                }
                break;
        }
    }

    private void OnSceneEventCompletion(AsyncOperation op)
    {
        op.completed -= OnSceneEventCompletion;

        ProcessNextSceneInQueue();
    }

    public async Task LoadSceneNetworked(string sceneName)
    {
        await LoadSceneNetworked(new string[] { sceneName });
    }

    public async Task LoadSceneNetworked(string[] sceneNames)
    {
        await LoadUnloadScenesAsync(sceneNames, true);
    }

    public async Task UnloadSceneNetworked(string sceneName)
    {
        await UnloadSceneNetworked(new string[] { sceneName });
    }

    public async Task UnloadSceneNetworked(string[] sceneNames)
    {
        await LoadUnloadScenesAsync(sceneNames, false);
    }

    public async Task LoadUnloadScenesAsync(string[] sceneNames, bool toLoad)
    {
        if (!isServerUp || sceneNames == null || sceneNames.Length == 0) return;

        if (isCurrentlyProcessingScenes)
        {
            Debug.LogError("Tried to perform load or unload operation while already processing scenes!");
            return;
        }

        _isCurrentlyPendingSceneEvent = new();
        _sceneQueue = new Queue<ProcessingScene>();
        EnqueueScenes(sceneNames, toLoad);

        ProcessNextSceneInQueue();

        await _isCurrentlyPendingSceneEvent.Task;

        _isCurrentlyPendingSceneEvent = null;


    }

    private void EnqueueScenes(string[] sceneNames, bool toLoad)
    {
        foreach (string sceneName in sceneNames)
        {
            _sceneQueue.Enqueue(new ProcessingScene { SceneName = sceneName, ToLoad = toLoad });
        }
    }

    private void ProcessNextSceneInQueue()
    {
        if (_sceneQueue.Count > 0)
        {
            // process the next scene
            ProcessingScene processingScene = _sceneQueue.Dequeue();
            if (processingScene.ToLoad)
                LoadQueuedSceneAsync(processingScene.SceneName);
            else
                UnloadQueuedSceneAsync(processingScene.SceneName);
        }
        else
        {
            // end processing, SetResult stops awaiting
            _sceneQueue = null;
            _isCurrentlyPendingSceneEvent.SetResult(true);
        }
    }


    private void LoadQueuedSceneAsync(string sceneName)
    {
        if (networkedSceneNames.Contains(sceneName))
        {
            Debug.LogWarning($"skip load, already loaded: {sceneName}");
            ProcessNextSceneInQueue();
            return;
        }

        var status = NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);

        if (status == SceneEventProgressStatus.Started)
        {
            Debug.Log($"Adding {sceneName} to networkedSceneNames after loading");
            networkedSceneNames.Add(sceneName);
        }
        else
        {
            Debug.LogWarning($"Scene load failed: {status} for {sceneName}");
            ProcessNextSceneInQueue();
        }
    }

    private void UnloadQueuedSceneAsync(string sceneName)
    {
        if (networkedSceneNames.Contains(sceneName) == false)
        {
            //Debug.LogWarning($"skip unload, not loaded: {sceneRef.SceneName}");
            ProcessNextSceneInQueue();
            return;
        }

        Scene sceneRef = SceneManager.GetSceneByName(sceneName);
        var status = NetworkManager.Singleton.SceneManager.UnloadScene(sceneRef);

        if (status == SceneEventProgressStatus.Started)
            networkedSceneNames.Remove(sceneName);
        else
        {
            Debug.LogWarning($"Scene unload failed: {status} for {sceneName}");
            ProcessNextSceneInQueue();
        }
    }

    public void SetActiveScene(string sceneName)
    {
        //if (!networkedSceneNames.Contains(sceneName)) return; //cant set unnetworked scene to active

        Scene newActive = SceneManager.GetSceneByName(sceneName);

        if (!newActive.IsValid())
        {
            Debug.LogError("Tried to set an unloaded scene as active!");
            return;
        }

        SceneManager.SetActiveScene(newActive);
    }

}
