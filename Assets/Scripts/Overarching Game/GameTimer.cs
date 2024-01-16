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
    [SerializeField] private HighScoreTable highScoreTable;

    private IEnumerator update;

    private void Start()
    {
        remainingTime = gameTime;
        update = UpdateCoroutine();
        StartCoroutine(update);
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
        youWonText.enabled = true;
        youWonText.gameObject.SetActive(true);

        HighScoreEntry newEntry = new HighScoreEntry("WOW ECHSTREM", Time.time);
        
        highScoreTable.CreateHighScoreVisuals(newEntry);
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
