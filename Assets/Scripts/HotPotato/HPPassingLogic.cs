using Unity.Netcode;
using UnityEngine;

public class HPPassingLogic : MonoBehaviour
{
    private void OnCollisionEnter(Collision other)
    {
        // Only marked person should try to pass
        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        if (!MarkManager.IsPlayerMarked(localClientId))
        {
            Debug.LogWarning($"Client {localClientId} tried to pass the mark but is not the marked player.");
            return;
        }
        
        if (other.gameObject.TryGetComponent(out Player player))
        {
            MarkManager.Instance.PassMarkToPlayer(localClientId, player.clientId);
        }
    }
}
