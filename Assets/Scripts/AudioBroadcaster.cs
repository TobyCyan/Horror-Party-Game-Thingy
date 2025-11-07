using Unity.Netcode;
using UnityEngine;

public class AudioBroadcaster : MonoBehaviour
{
    public void PlaySfxLocally(AudioSettings settings, ulong targetId)
    {
        if (settings.IsNullOrEmpty())
        {
            Debug.LogWarning($"PlaySfxLocallyRpc called with null or empty AudioSettings from {settings.requestorName}.");
            return;
        }

        if (NetworkManager.Singleton.LocalClientId == targetId)
        {
            Player localPlayer = PlayerManager.Instance.localPlayer;
            localPlayer.PlayLocalAudio(settings);
        }
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
