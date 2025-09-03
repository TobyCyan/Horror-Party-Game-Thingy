using UnityEngine;
using UnityEngine.LightTransport;

public interface IInteractable
{
    bool CanInteract(in InteractionContext ctx);
    void Interact(in InteractionContext ctx);
    string GetPrompt(in InteractionContext ctx);
}
