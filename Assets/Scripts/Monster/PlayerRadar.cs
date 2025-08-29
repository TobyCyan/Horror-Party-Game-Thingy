using UnityEngine;

public class PlayerRadar : MonoBehaviour
{
    [SerializeField] private float searchRadius = 10.0f;

    public bool IsPlayerInRange(out Transform player)
    {
        // Check for player within search radius
        Collider[] hits = Physics.OverlapSphere(transform.position, searchRadius);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                player = hit.transform;
                return true;
            }
        }

        player = null;
        return false;
    }
}
