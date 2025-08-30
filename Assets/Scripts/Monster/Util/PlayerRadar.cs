using UnityEngine;

public class PlayerRadar
{
    private readonly float searchRadius;

    public PlayerRadar(float searchRadius = 2.5f)
    {
        this.searchRadius = searchRadius;
    }

    public bool IsPlayerInRange(Vector3 origin, out Transform player)
    {
        // Check for player within search radius
        Collider[] hits = Physics.OverlapSphere(origin, searchRadius);
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
