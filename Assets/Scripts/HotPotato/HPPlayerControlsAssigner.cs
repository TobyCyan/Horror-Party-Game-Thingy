using System;
using Unity.Netcode;

public class HPPlayerControlsAssigner : NetworkBehaviour
{
    public Action<HPSkillInputManager> OnControlsAssigned;

    private void Awake()
    {
        PlayerManager.OnLocalPlayerSet += AssignControlsToPlayer;
        PlayerManager.OnPlayerRemoved += RemoveControlsFromPlayer;
    }

    public override void OnNetworkDespawn()
    {
        PlayerManager.OnLocalPlayerSet -= AssignControlsToPlayer;
        PlayerManager.OnPlayerRemoved -= RemoveControlsFromPlayer;
    }

    private void AssignControlsToPlayer(Player player)
    {
        if (player != null && player.IsOwner)
        {
            var inputManager = player.gameObject.AddComponent<HPSkillInputManager>();
            OnControlsAssigned?.Invoke(inputManager);
        }
    }

    private void RemoveControlsFromPlayer(Player player)
    {
        if (player != null && player.IsOwner)
        {
            if (player.TryGetComponent<HPSkillInputManager>(out var inputManager))
            {
                Destroy(inputManager);
            }
        }
    }
}
