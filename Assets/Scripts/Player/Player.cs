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
    public PlayerCam PlayerCam => playerCam;
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
        
        PlayerManager.Instance.AddPlayer(this);
    }
    
    // // Give Last touch player authority to move it
    // [Rpc(SendTo.Server)]
    // void ChangeOwnerServerRpc(NetworkObject other, RpcParams rpcParams = default)
    // {
    //     other.ChangeOwnership(rpcParams.Receive.SenderClientId);
    // }
}