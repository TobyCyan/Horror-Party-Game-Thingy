using UnityEngine;

public class TrapTriggerForwarder : MonoBehaviour
{
    private BlindTrap parentTrap;

    private void Awake()
    {
        parentTrap = GetComponentInParent<BlindTrap>();

        if (parentTrap == null)
        {
            Debug.LogError("[TrapTriggerForwarder] No BlindTrap found in parent!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (parentTrap != null)
        {
            parentTrap.ProcessTrigger(other);
            Debug.Log($"[TrapTriggerForwarder] Forwarded trigger from {other.name}");
        }
    }
}