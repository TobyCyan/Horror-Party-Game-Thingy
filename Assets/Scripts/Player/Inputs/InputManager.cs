using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    [SerializeField] protected InputActionAsset inputAction;

    protected virtual void OnValidate()
    {
        if (inputAction == null)
        {
            Debug.LogWarning($"InputManager on {name} has no InputAction assigned.");
        }
    }
}
