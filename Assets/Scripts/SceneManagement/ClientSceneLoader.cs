using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ClientSceneLoader : MonoBehaviour
{
    // We have to do additive Scene loading for multiplayer (Persistent Init Scene)
    // Each client to handle what scenes are already loaded
    // Server only tells to load, does not check if alr loaded on client side
    private HashSet<string> _loadedSceneNames = new();
    private int _asyncLoadingCount = 0;
    private TaskCompletionSource<bool> completionSource;

    public async Task LoadScenesAsync(string[] sceneNames)
    {
        await LoadOrUnloadSceneAsync(sceneNames, true);
    }
    
    public async Task UnloadScenesAsync(string[] sceneNames)
    {
        await LoadOrUnloadSceneAsync(sceneNames, false);
    }

    public AsyncOperation LoadSceneAsync(string sceneName)
    {
        if (_loadedSceneNames.Contains(sceneName)) return null;
        
        _loadedSceneNames.Add(sceneName);
        return SceneManager.LoadSceneAsync(sceneName);
    }

    public AsyncOperation UnloadSceneAsync(string sceneName)
    {
        if (!_loadedSceneNames.Contains(sceneName)) return null;
        
        _loadedSceneNames.Remove(sceneName);
        return SceneManager.UnloadSceneAsync(sceneName);
    }
    private async Task LoadOrUnloadSceneAsync(string[] sceneNames, bool toLoad)
    {
        // Init State for Task Scheduling
        TaskCompletionSource<bool> completionSource = new();
        _asyncLoadingCount = sceneNames.Length;
        
        // Execute on all scenes
        for (int i = 0; i < _asyncLoadingCount; i++)
        {
            AsyncOperation operation = toLoad ? LoadSceneAsync(sceneNames[i]) : UnloadSceneAsync(sceneNames[i]);
            operation.completed += OnSceneOperationComplete;
        }

        // Wait for all scenes to finish
        await completionSource.Task;
    }

    private void OnSceneOperationComplete(AsyncOperation operation)
    {
        // One operation done
        _asyncLoadingCount--;
        operation.completed -= OnSceneOperationComplete; // unsub from load event
    
        // Last task to finish sets the overall status
        if (_asyncLoadingCount == 0)
        {
            completionSource.SetResult(true);
        }
    }

    public bool IsSceneLoaded(string sceneName)
    {
        return _loadedSceneNames.Contains(sceneName);
    }
}
