using UnityEngine;

public class ColorInteractable : InteractableBase
{
    [Header("Target")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private int materialIndex = 0;

    [Header("Colours")]
    [SerializeField] private Color initialColor = Color.white;
    [SerializeField] private Color afterColor = Color.red;

    [Header("Shader Property")]
    [SerializeField, Tooltip("Built-in: _Color, URP/HDRP: _BaseColor")]
    private string colorProperty = "_Color";
    [SerializeField] private bool autoDetectColorProperty = true;

    private bool showingAfter = false;
    private MaterialPropertyBlock mpb;

    void Reset()
    {
        prompt = "Change Colour";
        interactionDistanceOverride = -1f; // not used
        if (!targetRenderer) targetRenderer = GetComponentInChildren<Renderer>(true);
        DetectColorProperty();
        ReadInitialFromMaterial();
    }

    void Awake()
    {
        if (!targetRenderer) targetRenderer = GetComponentInChildren<Renderer>(true);
        mpb = new MaterialPropertyBlock();
        if (autoDetectColorProperty) DetectColorProperty();
        ApplyColorImmediate(initialColor);
    }

   
    public override bool CanInteract(in InteractionContext ctx) => targetRenderer != null;

    public override void Interact(in InteractionContext ctx)
    {
        if (!targetRenderer) return;

        // toggle instantly
        Color next = showingAfter ? initialColor : afterColor;
        showingAfter = !showingAfter;
        prompt = showingAfter ? "Revert Colour" : "Change Colour";

        ApplyColorImmediate(next);
    }

    private void DetectColorProperty()
    {
        if (!targetRenderer) return;
        var mats = targetRenderer.sharedMaterials;
        if (mats == null || mats.Length == 0) return;
        int idx = Mathf.Clamp(materialIndex, 0, mats.Length - 1);
        var m = mats[idx];
        if (!m) return;
        if (m.HasProperty("_BaseColor")) colorProperty = "_BaseColor";
        else if (m.HasProperty("_Color")) colorProperty = "_Color";
    }

    private void ReadInitialFromMaterial()
    {
        if (!targetRenderer) return;
        var mats = targetRenderer.sharedMaterials;
        if (mats == null || mats.Length == 0) return;
        int idx = Mathf.Clamp(materialIndex, 0, mats.Length - 1);
        var m = mats[idx];
        if (m && m.HasProperty(colorProperty))
            initialColor = m.GetColor(colorProperty);
    }

    private void ApplyColorImmediate(Color c)
    {
        targetRenderer.GetPropertyBlock(mpb, materialIndex);
        mpb.SetColor(colorProperty, c);
        targetRenderer.SetPropertyBlock(mpb, materialIndex);
    }
}
