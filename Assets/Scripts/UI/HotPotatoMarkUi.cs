using UnityEngine;

public class HotPotatoMarkUi : MonoBehaviour, IPlayerBindedUi
{
    private MarkManager markManager;
    private ulong playerId;

    private void Start()
    {
        markManager = FindAnyObjectByType<MarkManager>();
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
        if (id == playerId)
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

    public void BindPlayer(GameObject playerObject)
    {
        if (playerObject == null)
        {
            Debug.LogWarning("Player object is null in BindPlayer.");
            return;
        }

        if (playerObject.TryGetComponent<Player>(out var player))
        {
            playerId = player.Id;
        }
        else
        {
            Debug.LogWarning("Player object does not have a Player component.");
        }
    }
}
