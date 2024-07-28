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
    [SerializeField] private TextMeshProUGUI totalScoreGUI;

    private int place;

    private List<HighScoreEntry> highScoreEntries = new();

    public async void AsyncTest(HighScoreEntry newEntry)
    {
        int waitBetweenAnimations = 500;
        float animationDuration = .7f;

        List<Func<Task>> uiAnimations = new List<Func<Task>> { () => Task.Delay(waitBetweenAnimations) };

        //Show time survived
        GameVariables.instance.gameTimer.GetTimeElapsed(out int minutes, out int seconds);
        string timeSurvivedString = string.Format("{0:00}:{1:00}", minutes, seconds) + " minutes";
        uiAnimations.Add(() => RevealTextBoxAnimated(timeSurvivedGUI, timeSurvivedString, animationDuration));
        
        //Show score for time survived
        int timeScore = (minutes * 60 + seconds) * 2;
        uiAnimations.Add(() => CountToNumberAnimated(timeScoreGUI, 0, timeScore, animationDuration));
        uiAnimations.Add(() => Task.Delay(waitBetweenAnimations));

        //show extra scores
        int tmpExtraScore = 0;
        int totalScore = timeScore;

        foreach (var pair in newEntry.allScores)
        {
            if (pair.Value <= 0)
                continue;

            string categoryText = pair.Key + "\n+" + pair.Value;
            uiAnimations.Add(() => RevealExtraScoreCategory(tmpExtraScore, pair.Value, categoryText, animationDuration));
            uiAnimations.Add(() => Task.Delay(waitBetweenAnimations));

            tmpExtraScore += pair.Value;
            totalScore = timeScore + tmpExtraScore;
        }

        if (tmpExtraScore <= 0)
            uiAnimations.Add(() => RevealTextBoxAnimated(totalExtraScoreGUI, "0", animationDuration));

        uiAnimations.Add(() => CountToNumberAnimated(totalScoreGUI, 0, totalScore, animationDuration * 3, "Score: "));

        //play saved animations sequentualy
        foreach(var animation in uiAnimations)
            await animation();
    }

    private async Task RevealTextBoxAnimated(TextMeshProUGUI textBox, string newText, float duration)
    {
        textBox.text = newText;
        RectTransform textTransform = textBox.rectTransform;

        textTransform.localScale *= 3;
        textTransform.rotation = Quaternion.Euler(0, 0, 25);

        textTransform.DORotateQuaternion(Quaternion.identity, duration).SetEase(Ease.OutCubic);
        await textTransform.DOScale(1, duration).SetEase(Ease.InCubic).AsyncWaitForCompletion();


    }

    private async Task CountToNumberAnimated(TextMeshProUGUI textBox,int countFrom, int countTo, float duration, string textAddition = "")
    {
        int currentNumber = countFrom;
        var task = DOTween.To(() => currentNumber, x => currentNumber = x, countTo, duration);
        textBox.rectTransform.DOShakeScale(duration, 1, 10, 0, true);

        while (task.active)
        {
            textBox.text = textAddition + currentNumber;
            await Task.Yield();
        }
    }

    private async Task RevealExtraScoreCategory(int currentExtraScore, int addedExtraScore, string categoryString, float duration)
    {
        RevealTextBoxAnimated(tmpExtraScoreGUI, categoryString, duration);

        int totalExtraScore = currentExtraScore + addedExtraScore;
        await CountToNumberAnimated(totalExtraScoreGUI, currentExtraScore, totalExtraScore, duration);
    }


    public IEnumerator CreateScoreVisualsAnimated(HighScoreEntry highScoreEntry)
    {
        float waitingTime = 1f;
        int totalScore = 0;


        //Show time survived
        GameVariables.instance.gameTimer.GetTimeElapsed(out int minutes, out int seconds);
        timeSurvivedGUI.text = string.Format("{0:00}:{1:00}", minutes, seconds) + " minutes";

        timeSurvivedGUI.rectTransform.DOShakeRotation(waitingTime, 10, 10);

        yield return new WaitForSecondsRealtime(waitingTime);

        //Show score for time
        int timeScore = (minutes * 60 + seconds) * 2 + 20;
        //timeScoreGUI.text = timeScore.ToString();

        int test = 0;
        var tween = DOTween.To(()=> test, x => test = x, timeScore, 1f);

        while (tween.active)
        {
            timeScoreGUI.text = test.ToString();
            yield return null;
        }

        totalScore += timeScore;

        yield return new WaitForSecondsRealtime(waitingTime);

        //show extra score 
        int tmpExtraScore = 0;

        foreach(var pair in highScoreEntry.allScores)
        {
            if (pair.Value <= 0)
                continue;


            tmpExtraScore += pair.Value;
            
            tmpExtraScoreGUI.text = pair.Key + "\n+" + pair.Value;
            totalExtraScoreGUI.text = tmpExtraScore.ToString();

            totalScore += pair.Value;
            

            yield return new WaitForSecondsRealtime(waitingTime);
        }

        //show final score
        totalScoreGUI.text = $"Score: {totalScore}";


        totalExtraScoreGUI.text = tmpExtraScore.ToString();
    }


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
        
        highScoreTable.gameObject.SetActive(true);
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
    
    private List<HighScoreEntry> BubbleSort()
    {
        place = highScoreEntries.Count;
        for(int i = highScoreEntries.Count -1; i > 0; i--)
        {
            int o = i - 1;
            if(highScoreEntries[i].timeSurvived >= highScoreEntries[o].timeSurvived)
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
}