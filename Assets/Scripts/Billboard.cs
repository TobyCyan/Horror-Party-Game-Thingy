using Unity.Cinemachine;
using UnityEngine;

public class Billboard : MonoBehaviour
{
    [SerializeField] private GameObject billboardObject;
    [Header("Lock Rotation Axes")]
    [SerializeField] private bool lockX = false;
    [SerializeField] private bool lockY = false;
    [SerializeField] private bool lockZ = false;
    private Vector3 initialRotation;
    private new CinemachineCamera camera;

    private void Awake()
    {
        initialRotation = billboardObject.transform.eulerAngles;
        PlayerManager.OnLocalPlayerSet += AssignCamera;
    }

    private void LateUpdate()
    {
        if (camera == null)
            return;

        transform.LookAt(transform.position + camera.transform.rotation * Vector3.forward,
                         camera.transform.rotation * Vector3.up);

        Vector3 eulerAngles = billboardObject.transform.eulerAngles;
        if (lockX) eulerAngles.x = initialRotation.x;
        if (lockY) eulerAngles.y = initialRotation.y;
        if (lockZ) eulerAngles.z = initialRotation.z;
    }

    private void OnDestroy()
    {
        PlayerManager.OnLocalPlayerSet -= AssignCamera;
    }

    private void AssignCamera(Player player)
    {
        camera = player.PlayerCam.playerCam;
    }
}
