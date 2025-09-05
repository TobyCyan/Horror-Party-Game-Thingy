using UnityEngine;

/// Info about "who" is interacting and what was hit.
/// Pass whatever you have (user/camera); hit is optional.
public struct InteractionContext
{
    public Transform user;   // e.g., player root or camera transform
    public Camera camera;    // the camera doing the ray (optional)
    public RaycastHit hit;   // valid only if hasHit == true
    public bool hasHit;

    public InteractionContext(Transform user, Camera camera)
    {
        this.user = user;
        this.camera = camera;
        this.hit = default;
        this.hasHit = false;
    }

    public InteractionContext(Transform user, Camera camera, in RaycastHit hit)
    {
        this.user = user;
        this.camera = camera;
        this.hit = hit;
        this.hasHit = true;
    }
}
