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
    [SerializeField] private TextMeshProUGUI youWonText;
    [SerializeField] private Slider sliderRight;
    [SerializeField] private Slider sliderLeft;

    private void Start()
    {
        remainingTime = gameTime;
    }

    void Update()
    {
        remainingTime -= Time.deltaTime;
        DisplayTimer();

        if (remainingTime < 0)
        {
            EndGame();
        }
    }

    void EndGame()
    {
        GameManager.instance.StopGame();
        youWonText.enabled = true;
        youWonText.gameObject.SetActive(true);

        float survivedMinutes = Mathf.FloorToInt(Time.time / 60);
        float survivedSeconds = Mathf.RoundToInt(Time.time % 60);
        
        string finalText = "You survived " + string.Format("{0:00}:{1:00}", survivedMinutes, survivedSeconds) + " ";
        finalText += "and gathered " + GameVariables.instance.fireflyCount + " fireflies";
        youWonText.text = finalText;
    }

    public void AddToTimer(float addedTime)
    {
        remainingTime += addedTime;
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
