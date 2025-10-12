using UnityEngine;

public abstract class InteractableBase : MonoBehaviour, IInteractable
{
    [SerializeField] protected string prompt = "Interact";
    [Tooltip("If >= 0, require user within this distance to interact.")]
    [SerializeField] protected float interactionDistanceOverride = -1f;

    public virtual bool CanInteract(in InteractionContext ctx)
    {
        if (interactionDistanceOverride >= 0f && ctx.user)
        {
            // Prefer precise distance to the hit point if we have one.
            float d = ctx.hasHit
                ? Vector3.Distance(ctx.hit.point, transform.position)
                : Vector3.Distance(ctx.user.position, transform.position);

            if (d > interactionDistanceOverride) return false;
        }
        return true;
    }

    public abstract void Interact(in InteractionContext ctx);

    public virtual string GetPrompt(in InteractionContext ctx) => prompt;
}
