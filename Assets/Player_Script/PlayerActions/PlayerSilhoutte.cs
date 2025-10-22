using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerSilhouette : NetworkBehaviour
{
    [Header("Renderer subtree ONLY (not the root player)")]
    [SerializeField] Transform meshRoot;

    [Header("Layers")]
    [SerializeField] LayerMask silhouetteLayer; // layer used by URP RenderObjects
    [SerializeField] LayerMask baselineLayer;   // restore target

    [SerializeField] float defaultDuration = 2f;

    // Store the original layers before we change them
    private Dictionary<GameObject, int> originalLayers = new Dictionary<GameObject, int>();
    private bool isSilhouetteActive = false;

    public event Action OnSilhouetteShown;
    public event Action OnSilhouetteHidden;

    void Awake()
    {
        // Validate that we have valid single layers
        if (silhouetteLayer == 0)
            Debug.LogError("Silhouette layer not set!");
        if (baselineLayer == 0)
            Debug.LogError("Baseline layer not set!");
    }

    // Server entry point
    [Rpc(SendTo.Server)]
    public void ShowForSecondsRpc(float seconds = -1f)
    {
        if (seconds <= 0f) seconds = defaultDuration;
        ShowOnceRpc(seconds);
    }

    [Rpc(SendTo.Everyone)]
    void ShowOnceRpc(float seconds)
    {
        if (!meshRoot) return;

        // If already showing silhouette, restart the timer with new duration
        if (isSilhouetteActive)
        {
            StopAllCoroutines();
        }

        StartCoroutine(ShowOnceCo(seconds));
    }

    IEnumerator ShowOnceCo(float seconds)
    {
        // Save original layers and switch to Silhouette
        int silhouetteLayerIndex = LayerMaskToLayer(silhouetteLayer);

        if (silhouetteLayerIndex >= 0 && !isSilhouetteActive)
        {
            Debug.Log($"PlayerSilhouette: ShowOnceCo {seconds} seconds on {gameObject}");

            // Save original layers before changing
            SaveOriginalLayers(meshRoot);

            // Switch to silhouette layer
            SetLayerRecursively(meshRoot, silhouetteLayerIndex);

            foreach (var r in meshRoot.GetComponentsInChildren<Renderer>(true))
                r.enabled = true;

            isSilhouetteActive = true;
        }

        OnSilhouetteShown?.Invoke();
        yield return new WaitForSeconds(seconds);

        // Restore to ORIGINAL layers (not just baseline)
        RestoreOriginalLayers();
        isSilhouetteActive = false;

        OnSilhouetteHidden?.Invoke();
    }

    /// <summary>
    /// Converts a LayerMask to a single layer index
    /// </summary>
    private int LayerMaskToLayer(LayerMask layerMask)
    {
        int layerNumber = 0;
        int layer = layerMask.value;
        while (layer > 1)
        {
            layer = layer >> 1;
            layerNumber++;
        }
        return layerNumber;
    }

    /// <summary>
    /// Saves the current layer of all GameObjects in the hierarchy
    /// </summary>
    private void SaveOriginalLayers(Transform root)
    {
        originalLayers.Clear();
        var stack = new Stack<Transform>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var t = stack.Pop();
            originalLayers[t.gameObject] = t.gameObject.layer;

            for (int i = 0; i < t.childCount; i++)
                stack.Push(t.GetChild(i));
        }

        Debug.Log($"[PlayerSilhouette] Saved {originalLayers.Count} original layers");
    }

    /// <summary>
    /// Restores all GameObjects to their original layers
    /// </summary>
    private void RestoreOriginalLayers()
    {
        if (originalLayers.Count == 0)
        {
            Debug.LogWarning("[PlayerSilhouette] No original layers to restore! Falling back to baseline.");

            // Fallback: restore to baseline if we somehow lost the original layers
            int baselineLayerIndex = LayerMaskToLayer(baselineLayer);
            if (baselineLayerIndex >= 0 && meshRoot != null)
                SetLayerRecursively(meshRoot, baselineLayerIndex);

            return;
        }

        int restoredCount = 0;
        foreach (var kvp in originalLayers)
        {
            if (kvp.Key != null) // Check if GameObject still exists
            {
                kvp.Key.layer = kvp.Value;
                restoredCount++;
            }
        }

        Debug.Log($"[PlayerSilhouette] Restored {restoredCount} objects to original layers");
        originalLayers.Clear();
    }

    static void SetLayerRecursively(Transform root, int layer)
    {
        var stack = new Stack<Transform>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var t = stack.Pop();
            t.gameObject.layer = layer;
            for (int i = 0; i < t.childCount; i++) stack.Push(t.GetChild(i));
        }
    }

    // Utility method to check current state
    public bool IsSilhouetteActive => isSilhouetteActive;

    // Force hide silhouette (useful for debugging or emergency stop)
    public void ForceHideSilhouette()
    {
        StopAllCoroutines();
        RestoreOriginalLayers();
        isSilhouetteActive = false;
        OnSilhouetteHidden?.Invoke();
    }

    private void OnDestroy()
    {
        // Clean up if destroyed while active
        if (isSilhouetteActive)
        {
            RestoreOriginalLayers();
        }
    }
}