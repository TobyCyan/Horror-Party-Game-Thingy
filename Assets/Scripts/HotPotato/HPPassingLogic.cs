using Unity.Netcode;
using UnityEngine;

public class HPPassingLogic : MonoBehaviour
{
    private const float PassCooldown = 5f;
    // Global pass time
    private static float lastPassTime = -Mathf.Infinity;

    private void OnCollisionEnter(Collision other)
    {
        // Only marked person should try to pass
        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        if (MarkManager.currentMarkedPlayer?.Id != localClientId) return;
        
        if (!CanPass()) return;

        if (other.gameObject.TryGetComponent(out Player player))
        {
            lastPassTime = Time.time;
            MarkManager.Instance.PassMarkToPlayer(localClientId, player.Id);
        }
    }

    private bool CanPass()
    {
        bool isCooldownOver = IsCooldownOver();
        return isCooldownOver;
    }

    private bool IsCooldownOver()
    {
        return Time.time - lastPassTime >= PassCooldown;
    }
}
