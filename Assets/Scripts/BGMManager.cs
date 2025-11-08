using UnityEngine;

public class BGMManager : MonoBehaviour
{
    [SerializeField] private AudioSamples samples;
    [SerializeField] private AudioSource src;

    private void Awake()
    {
        src = GetComponent<AudioSource>();
        PlayerManager.OnAllPlayersLoaded += PlayBgm;
    }

    private void PlayBgm()
    {
        PlayerManager.OnAllPlayersLoaded -= PlayBgm;
        if (samples != null && samples.Count > 0)
        {
            src.clip = samples.PickRandom();
            src.loop = true;
            src.Play();
        }
    }
}
