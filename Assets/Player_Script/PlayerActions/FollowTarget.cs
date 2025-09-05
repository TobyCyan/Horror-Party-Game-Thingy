using UnityEngine;

public class FollowTarget : MonoBehaviour
{
    [Header("Target")]
    public Transform target;                 // assign your character here
    [Tooltip("If empty, will try to find object with tag 'Player' at Start.")]
    public bool autoFindPlayer = true;

    [Header("Position")]
    public Vector3 offset = new Vector3(0f, 1.7f, -4f);
    [Tooltip("Higher = snappier, lower = smoother.")]
    public float followLerp = 10f;

    [Header("Rotation")]
    public bool lookAtTarget = true;         // rotate to face the target
    public Vector3 lookAtOffset = new Vector3(0f, 1.5f, 0f);
    public bool lockRoll = true;             // keep Z = 0 to avoid camera tilt

    [Header("Debug")]
    public bool drawGizmoLine = true;

    void Start()
    {
        if (!target && autoFindPlayer)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player) target = player.transform;
        }
    }

    void LateUpdate()
    {
        if (!target) return;

        // Smooth position
        Vector3 desired = target.TransformPoint(offset);
        transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-followLerp * Time.deltaTime));

        // Rotation
        if (lookAtTarget)
        {
            Vector3 lookPoint = target.position + lookAtOffset;
            Vector3 dir = (lookPoint - transform.position);
            if (dir.sqrMagnitude > 0.0001f)
            {
                Quaternion rot = Quaternion.LookRotation(dir.normalized, Vector3.up);
                if (lockRoll) rot = Quaternion.Euler(rot.eulerAngles.x, rot.eulerAngles.y, 0f);
                transform.rotation = rot;
            }
        }
    }

    void OnDrawGizmos()
    {
        if (drawGizmoLine && target)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, target.position + lookAtOffset);
        }
    }
}
