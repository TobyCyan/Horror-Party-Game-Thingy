using UnityEngine;

public class ShowMainMenu : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        UIManager.Instance.SwitchUIView<MainMenuView>();
    }

}
