using Unity.Netcode;
using UnityEngine;

public class AudioBroadcaster : MonoBehaviour
{
    public void PlaySfxLocal(AudioSettings settings, ulong targetId)
    {
        if (settings.IsNullOrEmpty())
        {
            Debug.LogWarning($"PlaySfxLocallyRpc called with null or empty AudioSettings from {settings.requestorName}.");
            return;
        }

        if (NetworkManager.Singleton.LocalClientId != targetId)
        {
            Debug.LogWarning($"PlaySfxLocal called with targetId {targetId} that does not match local client ID {NetworkManager.Singleton.LocalClientId}.");
            return;
        }

        Player localPlayer = PlayerManager.Instance.localPlayer;
        localPlayer.PlayLocalAudio(settings);
    }

    public void PlaySfxLocalToAll(AudioSettings settings)
    {
        if (settings.IsNullOrEmpty())
        {
            Debug.LogWarning($"PlaySfxToAll called with null or empty AudioSettings from {settings.requestorName}.");
            return;
        }

        Player localPlayer = PlayerManager.Instance.localPlayer;
        localPlayer.PlayLocalAudio(settings);
    }

    public void PlaySfxGlobal(AudioSettings settings, Vector3 position)
    {
        if (settings.IsNullOrEmpty())
        {
            Debug.LogWarning($"PlaySfxToAll called with null or empty AudioSettings from {settings.requestorName}.");
            return;
        }

        Player localPlayer = PlayerManager.Instance.localPlayer;
        localPlayer.PlayGlobalAudio(settings, position);
    }
}
