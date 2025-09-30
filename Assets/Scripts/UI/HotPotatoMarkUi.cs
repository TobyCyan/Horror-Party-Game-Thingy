using Unity.Netcode;
using UnityEngine;

public class HotPotatoMarkUi : MonoBehaviour, IPlayerBindedUi
{
    private MarkManager markManager;
    private ulong playerId => PlayerManager.Instance.localPlayer.Id;

    private void Start()
    {
        markManager = FindAnyObjectByType<MarkManager>();
        if (markManager == null)
        {
            Debug.LogWarning("MarkManager not found in HotPotatoUi.");
        }
        else
        {
            markManager.OnMarkPassed += CheckPlayerIdClientRpc;
            markManager.OnMarkedPlayerEliminated += Hide;
        }
    }

    private void OnDestroy()
    {
        if (markManager != null)
        {
            markManager.OnMarkPassed -= CheckPlayerIdClientRpc;
            markManager.OnMarkedPlayerEliminated -= Hide;
        }
    }

    [Rpc(SendTo.Everyone)]
    private void CheckPlayerIdClientRpc(ulong id)
    {
        Debug.Log("Should i display mark?");
        if (MarkManager.Instance.currentMarkedPlayer.Id == playerId)
        {
            Debug.Log("Should i display mark? YES");

            Reveal();
        }
        else
        {
            Debug.Log("Should i display mark? NO");

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
            //playerId = player.Id;
        }
        else
        {
            Debug.LogWarning("Player object does not have a Player component.");
        }
    }
}
