using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

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
    [SerializeField] private TextMeshProUGUI timeSurvivedDisplayGUI;
    [SerializeField] private TextMeshProUGUI timeScoreGUI;
    [SerializeField] private TextMeshProUGUI extraScoresDisplayGUI;
    [SerializeField] private TextMeshProUGUI totalExtraScoreGUI;
    [SerializeField] private TextMeshProUGUI totalScoreGUI;

    private int place;

    private List<HighScoreEntry> highScoreEntries = new();


    public IEnumerator CreateScoreVisualsAnimated(HighScoreEntry highScoreEntry)
    {
        float waitingTime = 1f;
        int totalScore = 0;


        //Show time survived
        GameVariables.instance.gameTimer.GetTimeElapsed(out int minutes, out int seconds);
        timeSurvivedDisplayGUI.text = string.Format("{0:00}:{1:00}", minutes, seconds) + " minutes";

        yield return new WaitForSecondsRealtime(waitingTime);

        //Show score for time
        int timeScore = (minutes * 60 + seconds) * 2;
        timeScoreGUI.text = timeScore.ToString();

        totalScore += timeScore;
        totalScoreGUI.text = $"Score: {totalScore}";

        yield return new WaitForSecondsRealtime(waitingTime);

        //show extra score 
        int tmpExtraScore = 0;

        foreach(var pair in highScoreEntry.allScores)
        {
            if (pair.Value <= 0)
                continue;


            tmpExtraScore += pair.Value;
            
            extraScoresDisplayGUI.text = pair.Key + "\n+" + pair.Value;
            totalExtraScoreGUI.text = tmpExtraScore.ToString();

            totalScore += pair.Value;
            totalScoreGUI.text = $"Score: {totalScore}";

            yield return new WaitForSecondsRealtime(waitingTime);
        }

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