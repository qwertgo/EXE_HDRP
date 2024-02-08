using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class GameTimer : MonoBehaviour
{
    [SerializeField] private float gameTime;
    private float remainingTime;
    [HideInInspector]public float timeElapsed;
    [SerializeField] private TextMeshProUGUI remainingTimeText;
    [SerializeField] private Slider sliderRight;
    [SerializeField] private Slider sliderLeft;
    [SerializeField] private Color sliderColorBase;
    [SerializeField] private Color sliderColorSpecial;
    [SerializeField] private Material echsenMaterial;

    private IEnumerator update;
    private AudioSource audioSource;

    private bool playTickingSound;

    private void Start()
    {
        remainingTime = gameTime;
        sliderLeft.image.color = sliderColorBase;
        sliderRight.image.color = sliderColorBase;

        audioSource = GetComponent<AudioSource>();

        update = UpdateCoroutine();
        StartCoroutine(update);
    }


    IEnumerator UpdateCoroutine()
    {
        while (!GameManager.instance.stoppedGame)
        {
            remainingTime -= Time.deltaTime;
            timeElapsed += Time.deltaTime;
            DisplayTimer();


            if (remainingTime / gameTime < .1f && !playTickingSound)
                StartCoroutine(PlayTickingSound());
            else if (remainingTime < 0)
                EndGame();

            yield return null;
        }
        
    }

    IEnumerator PlayTickingSound()
    {
        playTickingSound = true;
        audioSource.Play();
        
        while (playTickingSound)
        {
            float volume = -(remainingTime / gameTime * 10) + 1;
            audioSource.volume = volume;
            yield return null;
        }
        
        audioSource.Stop();
    }

    void EndGame()
    {
        StopCoroutine(update);
        playTickingSound = false;
        GameManager.instance.StopGame();
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
        float minutes = Mathf.FloorToInt(timeElapsed / 60);
        float seconds = Mathf.FloorToInt(timeElapsed % 60);
        remainingTimeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

        float remainingTimeInPercentage = remainingTime / gameTime;
        sliderLeft.value = remainingTimeInPercentage;
        sliderRight.value = remainingTimeInPercentage;
        
        echsenMaterial.SetFloat("_Percentage", remainingTimeInPercentage);
    }

    private void OnDestroy()
    {
        echsenMaterial.SetFloat("_Percentage", 1);
        remainingTime = 0;
    }
}
