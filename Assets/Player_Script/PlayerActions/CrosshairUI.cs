using UnityEngine;

public class CrosshairUI : MonoBehaviour
{
    [Header("Crosshair Settings")]
    [SerializeField] private RectTransform crosshairImage;
    [SerializeField] private float crosshairSize = 20f;
    [SerializeField] private Color crosshairColor = Color.white;

    void Start()
    {
        SetCrosshairVisible(false);
        SetupCrosshair();
    }

    void SetupCrosshair()
    {
        if (crosshairImage == null)
        {
            Debug.LogWarning("[CrosshairUI] No crosshair image assigned!");
            return;
        }

        // Center the crosshair
        crosshairImage.anchoredPosition = Vector2.zero;
        crosshairImage.sizeDelta = new Vector2(crosshairSize, crosshairSize);

        //// Set size
        //crosshairImage.sizeDelta = new Vector2(crosshairSize, crosshairSize);

        // Set color
        UnityEngine.UI.Image img = crosshairImage.GetComponent<UnityEngine.UI.Image>();
        if (img != null)
        {
            img.color = crosshairColor;
        }
    }

    public void SetCrosshairColor(Color color)
    {
        if (crosshairImage != null)
        {
            UnityEngine.UI.Image img = crosshairImage.GetComponent<UnityEngine.UI.Image>();
            if (img != null)
            {
                img.color = color;
            }
        }
    }

    public void SetCrosshairVisible(bool visible)
    {
        if (crosshairImage != null)
        {
            crosshairImage.gameObject.SetActive(visible);
        }
    }
}