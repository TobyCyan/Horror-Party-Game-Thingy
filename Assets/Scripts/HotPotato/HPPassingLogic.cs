using UnityEngine;

public class HPPassingLogic : MonoBehaviour
{
    private void OnCollisionEnter(Collision other)
    {
        Debug.Log("Collided with something");
        // Only marked person should try to pass
        if (MarkManager.Instance.currentMarkedPlayer.Id != PlayerManager.Instance.localPlayer.Id) return;
        
        if (other.gameObject.CompareTag("Player"))
        {
            MarkManager.Instance.PassMarkToPlayer(other.gameObject.GetComponent<Player>().Id);
        }
    }
}
