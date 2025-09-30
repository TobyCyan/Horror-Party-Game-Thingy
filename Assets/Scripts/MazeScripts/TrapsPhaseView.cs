using UnityEngine;
using UnityEngine.UI;

public class TrapsPhaseView : UIView
{
    [SerializeField] private Transform content;
    [SerializeField] private GameObject trapButtonPrefab;

    public override void Show()
    {
        base.Show();
        Debug.Log("Showing traps UI");


        var trapPrefabs = MazeTrapManager.Instance.trapPrefabs; // get array from manager
        if (trapPrefabs == null || trapPrefabs.Length == 0)
        {
            Debug.LogWarning("Traps UI could not get trap list");
        }

        for (int i = 0; i < trapPrefabs.Length; i++)
        {
            int index = i; 
            GameObject btnObj = Instantiate(trapButtonPrefab, content);
            Button btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                MazeTrapManager.Instance.SelectTrap(index);
                Debug.Log($"Selected trap {trapPrefabs[index].name}");
            });
        }
    }

    public override void Hide()
    {

        foreach (Transform child in content)
        {
            Button btn = child.GetComponent<Button>();
            btn.onClick.RemoveAllListeners();
            Destroy(child.gameObject);
        }

        base.Hide();
    }
}
