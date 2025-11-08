using UnityEngine;

public class RevealedUi : MonoBehaviour
{
    private PlayerSilhouette playerSilhouette;

    private void Awake()
    {
        Hide();
        PlayerManager.OnLocalPlayerSet += SetAndBindPlayerSilhouette;
        PlayerManager.OnPlayerRemoved += UnbindPlayerSilhouette;
    }

    private void OnDestroy()
    {
        PlayerManager.OnLocalPlayerSet -= SetAndBindPlayerSilhouette;
        PlayerManager.OnPlayerRemoved -= UnbindPlayerSilhouette;
    }

    private void SetAndBindPlayerSilhouette(Player player)
    {
        playerSilhouette = player.GetComponentInChildren<PlayerSilhouette>();
        if (playerSilhouette == null)
        {
            Debug.LogWarning("PlayerSilhouette is null in RevealUi.");
            return;
        }

        playerSilhouette.OnSilhouetteShown += Reveal;
        playerSilhouette.OnSilhouetteHidden += Hide;
    }

    private void UnbindPlayerSilhouette(Player _)
    {
        if (playerSilhouette != null)
        {
            playerSilhouette.OnSilhouetteShown -= Reveal;
            playerSilhouette.OnSilhouetteHidden -= Hide;
            playerSilhouette = null;
        }
    }

    private void Reveal()
    {
        gameObject.SetActive(true);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}