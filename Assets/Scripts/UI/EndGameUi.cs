using UnityEngine;
using UnityEngine.SceneManagement;

public class EndGameUi : MonoBehaviour
{
    [SerializeField] private HotPotatoGameManager gameManager;

    void Start()
    {
        // Unlock and show the cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ExitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif

        Debug.Log("Game is exiting...");
    }

    // Method to go to Lobby
    public async void LoadLobby()
    {
        try
        {
            string currentSceneName = SceneManager.GetActiveScene().name;

            gameManager.DespawnPlayerRpc();
            await SceneLifetimeManager.Instance.UnloadSceneNetworked(currentSceneName);
            await SceneLifetimeManager.Instance.ReturnToLobby();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error returning to lobby: {e.Message}");
            // Fallback: Load main menu directly
            SceneManager.LoadScene("MainMenu");
        }
    }
}
