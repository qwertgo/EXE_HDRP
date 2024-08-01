using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using UnityEngine.Events;

public class HighScoreTable : MonoBehaviour
{
    [Header("Highscore")]
    [SerializeField] private Color selectedColor;
    [SerializeField] private Color notSelectedColor1;
    [SerializeField] private Color notSelectedColor2;
    
    [SerializeField] private GameObject highScoreTable;
    [SerializeField] private RectTransform highScorePanel;
    [SerializeField] private GameObject highScoreEntryVisualsPrefab;

    [Header("Time and Extra Score")]
    [SerializeField] private TextMeshProUGUI timeSurvivedGUI;
    [SerializeField] private TextMeshProUGUI timeScoreGUI;
    [SerializeField] private TextMeshProUGUI tmpExtraScoreGUI;
    [SerializeField] private TextMeshProUGUI totalExtraScoreGUI;
    [SerializeField] private TextMeshProUGUI totalScoreTextGUI;
    [SerializeField] private TextMeshProUGUI totalScoreNumberGUI;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    private int place;

    private List<HighScoreEntry> highScoreEntries = new();

    #region Score Animation
    public async void GameOverUIAnimation(HighScoreEntry newEntry)
    {
        int animationsDelay = 500;
        float animationDuration = .6f;

        restartButton.transform.localScale = Vector3.zero;
        mainMenuButton.transform.localScale = Vector3.zero;

        await Task.Delay(animationsDelay * 2);
        highScoreTable.gameObject.SetActive(true);

        //Show time survived
        GameVariables.instance.gameTimer.GetTimeElapsed(out int minutes, out int seconds);
        string timeSurvivedString = string.Format("{0:00}:{1:00}", minutes, seconds) + " minutes";
        await RevealTextBoxAnimated(timeSurvivedGUI, timeSurvivedString, animationDuration);
        
        //Show score for time survived
        int timeScore = (minutes * 60 + seconds) * 2;
        await CountToNumberAnimated(timeScoreGUI, 0, timeScore, animationDuration);
        await Task.Delay(animationsDelay);

        await RevealExtraAndFinalScore(timeScore, newEntry, animationDuration, animationsDelay);

        await restartButton.transform.DOScale(1, animationDuration).AsyncWaitForCompletion();
        await mainMenuButton.transform.DOScale(1, animationDuration).AsyncWaitForCompletion();

        restartButton.onClick.AddListener(() => GameManager.instance.RestartLevel(false));
        mainMenuButton.onClick.AddListener(() => GameManager.instance.RestartLevel(true));
    }

    private async Task RevealTextBoxAnimated(TextMeshProUGUI textBox, string newText, float duration)
    {
        textBox.text = newText;
        RectTransform textTransform = textBox.rectTransform;

        textTransform.localScale = Vector3.one * 3;
        textTransform.rotation = Quaternion.Euler(0, 0, 25);

        textTransform.DORotateQuaternion(Quaternion.identity, duration).SetEase(Ease.OutCubic);
        await textTransform.DOScale(1, duration).SetEase(Ease.InCubic).AsyncWaitForCompletion();


    }

    private async Task CountToNumberAnimated(TextMeshProUGUI textBox,int countFrom, int countTo, float duration)
    {
        int currentNumber = countFrom;
        var task = DOTween.To(() => currentNumber, x => currentNumber = x, countTo, duration);
        textBox.rectTransform.DOShakeScale(duration, .75f, 5, 0, true);

        while (task.active)
        {
            textBox.text = currentNumber.ToString();
            await Task.Yield();
        }
    }

    private async Task RevealExtraScoreCategory(int currentExtraScore, int addedExtraScore, string categoryString, float duration)
    {
        RevealTextBoxAnimated(tmpExtraScoreGUI, categoryString, duration);

        int totalExtraScore = currentExtraScore + addedExtraScore;
        await CountToNumberAnimated(totalExtraScoreGUI, currentExtraScore, totalExtraScore, duration);
    }

    private async Task RevealExtraAndFinalScore(int timeScore, HighScoreEntry newEntry, float animationDuration, int animationsDelay)
    {
        //show extra scores
        int tmpExtraScore = 0;
        int totalScore = timeScore;

        foreach (var pair in newEntry.allScores)
        {
            if (pair.Value <= 0)
                continue;

            string categoryText = pair.Key.ToReadableString() + "\n+" + pair.Value;
            await RevealExtraScoreCategory(tmpExtraScore, pair.Value, categoryText, animationDuration);
            await Task.Delay(animationsDelay);

            tmpExtraScore += pair.Value;
            totalScore = timeScore + tmpExtraScore;
        }

        if (tmpExtraScore <= 0)
            await RevealTextBoxAnimated(totalExtraScoreGUI, "0", animationDuration);

        totalScoreTextGUI.text = "Score:";
        await CountToNumberAnimated(totalScoreNumberGUI, 0, totalScore, animationDuration * 3);
    }

    #endregion

    #region Highscore Visuals
    //makes HighScoreTable visible, Adds New Entry, and creates visuals for all HighScoreEntries
    public void CreateHighScoreVisuals(HighScoreEntry newEntry = null)
    {
        //load existing HighScore, Add Entry and sort the Highscorelist
        highScoreEntries = SaveSystem.LoadHighScore();
        bool createdNewEntry = false;

        if (newEntry is not null)
        {
            highScoreEntries.Add(newEntry);
            highScoreEntries = BubbleSort();
            createdNewEntry = true;
        }
        else
            place = 1;

        //change panelScale to match size of entries
        Vector2 sizeDelta = highScorePanel.sizeDelta;
        highScorePanel.sizeDelta = new Vector2(sizeDelta.x, highScoreEntries.Count * 100);

        for(int i = 0; i < highScoreEntries.Count; i++)
        {
            AddHighScoreEntryVisuals(highScoreEntries[i], i, createdNewEntry);
        }

        if (createdNewEntry)
            highScorePanel.anchoredPosition = new Vector2(0, (place - 3) * 100);

        SaveSystem.SaveHighscore(highScoreEntries);
    }

    //Creates Visuals for given HighscoreEntry
    private void AddHighScoreEntryVisuals(HighScoreEntry entry, int i, bool createdNewEntry)
    {
        GameObject entryGameObject = Instantiate(highScoreEntryVisualsPrefab, Vector3.zero, Quaternion.identity, highScorePanel);

        TextMeshProUGUI[] entryTexts = entryGameObject.GetComponentsInChildren<TextMeshProUGUI>();
        entryTexts[0].text = $"{i + 1}. {entry.name}" ;
        entryTexts[1].text = entry.totalScore.ToString();

        Image image = entryGameObject.GetComponent<Image>();

        if (i + 1 == place && createdNewEntry)
            image.color = selectedColor;
        else
            image.color = i % 2 == 0 ? notSelectedColor1 : notSelectedColor2;
    }
    #endregion

    #region Highscorelist sorting
    private List<HighScoreEntry> BubbleSort()
    {
        place = highScoreEntries.Count;
        for(int i = highScoreEntries.Count -1; i > 0; i--)
        {
            int o = i - 1;
            if(highScoreEntries[i].totalScore >= highScoreEntries[o].totalScore)
            {
                highScoreEntries = Swap(i, o);
                place--;
            }
            else
                break;
        }

        return highScoreEntries;
    }

    List<HighScoreEntry> Swap(int i, int o)
    {
        HighScoreEntry entry1 = highScoreEntries[i];
        highScoreEntries[i] = highScoreEntries[o];
        highScoreEntries[o] = entry1;
        return highScoreEntries;
    }
    #endregion
}