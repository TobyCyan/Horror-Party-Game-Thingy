using Unity.Netcode;
using UnityEngine;

public class HotPotatoMarkUi : MonoBehaviour
{
    [SerializeField] private MarkManager markManager;

    private void Awake()
    {
        if (markManager == null)
        {
            Debug.LogWarning("MarkManager not found in HotPotatoUi.");
            return;
        }
        
        markManager.OnMarkPassed += CheckPlayerId;
        markManager.OnMarkedPlayerEliminated += Hide;
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
        if (MarkManager.CurrentMarkedPlayerClientId.Value != NetworkManager.Singleton.LocalClientId 
            || PlayerManager.Instance.localPlayer == null)
        {
            Debug.LogWarning($"CurrentMarkedPlayer ID: {MarkManager.CurrentMarkedPlayerClientId.Value}, " +
                $"localPlayer: {NetworkManager.Singleton.LocalClientId} in HotPotatoMarkUi.");
            Hide();
            return;
        }

        if (MarkManager.CurrentMarkedPlayerClientId.Value == NetworkManager.Singleton.LocalClientId)
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
