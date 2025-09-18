    using Unity.Netcode;
using Unity.Collections;
using UnityEngine;

public class Player : NetworkBehaviour
{
    // hold player user info
    public NetworkVariable<FixedString64Bytes> playerName = new(writePerm: NetworkVariableWritePermission.Owner);
    
    [SerializeField] protected GameObject cam;
    [SerializeField] protected PlayerMovement playerMovement;
    [SerializeField] protected PlayerCam playerCam;

    public PlayerCam PlayerCam => playerCam;
    public ulong Id => NetworkObjectId;

    // Give owner control to stuff it should control
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (!IsOwner)
        {
            EnablePlayer(false);
        }
        
        PlayerManager.Instance.AddPlayer(this);
    }

    public void EnablePlayer(bool enable)
    {
        cam.SetActive(enable);
        playerMovement.enabled = enable;
        playerCam.enabled = enable;
    }

    public void LockPlayerInPlace()
    {
        EnablePlayer(false);
        playerCam.LookStraight();
    }
    
    // // Give Last touch player authority to move it
    // [Rpc(SendTo.Server)]
    // void ChangeOwnerServerRpc(NetworkObject other, RpcParams rpcParams = default)
    // {
    //     other.ChangeOwnership(rpcParams.Receive.SenderClientId);
    // }
}