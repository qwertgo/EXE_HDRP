using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AudioClipDataMultiple", menuName = "Audio/AudioClipDataMultiple", order = 2)]
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
