using UnityEngine;

public class HPPlayerControlsAssigner : MonoBehaviour
{
    private void Start()
    {
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnPlayerAdded += AssignControlsToPlayer;
            PlayerManager.Instance.OnPlayerRemoved += RemoveControlsFromPlayer;
        }
    }

    private void OnDestroy()
    {
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnPlayerAdded -= AssignControlsToPlayer;
            PlayerManager.Instance.OnPlayerRemoved -= RemoveControlsFromPlayer;
        }
    }

    private void AssignControlsToPlayer(Player player)
    {
        if (player != null && player.IsOwner)
        {
            var _ = player.gameObject.AddComponent<HPSkillInputManager>();
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
