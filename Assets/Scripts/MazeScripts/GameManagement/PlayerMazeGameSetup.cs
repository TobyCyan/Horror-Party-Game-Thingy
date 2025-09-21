using UnityEngine;
using Unity.Cinemachine;
using Unity.Netcode;

public class PlayerMazeGameSetup : MonoBehaviour
{
    public CinemachineCamera playerCam;
    private bool isLocalPlayer;
    void Start()
    {
        if (null == MazeGameManager.Instance) return; // not in maze game
        isLocalPlayer = GetComponent<NetworkBehaviour>().IsOwner;
        RegisterCam();
    }

    private void RegisterCam()
    {
        if (isLocalPlayer)
        {
            if (null != MazeCameraManager.Instance)
            {
                MazeCameraManager.Instance.RegisterLocalPlayerCamera(playerCam);
            }

        }
        else
        {
            playerCam.enabled = false;
        }
    }
}
