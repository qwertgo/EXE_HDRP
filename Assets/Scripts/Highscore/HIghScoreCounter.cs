using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class HighScoreCounter : MonoBehaviour
{
    [Header("Variables")]
    [SerializeField] private int inAirRewardValue;
    [SerializeField] private int driftDashRewardValue;
    [SerializeField] private int closeToObjectRewardValue;
    [SerializeField] private int CloseToEnemyRewardValue;
    [SerializeField] private int multipleFirefliesRewardValue;

    [SerializeField] private float timeInAirToGetScore;
    [SerializeField] private float inAirScorePerSecond;
    [HideInInspector] public bool playerIsInAir;
    private int additionalHighScore;
    private float inAirScoreTimeToWait;

    private int inAirScore;
    private int driftDashScore;
    private int closeToObjectScore;
    private int closeToEnemyScore;
    private int multipleFirefliesScore;

    public enum ScoreType { InAir, DriftDash, CloseToObject, CloseToEnemy, MultipleFireflies }


    [Header("References")]
    [SerializeField] private TextMeshProUGUI additionalScoreGUI;
    [HideInInspector] public static HighScoreCounter instance;
    
    private GameTimer gameTimer;
    

    private IEnumerator Start()
    {
        if(instance is not null)
            Destroy(instance);
        
        instance = this;

        yield return new WaitForEndOfFrame();

        gameTimer = GameVariables.instance.gameTimer;
        inAirScoreTimeToWait = 1 / inAirScorePerSecond;
    }

    private void OnDestroy()
    {
        instance = null;
    }

    public void AddToScore(ScoreType scoreType, float scaleFactor = 1)
    {
        int addedScore = 0;  

        switch (scoreType)
        {
            case ScoreType.InAir:
                addedScore = Mathf.RoundToInt(inAirRewardValue * scaleFactor);
                inAirScore += addedScore;
                break;

            case ScoreType.DriftDash:
                addedScore = Mathf.RoundToInt(driftDashRewardValue * scaleFactor);
                driftDashScore = addedScore;
                break;

            case ScoreType.CloseToObject:
                addedScore = Mathf.RoundToInt(closeToObjectRewardValue * scaleFactor);
                closeToObjectScore = addedScore;
                break;

            case ScoreType.CloseToEnemy:
                addedScore = Mathf.RoundToInt(CloseToEnemyRewardValue * scaleFactor);
                closeToEnemyScore = addedScore;
                break;

            case ScoreType.MultipleFireflies:
                addedScore = Mathf.RoundToInt(multipleFirefliesRewardValue * scaleFactor);
                multipleFirefliesScore = addedScore;
                break;
        }

        //Debug.Log($"Highscoretype: {scoreType}, Amount: {addedScore}");

        additionalHighScore += addedScore;
        additionalScoreGUI.text = additionalHighScore.ToString();
    }

    public float GetTotalScore()
    {
        Debug.Log($"time: {gameTimer.timeElapsed}, additionale Score: {additionalHighScore}");
        return gameTimer.timeElapsed + additionalHighScore;
    }

    public HighScoreEntry CreateHighscoreEntry(string name) 
        => new HighScoreEntry(name, gameTimer.timeElapsed, inAirScore, driftDashScore, closeToObjectScore, closeToEnemyScore, multipleFirefliesScore);

#region Multiple Fireflies Score
    public void StartMultipleFirfliesCounter()
    {
        // joo instantiate shit
    }

    public void StopMultipleFirefliesCounter(int collectedFireflies)
    {
        AddToScore(ScoreType.MultipleFireflies, collectedFireflies);
    }
#endregion

    public IEnumerator StartInAirScoreCounter()
    {
        playerIsInAir = true;
        int inAirScore = 0;

        yield return new WaitForSeconds(timeInAirToGetScore);

        if(!playerIsInAir)
            yield break;

        //TODO: spawn In Air UI

        while(playerIsInAir)
        {
            inAirScore++;
            yield return new WaitForSeconds(inAirScoreTimeToWait);
        }

        AddToScore(ScoreType.InAir, inAirScore);
    }
}
