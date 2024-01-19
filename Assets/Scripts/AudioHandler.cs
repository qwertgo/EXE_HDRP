using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AudioHandler 
{
    public static void PlayRandomOneShot(this AudioSource audioSource, AudioClip[] audioClips, float volume = 1)
    {
        int i = Random.Range(0, audioClips.Length);
        audioSource.PlayOneShot(audioClips[i], volume);
    }

    public static void PlayRandomOneShotVariation(this AudioSource audioSource, AudioClip[] audioClips, Vector2 volumeRange,
        Vector2 pitchRange)
    {
        int i = Random.Range(0, audioClips.Length);
        float volume = Random.Range(volumeRange.x, volumeRange.y);
        audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        audioSource.PlayOneShot(audioClips[i], volume);
    }

    public static void PlayOneShotVariation(this AudioSource audioSource,AudioClip clip, Vector2 volumeRange, Vector2 pitchRange)
    {
        audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        float volume = Random.Range(volumeRange.x, volumeRange.y);
        audioSource.PlayOneShot(clip, volume);
    }

    public static void PlayRandomAudio(this AudioSource audioSource, AudioClip[] audioClips, float volume = 1, bool playReverse = false)
    {
        int i = Random.Range(0, audioClips.Length);
        audioSource.pitch = playReverse ? -1 : 1;
        audioSource.time = playReverse ? audioClips[i].length - .01f : 0;
        audioSource.volume = volume;
        audioSource.clip = audioClips[i];
        audioSource.Play();
    }

    public static void PlayRandomAudioVariation(this AudioSource audioSource, AudioClip[] audioClips, Vector2 volumeRange,
        Vector2 pitchRange, bool playReverse = false)
    {
        int i = Random.Range(0, audioClips.Length);
        audioSource.clip = audioClips[i];
        
        audioSource.time = playReverse ? audioClips[i].length - .01f : 0;
        audioSource.volume = Random.Range(volumeRange.x, volumeRange.y);
        
        audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        float tmpPitch = audioSource.pitch;
        audioSource.pitch = playReverse ? -tmpPitch : tmpPitch;
        
        audioSource.Play();
    }
}
