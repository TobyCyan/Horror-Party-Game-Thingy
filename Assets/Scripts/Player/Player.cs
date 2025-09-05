using Unity.Netcode;
using Unity.Collections;
using UnityEngine;


public class Player : NetworkBehaviour
{
    // hold player user info
    public NetworkVariable<FixedString64Bytes> playerName = new(writePerm: NetworkVariableWritePermission.Owner);
    
    [SerializeField] private GameObject cam;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerCam playerCam;
    public ulong Id => NetworkObjectId;

    // Give owner control to stuff it should control
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (!IsOwner)
        {
            cam.SetActive(false);
            playerMovement.enabled = false;
            playerCam.enabled = false;
        }
    }
}