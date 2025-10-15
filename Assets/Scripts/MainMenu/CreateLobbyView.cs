using UnityEngine;

public class CreateLobbyView : UIView
{
    public void ShowMainMenu()
    {
        UIManager.Instance.SwitchUIView<MainMenuView>();
    }
}
