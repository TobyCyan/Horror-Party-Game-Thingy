using UnityEngine;

public class RevealedUi : MonoBehaviour
{
    private PlayerSilhouette playerSilhouette;

    private void Awake()
    {
        Hide();
        PlayerManager.OnLocalPlayerSet += SetAndBindPlayerSilhouette;
    }

    private void OnDestroy()
    {
        PlayerManager.OnLocalPlayerSet -= SetAndBindPlayerSilhouette;
        if (playerSilhouette != null)
        {
            playerSilhouette.OnSilhouetteShown -= Reveal;
            playerSilhouette.OnSilhouetteHidden -= Hide;
        }
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

    private void Reveal()
    {
        gameObject.SetActive(true);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}