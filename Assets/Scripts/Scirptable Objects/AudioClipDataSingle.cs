using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AudioClipDataSingle", menuName = "Audio/AudioClipDataSingle", order = 1)]
public class AudioClipDataSingle : ScriptableObject
{
    public float volume = 1;
    public float pitch = 1;

    public bool hasPitchVariation;

    public Vector2 volumeVariation = new (.8f, 1);
    public Vector2 pitchVariation = new (.9f, 1.1f);
    public AudioClip audioClip;
}
