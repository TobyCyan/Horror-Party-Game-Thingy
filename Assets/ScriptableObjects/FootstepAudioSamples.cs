using UnityEngine;
using System.Collections.Generic;


[CreateAssetMenu(fileName = "FootstepAudioSamples", menuName = "ScriptableObjects/FootstepAudioSamples")]
public class FootstepAudioSamples : ScriptableObject, IEnumerable<AudioClip>
{
    [SerializeField]
    private AudioClip[] _clips;

    public int Count { get { return _clips.Length; } }

    public AudioClip this[int index]
    {
        get { return _clips[index]; }
    }

    public AudioClip PickRandom()
    {
        return _clips[Random.Range(0, Count)];
    }

    public IEnumerator<AudioClip> GetEnumerator()
    {
        return (_clips as IEnumerable<AudioClip>).GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return _clips.GetEnumerator();
    }
}

