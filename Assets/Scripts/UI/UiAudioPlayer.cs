using UnityEngine;

public class UIAudioPlayer : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioSamples buttonClickAudioSamples;

    public void PlayButtonClick()
    {
        if (audioSource == null)
        {
            Debug.LogWarning("AudioSource is not assigned.");
            return;
        }

        if (buttonClickAudioSamples != null && buttonClickAudioSamples.Count > 0)
        {
            AudioClip clip = buttonClickAudioSamples.PickRandom();
            audioSource.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning("Button click audio samples are not assigned.");
        }
    }
}
