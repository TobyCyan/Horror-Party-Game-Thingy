using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(HPSkillInputManager))]
public class HPPlayerControlsAssigner : NetworkBehaviour
{
    private HPSkillInputManager skillInputManager;
    public override void OnNetworkSpawn()
    {
        if (skillInputManager == null)
        {
            skillInputManager = GetComponent<HPSkillInputManager>();
        }

        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnPlayerAdded += AssignControlsToPlayer;
            PlayerManager.Instance.OnPlayerRemoved += RemoveControlsFromPlayer;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnPlayerAdded -= AssignControlsToPlayer;
            PlayerManager.Instance.OnPlayerRemoved -= RemoveControlsFromPlayer;
        }
    }

    private void AssignControlsToPlayer(Player player)
    {
        if (player != null && player.IsOwner && skillInputManager != null)
        {
            var newManager = player.gameObject.AddComponent<HPSkillInputManager>();

            // Copy inspector values from the template
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(skillInputManager), newManager);
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
