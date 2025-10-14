using UnityEngine;

public class TrapTriggerForwarder : MonoBehaviour
{
    private BlindTrap parentTrap;

    private void Awake()
    {
        parentTrap = GetComponentInParent<BlindTrap>();
    }

    private void OnTriggerEnter(Collider other)
    {
        parentTrap?.HandleTriggerEnter(other);
        Debug.Log("Forwarded");
    }
}
