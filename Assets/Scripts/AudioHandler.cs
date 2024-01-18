using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AudioHandler 
{
    public static void PlayOneShotRandom(AudioSource audioSource, AudioClip[] audioClips, float soundVolume = 1)
    {
        int i = Random.Range(0, audioClips.Length);
        audioSource.PlayOneShot(audioClips[i], soundVolume);
    }

    public static void PlayAudioRandom(AudioSource audioSource, AudioClip[] audioClips, float soundVolume = 1, bool playReverse = false)
    {
        int i = Random.Range(0, audioClips.Length);
        audioSource.pitch = playReverse ? -1 : 1;
        audioSource.time = playReverse ? audioClips[i].length - .01f : 0;
        audioSource.volume = soundVolume;
        audioSource.clip = audioClips[i];
        audioSource.Play();
    }
}
