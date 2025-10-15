using UnityEngine;

public static class MenuUtils
{
    public static void ShowCreate()
    {
        UIManager.Instance.SwitchUIView<CreateLobbyView>();
    }

    public static void ShowJoin()
    {
        UIManager.Instance.SwitchUIView<JoinLobbyView>();
    }
}
