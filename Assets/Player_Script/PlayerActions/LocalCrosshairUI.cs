using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

/// <summary>
/// Local crosshair UI for each player
/// Assign your crosshair sprite in the inspector
/// </summary>
public class LocalCrosshairUI : NetworkBehaviour
{
    [Header("Crosshair Sprite")]
    [SerializeField] private Sprite crosshairSprite;

    [Header("Settings")]
    [SerializeField] private Vector2 crosshairSize = new Vector2(32f, 32f);
    [SerializeField] private Color validColor = new Color(0f, 1f, 0f, 0.8f); // Green
    [SerializeField] private Color invalidColor = new Color(1f, 0f, 0f, 0.8f); // Red
    [SerializeField] private Color defaultColor = new Color(1f, 1f, 1f, 0.8f); // White

    private Canvas canvas;
    private Image crosshairImage;
    private bool isInitialized = false;

    // =========================================================
    // === INITIALIZATION ======================================
    // =========================================================

    void Start()
    {
        Invoke(nameof(Initialize), 0.1f);
    }

    void Initialize()
    {
        if (!IsOwner)
        {
            Debug.Log("[LocalCrosshairUI] Not owner, disabling crosshair");
            enabled = false;
            return;
        }

        Debug.Log("[LocalCrosshairUI] Creating crosshair for local player");
        CreateCrosshair();
    }

    void OnDestroy()
    {
        if (canvas != null)
        {
            Destroy(canvas.gameObject);
        }
    }

    // =========================================================
    // === CREATE CROSSHAIR ====================================
    // =========================================================

    void CreateCrosshair()
    {
        // Create canvas
        GameObject canvasObj = new GameObject("CrosshairCanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // Very high so it's always on top

        // Add CanvasScaler
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Add GraphicRaycaster
        canvasObj.AddComponent<GraphicRaycaster>();

        // Create crosshair image
        GameObject crosshairObj = new GameObject("Crosshair");
        crosshairObj.transform.SetParent(canvas.transform, false);

        // Add Image component
        crosshairImage = crosshairObj.AddComponent<Image>();

        // Set sprite
        if (crosshairSprite != null)
        {
            crosshairImage.sprite = crosshairSprite;
        }
        else
        {
            Debug.LogWarning("[LocalCrosshairUI] No sprite assigned! Crosshair may not be visible.");
        }

        crosshairImage.color = defaultColor;

        // Setup RectTransform - CENTER OF SCREEN
        RectTransform rect = crosshairImage.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero; // Dead center
        rect.sizeDelta = crosshairSize;

        // Hide by default
        crosshairObj.SetActive(false);

        isInitialized = true;
        Debug.Log("[LocalCrosshairUI] Crosshair created and centered on screen!");
    }

    // =========================================================
    // === PUBLIC API ==========================================
    // =========================================================

    public void SetCrosshairVisible(bool visible)
    {
        if (!isInitialized || crosshairImage == null) return;

        crosshairImage.gameObject.SetActive(visible);
    }

    public void SetValidPlacement(bool isValid)
    {
        if (!isInitialized || crosshairImage == null) return;

        crosshairImage.color = isValid ? validColor : invalidColor;
    }

    public void SetCrosshairColor(Color color)
    {
        if (!isInitialized || crosshairImage == null) return;

        crosshairImage.color = color;
    }

    public void ResetColor()
    {
        if (!isInitialized || crosshairImage == null) return;

        crosshairImage.color = defaultColor;
    }
}