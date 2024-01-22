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
    [SerializeField] private TextMeshProUGUI remainingTimeText;
    [SerializeField] private Slider sliderRight;
    [SerializeField] private Slider sliderLeft;
    [SerializeField] private Color sliderColorBase;
    [SerializeField] private Color sliderColorSpecial;

    private IEnumerator update;

    private void Start()
    {
        remainingTime = gameTime;
        update = UpdateCoroutine();
        StartCoroutine(update);
        sliderLeft.image.color = sliderColorBase;
        sliderRight.image.color = sliderColorBase;
    }

    IEnumerator UpdateCoroutine()
    {
        while (enabled)
        {
            remainingTime -= Time.deltaTime;
            DisplayTimer();

            if (remainingTime < 0)
                EndGame();

            yield return null;
        }
        
    }

    void EndGame()
    {
        StopCoroutine(update);
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
        float minutes = Mathf.FloorToInt(remainingTime / 60);
        float seconds = Mathf.FloorToInt(remainingTime % 60);
        remainingTimeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

        float remaingTimeInPercentage = remainingTime / gameTime;
        sliderLeft.value = remaingTimeInPercentage;
        sliderRight.value = remaingTimeInPercentage;
    }
}
