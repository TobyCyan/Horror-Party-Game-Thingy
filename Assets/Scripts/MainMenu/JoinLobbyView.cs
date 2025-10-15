using UnityEngine;

public class JoinLobbyView : UIView
{
    public void ShowMainMenu()
    {
        UIManager.Instance.SwitchUIView<MainMenuView>();
    }
}
