using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class HighScoreCounter : MonoBehaviour
{
    public static HighScoreCounter instance;
    private GameTimer gameTimer;
    private int additonalHighScore;

    [SerializeField] private int InAirScore;
    [SerializeField] private int DriftDashScore;
    [SerializeField] private int CloseToObjectScore;
    [SerializeField] private int CloseToEnemyScore;
    [SerializeField] private int MultipleFirefliesScore;

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

    public void AddToHighscore(ScoreType scoreType)
    {
        int value = GetSCoreFromType(scoreType);

        additonalHighScore += value;
    }

    private int GetSCoreFromType(ScoreType scoreType)
    {
        switch(scoreType)
        {
            case ScoreType.InAir:
                return InAirScore;

            case ScoreType.DriftDash:
                return DriftDashScore;

            case ScoreType.CloseToObject:
                return CloseToObjectScore;

            case ScoreType.CloseToEnemy:
                return CloseToEnemyScore;

            case ScoreType.MultipleFireflies:
                return MultipleFirefliesScore;

            default:
                return 0;
        }
    }

    public float GetTotalHighscore()
    {
        Debug.Log($"time: {gameTimer.timeElapsed}, additionale Score: {additonalHighScore}");
        return gameTimer.timeElapsed + additonalHighScore;
    }
}
