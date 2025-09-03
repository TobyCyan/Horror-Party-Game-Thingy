using UnityEngine;

public class MoveCamera : MonoBehaviour
{
    public Transform cameraPosition; // assign CameraMount (child of player)

    void LateUpdate()
    {
        if (!cameraPosition) return;
        transform.SetPositionAndRotation(cameraPosition.position, cameraPosition.rotation);
    }
}
