using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    [SerializeField] protected PlayerInput playerInput;

    protected virtual void OnValidate()
    {
        if (playerInput == null)
        {
            Debug.LogWarning($"InputManager on {name} has no PlayerInput assigned.");
        }
    }
}
