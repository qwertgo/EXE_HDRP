using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class HighScoreCounter : MonoBehaviour
{
    [Header("Variables")]
    [SerializeField] private int inAirScore;
    [SerializeField] private int driftDashScore;
    [SerializeField] private int closeToObjectScore;
    [SerializeField] private int CloseToEnemyScore;
    [SerializeField] private int multipleFirefliesScore;

    [SerializeField] private float timeInAirToGetScore;
    [SerializeField] private float inAirScorePerSecond;
    [HideInInspector] public bool playerIsInAir;
    private int additionalHighScore;
    private float inAirScoreTimeToWait;


    [Header("References")]
    [SerializeField] private TextMeshProUGUI additionalScoreGUI;
    [HideInInspector] public static HighScoreCounter instance;
    private GameTimer gameTimer;

    public enum ScoreType {InAir, DriftDash, CloseToObject, CloseToEnemy, MultipleFireflies}

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
        int addedScore = GetScoreFromType(scoreType, scaleFactor);
        Debug.Log($"Highscoretype: {scoreType}, Amount: {addedScore}");

        additionalHighScore += addedScore;
        additionalScoreGUI.text = additionalHighScore.ToString();
    }

    private int GetScoreFromType(ScoreType scoreType, float scaleFactor)
    {
        int tmpScore = 0;

        switch(scoreType)
        {
            case ScoreType.InAir:
                tmpScore = inAirScore;
                break;

            case ScoreType.DriftDash:
                tmpScore = driftDashScore;
                break;

            case ScoreType.CloseToObject:
                tmpScore = closeToObjectScore;
                break;

            case ScoreType.CloseToEnemy:
                tmpScore = CloseToEnemyScore;
                break;

            case ScoreType.MultipleFireflies:
                tmpScore = multipleFirefliesScore;
                break;
        }

        return Mathf.RoundToInt(tmpScore + scaleFactor);
    }

    public float GetTotalScore()
    {
        Debug.Log($"time: {gameTimer.timeElapsed}, additionale Score: {additionalHighScore}");
        return gameTimer.timeElapsed + additionalHighScore;
    }

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
