using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameTimer : MonoBehaviour
{
    [SerializeField] private float timeToMorning;
    [SerializeField] private TextMeshProUGUI timeElapsedText;
    [SerializeField] private TextMeshProUGUI youWonText;

    private IEnumerator Start()
    {
        GameVariables.instance.dayNightCycleController.gameTime = timeToMorning;
        
        yield return new WaitForSeconds(timeToMorning);
        
        GameManager.instance.StopGame();
        youWonText.enabled = true;
        youWonText.gameObject.SetActive(true);

        switch (GameVariables.instance.player.fireflyCount)
        {
            case > 1:
                youWonText.text = "You gathered " + GameVariables.instance.player.fireflyCount + " fireflies";
                break;
            case 1:
                youWonText.text = "You gathered one firefly";
                break;
            default:
                youWonText.text = "You gathered no fireflies and died of hunger";
                break;
        }

        
    }

    void Update()
    {
        timeElapsedText.text = Mathf.Floor(Time.time / 60) + " : " + Mathf.Floor(Time.time % 60);
    }
}
