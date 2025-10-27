// BlindEffect.cs - Attach to PLAYER prefab
using Unity.Netcode;
using UnityEngine;

public class BlindEffect : NetworkBehaviour
{
    [Header("Effect Settings")]
    [SerializeField] private float defaultDuration = 5f;

    /// <summary>
    /// Apply blind effect to this player (call from server only)
    /// </summary>
    public void Apply(float duration = -1f)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("[BlindEffect] Apply() must be called on server!");
            return;
        }

        if (duration < 0)
            duration = defaultDuration;

        Debug.Log($"[BlindEffect - SERVER] Applying blind to player {OwnerClientId} for {duration}s");

        // Send RPC to ONLY this player (the owner)
        ApplyBlindClientRpc(duration, new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { OwnerClientId }
            }
        });
    }

    [ClientRpc]
    private void ApplyBlindClientRpc(float duration, ClientRpcParams clientRpcParams = default)
    {
        // This only runs on the owner client
        if (!IsOwner)
        {
            Debug.LogWarning("[BlindEffect - CLIENT] Received RPC but not owner, ignoring");
            return;
        }

        Debug.Log($"[BlindEffect - CLIENT] Activating blind for {duration}s");

        // Apply blind to this player's movement
        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null)
        {
            movement.Blind(duration);
            Debug.Log($"[BlindEffect - CLIENT] ✅ Blind applied");
        }
        else
        {
            Debug.LogError($"[BlindEffect - CLIENT] No PlayerMovement component!");
        }
    }
}