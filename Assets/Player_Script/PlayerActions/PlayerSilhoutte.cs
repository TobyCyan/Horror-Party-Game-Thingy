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
    [SerializeField] string silhouetteLayerName = "Silhouette"; // layer used by URP RenderObjects
    [SerializeField] string baselineLayerName = "Player";      // restore target

    [SerializeField] float defaultDuration = 2f;

    int silhouetteLayer;
    int baselineLayer;

    public event Action OnSilhouetteShown;
    public event Action OnSilhouetteHidden;

    void Awake()
    {
        silhouetteLayer = LayerMask.NameToLayer(silhouetteLayerName);
        baselineLayer = LayerMask.NameToLayer(baselineLayerName);

        if (baselineLayer < 0)
            Debug.LogError($"Baseline layer '{baselineLayerName}' not found. Set it in Project Settings > Tags and Layers.");
        if (silhouetteLayer < 0)
            Debug.LogWarning($"Silhouette layer '{silhouetteLayerName}' not found. Silhouette effect will be skipped.");
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
        StartCoroutine(ShowOnceCo(seconds));
    }

    IEnumerator ShowOnceCo(float seconds)
    {
        // switch to Silhouette (only if that layer exists)
        if (silhouetteLayer >= 0)
        {
            Debug.Log($"PlayerSilhouette: ShowOnceCo {seconds} seconds on {gameObject}");
            SetLayerRecursively(meshRoot, silhouetteLayer);
            foreach (var r in meshRoot.GetComponentsInChildren<Renderer>(true))
                r.enabled = true;
        }

        OnSilhouetteShown?.Invoke();
        yield return new WaitForSeconds(seconds);

        // ALWAYS restore to baseline Player layer
        if (baselineLayer >= 0)
            SetLayerRecursively(meshRoot, baselineLayer);

        OnSilhouetteHidden?.Invoke();
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
}
