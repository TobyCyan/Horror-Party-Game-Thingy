using System;
using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour
{
    // hold player user info
    public NetworkVariable<FixedString64Bytes> playerName = new(writePerm: NetworkVariableWritePermission.Owner);
    
    [SerializeField] protected GameObject cam;
    [SerializeField] protected AudioListener audioListener; 
    [SerializeField] protected PlayerMovement playerMovement;
    [SerializeField] protected PlayerCam playerCam;
    [SerializeField] protected Transform meshRoot;
    [SerializeField] private AudioPlayer audioPlayer;
    public Transform MeshRoot => meshRoot;

    public PlayerCam PlayerCam => playerCam;
    public ulong Id => NetworkObjectId;
    public ulong clientId;
    
    private readonly NetworkVariable<bool> isEliminated = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public bool IsEliminated => isEliminated.Value;
    public event Action OnPlayerEliminated;

    [SerializeField] private string defaultLayerName = "Default";
    int defaultLayer;

    // Generic score hooks if needed based on game
    private int int0 = 0; // HP/Maze: Traps Set
    private int int1 = 0; // HP/Maze: Sabotage success
    private int int2 = 0;
    public float float0 = 0; // HP: Time as Hot Potato
    private float float1 = 0;

    private void Awake()
    {
        defaultLayer = LayerMask.NameToLayer(defaultLayerName);
    }

    // Give owner control to stuff it should control
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (!IsOwner)
        {
            EnablePlayer(false);
        }

        clientId = OwnerClientId;
        isEliminated.Value = false;
        PlayerManager.Instance.AddPlayer(this);
        ScoreUiManager.Instance?.PlayerJoined(clientId);

        // Notify PlayerManager that this player is ready
        // Important for syncing game start only when all players are ready
        if (IsOwner)
            NotifyPlayerManagerServerRpc(clientId);
    }
    
    public override void OnNetworkDespawn()
    {
        PlayerManager.Instance.RemovePlayer(this);
        ScoreUiManager.Instance?.PlayerLeft(clientId);
        base.OnNetworkDespawn();
    }

    [Rpc(SendTo.Server)]
    private void NotifyPlayerManagerServerRpc(ulong clientId)
    {
        PlayerManager.Instance.NotifyPlayerReady(clientId);
    }

    public void EnablePlayer(bool enable)
    {
        // cam.SetActive(enable); // Switching to cinemachine priority
        playerMovement.enabled = enable;
        playerCam.enabled = enable;
        audioListener.enabled = enable;
        audioPlayer.enabled = enable;
    }

    public void LockPlayerInPlace()
    {
        // EnablePlayer(false);
        playerCam.enabled = false;
        playerMovement.FreezeInPlace();
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

    [Rpc(SendTo.Server)]
    public void EliminatePlayerServerRpc(RpcParams rpcParams = default)
    {
        // Verify that the caller owns this NetworkObject
        if (rpcParams.Receive.SenderClientId != OwnerClientId)
        {
            Debug.LogWarning($"[{name}] Unauthorized eliminate request from {rpcParams.Receive.SenderClientId} ignored.");
            return;
        }

        if (isEliminated.Value)
        {
            Debug.LogWarning($"[{name}] Player already eliminated. Duplicate eliminate request ignored.");
            return;
        }

        isEliminated.Value = true;
        NotifyPlayerEliminatedClientRpc(clientId);
        OnPlayerEliminated?.Invoke();
        StartCoroutine(DelayedDespawn());
    }

    [Rpc(SendTo.Everyone)]
    private void NotifyPlayerEliminatedClientRpc(ulong eliminatedClientId)
    {
        // Local feedback only
        if (NetworkManager.Singleton.LocalClientId == eliminatedClientId)
        {
            Debug.Log($"[Client] {eliminatedClientId} with local client Id {NetworkManager.Singleton.LocalClientId} You were eliminated.");
            // UIManager.Instance.ShowEliminatedScreen();
        }
        else
        {
            Debug.Log($"[Client] Player {eliminatedClientId} was eliminated.");
            // UIManager.Instance.ShowOtherPlayerEliminated(eliminatedClientId);
        }
    }

    private IEnumerator DelayedDespawn()
    {
        yield return null;
        SpawnManager.Instance.DespawnPlayerServerRpc(Id);
    }

    [Rpc(SendTo.Everyone)]
    public void ResetLayerRpc()
    {
        gameObject.layer = defaultLayer;
        SetMeshRootLayerRpc(defaultLayer);
    }

    [Rpc(SendTo.Everyone)]
    public void SetMeshRootLayerRpc(int layer)
    {
        if (meshRoot != null)
        {
            meshRoot.gameObject.layer = layer;
        }
    }

    public void PlayLocalAudio(AudioSettings settings)
    {
        if (settings.IsNullOrEmpty())
        {
            Debug.LogWarning($"AudioSettings is null or empty in requestor {settings.requestorName}.");
            return;
        }

        if (audioPlayer == null)
        {
            Debug.LogWarning($"AudioPlayer component is not assigned in Player {name}.");
            return;
        }

        audioPlayer.PlaySfx(settings);
    }

    public void PlayGlobalAudio(AudioSettings settings, Vector3 position)
    {
        if (settings.IsNullOrEmpty())
        {
            Debug.LogWarning($"AudioSettings is null or empty in requestor {settings.requestorName}.");
            return;
        }
        if (audioPlayer == null)
        {
            Debug.LogWarning($"AudioPlayer component is not assigned in Player {name}.");
            return;
        }
        audioPlayer.PlayGlobal(settings, position);
    }

    // // Give Last touch player authority to move it
    // [Rpc(SendTo.Server)]
    // void ChangeOwnerServerRpc(NetworkObject other, RpcParams rpcParams = default)
    // {
    //     other.ChangeOwnership(rpcParams.Receive.SenderClientId);
    // }
}