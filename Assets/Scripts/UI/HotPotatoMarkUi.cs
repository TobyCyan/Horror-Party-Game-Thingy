using UnityEngine;

public class HotPotatoMarkUi : MonoBehaviour
{
    private MarkManager markManager;
    private ulong playerId;

    private void Start()
    {
        markManager = MarkManager.Instance;
        if (markManager == null)
        {
            Debug.LogWarning("MarkManager not found in HotPotatoUi.");
        }
        else
        {
            markManager.OnMarkPassed += CheckPlayerId;
            markManager.OnMarkedPlayerEliminated += Hide;
        }
    }

    private void OnDestroy()
    {
        if (markManager != null)
        {
            markManager.OnMarkPassed -= CheckPlayerId;
            markManager.OnMarkedPlayerEliminated -= Hide;
        }
    }

    private void CheckPlayerId(ulong id)
    {
        if (MarkManager.Instance.currentMarkedPlayer.Id == PlayerManager.Instance.localPlayer.Id)
        {
            Reveal();
        }
        else
        {
            Hide();
        }
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }

    private void Reveal()
    {
        gameObject.SetActive(true);
    }
}
