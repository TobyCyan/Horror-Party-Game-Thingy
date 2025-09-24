    using Unity.Netcode;
using Unity.Collections;
using UnityEngine;
using System;

public class Player : NetworkBehaviour
{
    // hold player user info
    public NetworkVariable<FixedString64Bytes> playerName = new(writePerm: NetworkVariableWritePermission.Owner);
    
    [SerializeField] protected GameObject cam;
    [SerializeField] protected PlayerMovement playerMovement;
    [SerializeField] protected PlayerCam playerCam;

    public PlayerCam PlayerCam => playerCam;
    public ulong Id => NetworkObjectId;

    public event Action OnPlayerEliminated;

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

    public void Stun(float duration)
    {
        playerMovement.Stun(duration);
    }

    public void Blind(float duration)
    {
        playerMovement.Blind(duration);
    }

    public void EliminatePlayer()
    {
        Debug.Log($"Player {Id} eliminated.");
        OnPlayerEliminated?.Invoke();

        // TODO: Add logic to hide player and go into spectator mode
    }

    // // Give Last touch player authority to move it
    // [Rpc(SendTo.Server)]
    // void ChangeOwnerServerRpc(NetworkObject other, RpcParams rpcParams = default)
    // {
    //     other.ChangeOwnership(rpcParams.Receive.SenderClientId);
    // }
}