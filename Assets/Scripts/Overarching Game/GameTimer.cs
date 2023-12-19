using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Serialization;

public class GameTimer : MonoBehaviour
{
    [SerializeField] private float timeToMorning;
    private float remainingTime;
    [SerializeField] private TextMeshProUGUI remainingTimeText;
    [SerializeField] private TextMeshProUGUI youWonText;

    private IEnumerator Start()
    {
        GameVariables.instance.dayNightCycleController.gameTime = timeToMorning;
        remainingTime = timeToMorning;
        
        yield return new WaitForSeconds(timeToMorning);
        
        

        
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
    }
}
