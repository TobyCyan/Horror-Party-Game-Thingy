using System;
using UnityEngine;

public class AudioPlayer : MonoBehaviour
{
    public enum PlaybackMode
    {
        OneShot,
        Loop,
        Random
    }

    private AudioSamples audioSamples;
    private int playIndex = 0;
    [SerializeField] private AudioSource src;

    public void PlaySfx(in AudioSettings settings)
    {
        audioSamples = settings.samples;
        playIndex = settings.index;
        PlaybackMode playbackMode = settings.mode;

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

[Serializable]
public struct AudioSettings
{
    public AudioSamples samples;
    public AudioPlayer.PlaybackMode mode;
    public int index;

    public AudioSettings(AudioSamples samples, AudioPlayer.PlaybackMode mode, int index = 0)
    {
        this.samples = samples;
        this.mode = mode;
        this.index = index;
    }

    public readonly bool IsNullOrEmpty()
    {
        return samples == null || samples.Count == 0;
    }
}
