using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AudioHandler 
{
    public static void PlayRandomOneShot(this AudioSource audioSource, AudioClipDataMultiple data)
    {
        if(data.hasPitchVariation)
            audioSource.PlayRandomOneShotVariation(data);
        else
            audioSource.PlayRandomOneShotNormal(data);
    }   
    
    private static void PlayRandomOneShotNormal(this AudioSource audioSource, AudioClipDataMultiple data)
    {
        int i = Random.Range(0, data.audioClips.Length);
        audioSource.PlayOneShot(data.audioClips[i], data.volume);
    }

    private static void PlayRandomOneShotVariation(this AudioSource audioSource, AudioClipDataMultiple data)
    {
        int i = Random.Range(0, data.audioClips.Length);
        float volume = Random.Range(data.volumeVariation.x, data.volumeVariation.y);
        audioSource.pitch = Random.Range(data.pitchVariation.x, data.pitchVariation.y);
        audioSource.PlayOneShot(data.audioClips[i], volume);
    }

    public static void PlayOneShotVariation(this AudioSource audioSource, AudioClipDataSingle data)
    {
        float volume = Random.Range(data.volumeVariation.x, data.volumeVariation.y);
        audioSource.pitch = Random.Range(data.pitchVariation.x, data.pitchVariation.y);
        audioSource.PlayOneShot(data.audioClip, volume);
    }
    
    public static void PlayOneShotPitched(this AudioSource audioSource, AudioClipDataSingle data)
    {
        audioSource.pitch = data.pitch;
        audioSource.PlayOneShot(data.audioClip, data.volume);
    }
    
    public static void PlayOneShotPitched(this AudioSource audioSource, AudioClipDataSingle data, float pitch)
    {
        audioSource.pitch = pitch;
        audioSource.PlayOneShot(data.audioClip, data.volume);
    }

    public static void PlayRandomAudioVariation(this AudioSource audioSource, AudioClipDataMultiple data, bool playReverse = false)
    {
        int i = Random.Range(0, data.audioClips.Length);
        audioSource.clip = data.audioClips[i];
        
        audioSource.time = playReverse ? data.audioClips[i].length - .01f : 0;
        audioSource.volume = Random.Range(data.volumeVariation.x, data.volumeVariation.y);
        
        audioSource.pitch = Random.Range(data.pitchVariation.x, data.pitchVariation.y);
        float tmpPitch = audioSource.pitch;
        audioSource.pitch = playReverse ? -tmpPitch : tmpPitch;
        
        audioSource.Play();
    }

    
}
