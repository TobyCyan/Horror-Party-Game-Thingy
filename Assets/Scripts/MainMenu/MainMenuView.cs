using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuView : UIView
{
    public void ShowCreate()
    {
        UIManager.Instance.SwitchUIView<CreateLobbyView>();
    }

    public void ShowJoin()
    {
        UIManager.Instance.SwitchUIView<JoinLobbyView>();
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
                EditorApplication.isPlaying = false;
        #else
                Application.Quit();
        #endif
    }
}
