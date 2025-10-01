using UnityEngine;

public class RevealedUi : MonoBehaviour
{
    private PlayerSilhouette playerSilhouette;

    private void Start()
    {
        Hide();
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnLocalPlayerSet += SetAndBindPlayerSilhouette;
        }
    }

    private void OnDestroy()
    {
        if (playerSilhouette != null)
        {
            playerSilhouette.OnSilhouetteShown -= Reveal;
            playerSilhouette.OnSilhouetteHidden -= Hide;
        }
    }

    private void SetAndBindPlayerSilhouette()
    {
        playerSilhouette = PlayerManager.Instance.localPlayer.GetComponentInChildren<PlayerSilhouette>();
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
