using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class HighScoreCounter : MonoBehaviour
{
    public static HighScoreCounter instance;
    private GameTimer gameTimer;
    private int additionalHighScore;

    [Header("Variables")]

    [SerializeField] private int inAirScore;
    [SerializeField] private int driftDashScore;
    [SerializeField] private int closeToObjectScore;
    [SerializeField] private int CloseToEnemyScore;
    [SerializeField] private int multipleFirefliesScore;

    [Header("References")]
    [SerializeField] private TextMeshProUGUI additionalScoreGUI;

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
