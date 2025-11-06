using UnityEngine;

public class AudioPlayer : MonoBehaviour
{
    public enum PlaybackMode
    {
        OneShot,
        Loop,
        Random
    }

    [SerializeField] private AudioSamples audioSamples;
    [SerializeField] private AudioSource src;
    [SerializeField] private PlaybackMode playbackMode = PlaybackMode.OneShot;
    [Header("Non-Random Playback Settings")]
    [SerializeField] private int playIndex = 0;


    public void PlaySfx()
    {
        if (audioSamples == null || audioSamples.Count == 0)
        {
            Debug.LogWarning("No audio samples assigned to AudioPlayer.");
            return;
        }

        switch (playbackMode)
        {
            case PlaybackMode.OneShot:
                if (IsIndexValid(playIndex))
                {
                    src.PlayOneShot(audioSamples[playIndex]);
                }
                else
                {
                    Debug.LogWarning($"OneShot index {playIndex} is out of range.");
                }
                break;
            case PlaybackMode.Loop:
                if (IsIndexValid(playIndex))
                {
                    src.clip = audioSamples[playIndex];
                    src.loop = true;
                    src.Play();
                }
                else
                {
                    Debug.LogWarning($"Loop index {playIndex} is out of range.");
                }
                break;
            case PlaybackMode.Random:
                AudioClip clip = audioSamples.PickRandom();
                src.PlayOneShot(clip);
                break;
            default:
                Debug.LogWarning("Unsupported playback mode.");
                break;
        }
        Debug.Log($"[AudioPlayer] PlaySfx - Mode: {playbackMode}, Index: {playIndex}");
    }

    private bool IsIndexValid(int index)
    {
        return index >= 0 && index < audioSamples.Count;
    }
}
