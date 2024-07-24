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
    private int additionalHighScore;
    private int recentlyCollectedFireflies;


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
    }

    private void OnDestroy()
    {
        instance = null;
    }

    public void AddToHighscore(ScoreType scoreType, float scaleFactor = 1)
    {
        int addedScore = GetScoreFromType(scoreType, scaleFactor);
        Debug.Log($"Highscoretype: {scoreType}, Amount: {addedScore}");

        additionalHighScore += addedScore;
        additionalScoreGUI.text = additionalHighScore.ToString();
    }

    public IEnumerator StartFireflyCounter() 
    {
        // if(recentlyCollectedFireflies == 1)
        //     Debug.Log("Spawn Visuals for multiple Fireflies");
        
        recentlyCollectedFireflies++;
        int tmpFireFliesCollected = recentlyCollectedFireflies;

        yield return new WaitForSeconds(1f);

        if(recentlyCollectedFireflies > 1 && recentlyCollectedFireflies == tmpFireFliesCollected)
        {
            AddToHighscore(ScoreType.MultipleFireflies, recentlyCollectedFireflies);
            recentlyCollectedFireflies = 0;

        }
    }

    public IEnumerator StartInAirCounter()
    {
        yield return new WaitForSeconds(1f);
    }

    private int GetScoreFromType(ScoreType scoreType, float scaleFactor)
    {
        switch(scoreType)
        {
            case ScoreType.InAir:
                return inAirScore;

            case ScoreType.DriftDash:
                return Mathf.RoundToInt(driftDashScore * scaleFactor);

            case ScoreType.CloseToObject:
                return Mathf.RoundToInt(closeToObjectScore * scaleFactor);

            case ScoreType.CloseToEnemy:
                return Mathf.RoundToInt(CloseToEnemyScore * scaleFactor);

            case ScoreType.MultipleFireflies:
                return Mathf.RoundToInt(multipleFirefliesScore * scaleFactor);

            default:
                return 0;
        }
    }

    public float GetTotalHighscore()
    {
        Debug.Log($"time: {gameTimer.timeElapsed}, additionale Score: {additionalHighScore}");
        return gameTimer.timeElapsed + additionalHighScore;
    }
}
