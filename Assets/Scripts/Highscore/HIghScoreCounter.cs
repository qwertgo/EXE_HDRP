using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

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
    private float inAirScoreDelay;

    private int inAirScore;
    private int driftDashScore;
    private int closeToObjectScore;
    private int closeToEnemyScore;
    private int multipleFirefliesScore;
    int tmpInAirScore = 0;

    private int currentScoreVisualIndex;
    private int currentNumberOfScoreVisuals;

    public enum ScoreType { InAir, DriftDash, CloseToObject, CloseToEnemy, MultipleFireflies }


    [Header("References")]
    [SerializeField] private TextMeshProUGUI extraScoreGUI;
    [SerializeField] private RectTransform mainUICanvas;
    [SerializeField] private RectTransform scoreTextBox;
    [SerializeField] private TextMeshProUGUI scoreVisualPrefab;
    [HideInInspector] public static HighScoreCounter instance;
    
    private GameTimer gameTimer;
    private TextMeshProUGUI[] scoreVisuals;
    private IEnumerator inAirScoreCounterCoroutine;


    private IEnumerator Start()
    {
        if(instance is not null)
            Destroy(instance);
        
        instance = this;

        SpawnScoreVisuals();

        yield return new WaitForEndOfFrame();

        gameTimer = GameVariables.instance.gameTimer;
        inAirScoreDelay = 1 / inAirScorePerSecond;
    }

    private void SpawnScoreVisuals()
    {
        int numberOfVisuals = 10;
        scoreVisuals = new TextMeshProUGUI[numberOfVisuals];

        for(int i = 0; i < numberOfVisuals; i++)
        {
            var currentVisual = Instantiate(scoreVisualPrefab, mainUICanvas);
            currentVisual.text = "";
            scoreVisuals[i] = currentVisual;
        }
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

        if (addedScore <= 0)
            return;

        additionalHighScore += addedScore;
        //additionalScoreGUI.text = additionalHighScore.ToString();
        SpawnScoreVisualAnimated(addedScore, scoreType);
    }

    private async void SpawnScoreVisualAnimated(int addedScore, ScoreType scoreType)
    {
        currentNumberOfScoreVisuals++;

        var visual = scoreVisuals[currentScoreVisualIndex];
        visual.text = $"{scoreType}\n+{addedScore}";
        currentScoreVisualIndex++;
        currentScoreVisualIndex %= scoreVisuals.Length;

        visual.rectTransform.position = scoreTextBox.position - new Vector3(0, currentNumberOfScoreVisuals * 100);

        Vector3 endPosition = scoreTextBox.position;
        await visual.rectTransform.DOMove(endPosition, 1f).SetEase(Ease.InExpo).AsyncWaitForCompletion();

        visual.text = "";
        currentNumberOfScoreVisuals--;

        extraScoreGUI.text = additionalHighScore.ToString();
        var extraScoreTransform = extraScoreGUI.rectTransform;
        await extraScoreTransform.DOPunchScale(Vector3.one * 2, 1f, 4).AsyncWaitForCompletion();

        if (currentNumberOfScoreVisuals <= 0 && extraScoreTransform.localScale != Vector3.one)
            extraScoreTransform.DOScale(1, .2f).SetEase(Ease.InOutSine);
    }

    private Vector2 GetRandomScreenPosition()
    {
        int x = Random.Range(-640, 640);
        x = Mathf.Max(Mathf.Abs(x), 160) * (int)Mathf.Sign(x);

        int y = Random.Range(-360, 360);
        y = Mathf.Max(Mathf.Abs(y), 90) * (int)Mathf.Sign(y);

        return new Vector2(x, y);
    }

    public float GetTotalScore()
    {
        //Debug.Log($"time: {gameTimer.timeElapsed}, additionale Score: {additionalHighScore}");
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

    public void StartInAirScore()
    {
        if (inAirScoreCounterCoroutine is not null)
        {
            StopCoroutine(inAirScoreCounterCoroutine);
            AddToScore(ScoreType.InAir, tmpInAirScore);
        }

        inAirScoreCounterCoroutine = InAirScoreCounter();
        StartCoroutine(inAirScoreCounterCoroutine);
    }

    

    private IEnumerator InAirScoreCounter()
    {
        playerIsInAir = true;
        tmpInAirScore = 0;

        yield return new WaitForSeconds(timeInAirToGetScore);

        if(!playerIsInAir)
            yield break;

        while (playerIsInAir)
        {
            tmpInAirScore++;
            yield return new WaitForSeconds(inAirScoreDelay);
        }

        AddToScore(ScoreType.InAir, tmpInAirScore);
        inAirScoreCounterCoroutine = null;
    }
}
