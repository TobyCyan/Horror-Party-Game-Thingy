using UnityEngine;

public class RevealedUi : MonoBehaviour, IPlayerBindedUi
{
    private PlayerSilhouette playerSilhouette;

    private void Start()
    {
        Hide();
    }

    private void OnDestroy()
    {
        if (playerSilhouette != null)
        {
            playerSilhouette.OnSilhouetteShown -= Reveal;
        }
    }

    private void SetAndBindPlayerSilhouette(PlayerSilhouette silhouette)
    {
        playerSilhouette = silhouette;
        if (playerSilhouette == null)
        {
            Debug.LogWarning("PlayerSilhouette is null in RevealUi.");
            return;
        }

        playerSilhouette.OnSilhouetteShown += Reveal;
    }

    private void Reveal()
    {
        gameObject.SetActive(true);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }

    public void BindPlayer(GameObject playerObject)
    {
        if (playerObject == null)
        {
            Debug.LogWarning("Player object is null in BindPlayer.");
            return;
        }

        if (playerObject.TryGetComponent<PlayerSilhouette>(out var silhouette))
        {
            SetAndBindPlayerSilhouette(silhouette);
        }
        else
        {
            Debug.LogWarning("Player object does not have a PlayerSilhouette component.");
        }
    }
}
