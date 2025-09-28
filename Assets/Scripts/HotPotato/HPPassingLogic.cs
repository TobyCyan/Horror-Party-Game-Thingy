using UnityEngine;

public class HPPassingLogic : MonoBehaviour
{
    private void OnCollisionEnter(Collision other)
    {
        Debug.Log("Collided with something");
        // Only marked person should try to pass
        if (MarkManager.Instance.currentMarkedPlayer.Id != PlayerManager.Instance.localPlayer.Id) return;
        
        if (other.gameObject.TryGetComponent(out Player player))
        {
            MarkManager.Instance.PassMarkToPlayer(player.Id);
        }
    }
}
