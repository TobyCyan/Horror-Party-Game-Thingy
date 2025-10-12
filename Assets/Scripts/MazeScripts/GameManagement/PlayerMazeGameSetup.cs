using UnityEngine;
using Unity.Cinemachine;
using Unity.Netcode;

public class PlayerMazeGameSetup : NetworkBehaviour
{
    public CinemachineCamera playerCam;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsOwner)
        {
            playerCam.enabled = false;
            return;
        }

        if (MazeCameraManager.Instance != null)
        {
            Debug.Log("registering cam");
            MazeCameraManager.Instance.RegisterLocalPlayerCamera(playerCam);
        }
    }

}
