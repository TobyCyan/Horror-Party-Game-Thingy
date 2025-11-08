using UnityEngine;

public class TrapTriggerForwarder : MonoBehaviour
{
    private TrapBase parentTrap;

    private void Awake()
    {
        parentTrap = GetComponentInParent<TrapBase>();

        if (parentTrap == null)
        {
            Debug.LogError("[TrapTriggerForwarder] No TrapBase found in parent!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (parentTrap != null)
        {
            parentTrap.HandleTriggerEnter(other);
            Debug.Log($"[TrapTriggerForwarder] Forwarded trigger from {other.name}");
        }
    }
}