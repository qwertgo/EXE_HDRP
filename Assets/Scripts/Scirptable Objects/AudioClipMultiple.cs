using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// [CreateAssetMenu(fileName = "AudioClipMultiple", menuName = "Audio/AudioClipMutliple", order = 1)]
// public abstract class AudioClipData : ScriptableObject
// {
//     public float volume = 1;
//     public float pitch = 1;
//
//     public bool hasPitchVariation;
//
//     public Vector2 volumeVariation = new (.8f, 1);
//     public Vector2 pitchVariation = new (.9f, 1.1f);
// }

[CreateAssetMenu(fileName = "AudioClipSingle", menuName = "Audio/AudioClipSingle", order = 1)]
public class AudioClipDataSingle : ScriptableObject
{
    public float volume = 1;
    public float pitch = 1;

    public bool hasPitchVariation;

    public Vector2 volumeVariation = new (.8f, 1);
    public Vector2 pitchVariation = new (.9f, 1.1f);
    public AudioClip audioClip;
}

[CreateAssetMenu(fileName = "AudioClipMultiple", menuName = "Audio/AudioClipMultiple", order = 2)]
public class AudioClipDataMultiple : ScriptableObject
{
    public float volume = 1;
    public float pitch = 1;

    public bool hasPitchVariation;

    public Vector2 volumeVariation = new (.8f, 1);
    public Vector2 pitchVariation = new (.9f, 1.1f);
    public string path;
    public AudioClip[] audioClips { get; private set; }

    public void LoadClips()
    {
        audioClips = Resources.LoadAll<AudioClip>(path);
    }
}


