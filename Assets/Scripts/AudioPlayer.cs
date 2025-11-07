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

        AudioClip clip = GetClip(playbackMode);
        if (clip == null)
        {
            Debug.LogWarning("No valid audio clip found for playback.");
            return;
        }

        switch (playbackMode)
        {
            case PlaybackMode.OneShot:
                src.PlayOneShot(clip);
                break;
            case PlaybackMode.Loop:
                src.clip = clip;
                src.loop = true;
                src.Play();
                break;
            case PlaybackMode.Random:
                src.PlayOneShot(clip);
                break;
            default:
                Debug.LogWarning("Unsupported playback mode.");
                break;
        }
        Debug.Log($"[AudioPlayer] PlaySfx - Mode: {playbackMode}, Index: {playIndex}");
    }

    public void PlayGlobal(AudioSettings settings, Vector3 position)
    {
        AudioClip clip = GetClip(settings.mode);
        AudioSource.PlayClipAtPoint(clip, position, src.volume);
    }

    private AudioClip GetClip(PlaybackMode mode)
    {
        switch (mode)
        {
            case PlaybackMode.OneShot:
                if (IsIndexValid(playIndex))
                {
                    return audioSamples[playIndex];
                }
                else
                {
                    Debug.LogWarning($"OneShot index {playIndex} is out of range.");
                }
                break;
            case PlaybackMode.Loop:
                if (IsIndexValid(playIndex))
                {
                    return audioSamples[playIndex];
                }
                else
                {
                    Debug.LogWarning($"Loop index {playIndex} is out of range.");
                }
                break;
            case PlaybackMode.Random:
                AudioClip clip = audioSamples.PickRandom();
                return clip;
            default:
                Debug.LogWarning("Unsupported playback mode.");
                break;
        }
        return null;
    }

    private bool IsIndexValid(int index)
    {
        return index >= 0 && index < audioSamples.Count;
    }
}

[Serializable]
public struct AudioSettings
{
    public string requestorName;
    public AudioSamples samples;
    public AudioPlayer.PlaybackMode mode;
    public int index;

    public AudioSettings(string requestorName, AudioSamples samples, AudioPlayer.PlaybackMode mode, int index = 0)
    {
        this.requestorName = requestorName;
        this.samples = samples;
        this.mode = mode;
        this.index = index;
    }

    public readonly bool IsNullOrEmpty()
    {
        return samples == null || samples.Count == 0;
    }
}
