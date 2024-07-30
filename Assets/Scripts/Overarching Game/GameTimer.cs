using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class GameTimer : MonoBehaviour
{
    [SerializeField] private float gameTime;
    [SerializeField] private float startTickingTime = 15f;


    private float remainingTime;


    [HideInInspector]public float timeElapsed;
    [SerializeField] private TextMeshProUGUI remainingTimeText;
    [SerializeField] private Slider sliderRight;
    [SerializeField] private Slider sliderLeft;
    [SerializeField] private Color sliderColorBase;
    [SerializeField] private Color sliderColorSpecial;
    [SerializeField] private Material echsenMaterial;
    [SerializeField] private VolumeProfile volumeProfile;

    private IEnumerator update;
    private AudioSource audioSource;
    private Vignette vignette;

    private bool playTickingSound;

    private void Start()
    {
        remainingTime = gameTime;
        sliderLeft.image.color = sliderColorBase;
        sliderRight.image.color = sliderColorBase;

        audioSource = GetComponent<AudioSource>();

        update = UpdateCoroutine();
        StartCoroutine(update);
        StartCoroutine(WaitForTimeToPass(120f));

        foreach(var profile in volumeProfile.components)
        {
            if(profile is Vignette)
            {
                vignette = (Vignette)profile;
                vignette.intensity.Override(0f);
            }
        }
    }


    IEnumerator UpdateCoroutine()
    {
        while (!GameManager.instance.stoppedGame)
        {
            float scaledDeltaTime = Time.deltaTime * Time.timeScale;
            remainingTime -= scaledDeltaTime;
            timeElapsed += scaledDeltaTime;
            DisplayTimer();

            if (remainingTime <=  startTickingTime && !playTickingSound)
                StartCoroutine(PlayTickingSound());
            else if (remainingTime <= 0)
                EndGame();

            yield return null;
        }
        
    }

    IEnumerator WaitForTimeToPass(float timeToPass)
    {
        yield return new WaitUntil(() => timeElapsed >= timeToPass);
        GameVariables.instance.twoMinutesPassed.Invoke();
    }

    IEnumerator PlayTickingSound()
    {
        playTickingSound = true;
        float vignetteIntesnity = .7f;
        audioSource.Play();

        float t = 0;
        while (playTickingSound)
        {
            t = 1 - remainingTime / startTickingTime;
            float volume = t;
            audioSource.volume = volume;

            vignette.intensity.Override(t * vignetteIntesnity);
            yield return null;
        }

        t = 1;

        while(t > 0)
        {
            audioSource.volume = t;
            vignette.intensity.Override(t * vignetteIntesnity);

            t -= Time.deltaTime * .5f;
            yield return null;
        }
        
        audioSource.Stop();
        vignette.intensity.Override(0f);
    }

    void EndGame()
    {
        StopCoroutine(update);
        playTickingSound = false;
        GameManager.instance.GameOver();
    }

    public void AddToTimer(float addedTime)
    {
        remainingTime += addedTime;
        StartCoroutine(SliderGlowUp());
    }

    IEnumerator SliderGlowUp()
    {
        float t = 0;
        
        while (t < 2)
        {
            t += Time.deltaTime * 4;

            float k = Mathf.PingPong(t, 1f);
            
            Color tmpColor = Color.Lerp(sliderColorBase, sliderColorSpecial, k * k);

            sliderLeft.image.color = tmpColor;
            sliderRight.image.color = tmpColor;

            yield return null;
        }
    }

    void DisplayTimer()
    {
        GetTimeElapsed(out int minutes, out int seconds);
        remainingTimeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

        float remainingTimeInPercentage = remainingTime / gameTime;
        sliderLeft.value = remainingTimeInPercentage;
        sliderRight.value = remainingTimeInPercentage;
        
        echsenMaterial.SetFloat("_Percentage", remainingTimeInPercentage);
    }

    public void GetTimeElapsed(out int minutes, out int seconds)
    {
        minutes = Mathf.FloorToInt(timeElapsed / 60);
        seconds = Mathf.FloorToInt(timeElapsed % 60);
    }

    private void OnDestroy()
    {
        echsenMaterial.SetFloat("_Percentage", 1);
        remainingTime = 0;
        vignette.intensity.Override(0f);
        
    }
}
