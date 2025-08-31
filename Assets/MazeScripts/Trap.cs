using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class Trap : MonoBehaviour
{
    [Header("Effect to perform when triggered")]
    [SerializeField] private UnityEvent effect;

    // coroutine instead?
    public void Trigger()
    {
        Debug.Log("Trap Triggered");
        effect?.Invoke();
    }
}