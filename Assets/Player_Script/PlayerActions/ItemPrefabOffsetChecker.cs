using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Attach this to your item prefabs to check for offset issues
/// This will help identify why items spawn at wrong positions
/// </summary>
[ExecuteInEditMode]
public class ItemPrefabOffsetChecker : MonoBehaviour
{
    [Header("Debug Info (Read Only)")]
    [SerializeField] private bool hasOffsetIssues = false;
    [SerializeField] private Vector3 meshCenterOffset;
    [SerializeField] private Vector3 colliderCenterOffset;
    [SerializeField] private Vector3 pivotPosition;

    [Header("Visual Debug")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private float gizmoSize = 0.5f;

    private void OnValidate()
    {
        CheckForOffsets();
    }

    private void Awake()
    {
        if (Application.isPlaying)
        {
            CheckForOffsets();
            LogIssues();
        }
    }

    private void Start()
    {
        if (Application.isPlaying)
        {
            // Log spawn position when created
            Debug.Log($"[ItemPrefabChecker] '{gameObject.name}' spawned at: {transform.position}");

            // Check if position is being modified by physics
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                Debug.LogWarning($"[ItemPrefabChecker] '{gameObject.name}' has non-kinematic Rigidbody! This may cause position drift.");
            }
        }
    }

    private void CheckForOffsets()
    {
        hasOffsetIssues = false;

        // Get the pivot position (transform position in local space should be 0,0,0 for a properly centered prefab)
        pivotPosition = transform.localPosition;

        // Check mesh renderers
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        if (renderers.Length > 0)
        {
            Bounds combinedBounds = new Bounds(transform.position, Vector3.zero);
            foreach (var renderer in renderers)
            {
                combinedBounds.Encapsulate(renderer.bounds);
            }

            // Calculate offset between mesh center and pivot
            meshCenterOffset = transform.InverseTransformPoint(combinedBounds.center);

            if (meshCenterOffset.magnitude > 0.01f)
            {
                hasOffsetIssues = true;
            }
        }

        // Check colliders
        Collider[] colliders = GetComponentsInChildren<Collider>();
        if (colliders.Length > 0)
        {
            Bounds combinedColliderBounds = new Bounds(transform.position, Vector3.zero);
            foreach (var collider in colliders)
            {
                combinedColliderBounds.Encapsulate(collider.bounds);
            }

            // Calculate offset between collider center and pivot
            colliderCenterOffset = transform.InverseTransformPoint(combinedColliderBounds.center);

            if (colliderCenterOffset.magnitude > 0.01f)
            {
                hasOffsetIssues = true;
            }
        }
    }

    private void LogIssues()
    {
        if (hasOffsetIssues)
        {
            Debug.LogWarning($"[ItemPrefabChecker] '{gameObject.name}' has offset issues!");
            Debug.LogWarning($"  - Mesh Center Offset: {meshCenterOffset}");
            Debug.LogWarning($"  - Collider Center Offset: {colliderCenterOffset}");
            Debug.LogWarning($"  - Pivot Position: {pivotPosition}");
            Debug.LogWarning($"  These offsets may cause items to spawn at incorrect positions!");
        }
    }

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        // Draw pivot position (green)
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, gizmoSize * 0.5f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * gizmoSize);

        // Draw mesh center (blue)
        if (meshCenterOffset.magnitude > 0.01f)
        {
            Gizmos.color = Color.blue;
            Vector3 meshWorldCenter = transform.TransformPoint(meshCenterOffset);
            Gizmos.DrawWireSphere(meshWorldCenter, gizmoSize * 0.4f);
            Gizmos.DrawLine(transform.position, meshWorldCenter);
        }

        // Draw collider center (red)
        if (colliderCenterOffset.magnitude > 0.01f)
        {
            Gizmos.color = Color.red;
            Vector3 colliderWorldCenter = transform.TransformPoint(colliderCenterOffset);
            Gizmos.DrawWireSphere(colliderWorldCenter, gizmoSize * 0.3f);
            Gizmos.DrawLine(transform.position, colliderWorldCenter);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw more detailed info when selected
        if (hasOffsetIssues)
        {
            // Draw offset vectors
            Gizmos.color = Color.yellow;

            // Draw text labels (using Handles in editor)
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * (gizmoSize + 0.5f),
                $"OFFSET WARNING\nMesh: {meshCenterOffset}\nCollider: {colliderCenterOffset}");
#endif
        }
    }

    // Context menu options for quick fixes
    [ContextMenu("Center Mesh on Pivot")]
    private void CenterMeshOnPivot()
    {
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        foreach (var renderer in renderers)
        {
            renderer.transform.localPosition -= meshCenterOffset;
        }
        CheckForOffsets();
        Debug.Log("Centered mesh on pivot");
    }

    [ContextMenu("Center Collider on Pivot")]
    private void CenterColliderOnPivot()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (var collider in colliders)
        {
            if (collider.transform == transform)
            {
                // For colliders on the same GameObject, adjust the center property
                if (collider is BoxCollider box)
                {
                    box.center -= colliderCenterOffset;
                }
                else if (collider is SphereCollider sphere)
                {
                    sphere.center -= colliderCenterOffset;
                }
                else if (collider is CapsuleCollider capsule)
                {
                    capsule.center -= colliderCenterOffset;
                }
            }
            else
            {
                // For child colliders, adjust position
                collider.transform.localPosition -= colliderCenterOffset;
            }
        }
        CheckForOffsets();
        Debug.Log("Centered collider on pivot");
    }
}