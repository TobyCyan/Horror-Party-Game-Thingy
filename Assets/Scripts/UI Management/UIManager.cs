using UnityEngine;
using System.Linq;
using System.Collections.Generic;

//attach to player
public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    private UIView currentView;
    [SerializeField] 
    public List<UIView> uiViews; // contains everything right now..?

    private void Awake()
    {
        // enforce singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        foreach (var view in uiViews)
        {
            view.Hide();
        }
    }

    public void SwitchUIView<T>() where T : UIView
    {
        UIView nextView = uiViews.FirstOrDefault(v => v is T);

        if (null == nextView)
        {
            Debug.LogWarning($"UI view {typeof(T).Name} not found. Assign in InitScene's UIManager!");
            return;
        }

        if (null != currentView) currentView.Hide();
        currentView = nextView;
        currentView.Show();
    }
}
