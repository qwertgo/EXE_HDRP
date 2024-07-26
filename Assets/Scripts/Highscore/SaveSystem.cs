using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using static HighScoreCounter;

public static class SaveSystem
{
   private static string dataPath = Application.persistentDataPath + "HuntGameHighScore.bin";
   
   public static void SaveHighscore(List<HighScoreEntry> highscoreList)
   {
      BinaryFormatter formatter = new BinaryFormatter();

      FileStream stream = new FileStream(dataPath, FileMode.Create);
      formatter.Serialize(stream, highscoreList);

      stream.Close();
   }
   
   public static List<HighScoreEntry> LoadHighScore()
   {
      if(!File.Exists(dataPath))
         return new List<HighScoreEntry>();


      BinaryFormatter formatter = new BinaryFormatter();
      FileStream stream = new FileStream(dataPath, FileMode.Open);

      List<HighScoreEntry> list = formatter.Deserialize(stream) as List<HighScoreEntry>;
      stream.Close();

      return list;
   }
   
}

[System.Serializable]
public class HighScoreEntry
{
    public string name { get; private set; }
    public float timeSurvived { get; private set; }
    public int timeScore { get; private set; }
    public int totalScore { get; private set; }
    public Dictionary<ScoreType, int> allScores { get; private set; } = new();



    public HighScoreEntry(string name, float timeSurvived, int inAirScore, int driftDashScore, int closeToObjectScore, int closeToEnemyScore, int multipleFirefliesScore)
    {
        this.name = name;
        this.timeSurvived = timeSurvived;     

        allScores.Add(ScoreType.InAir, inAirScore);
        allScores.Add(ScoreType.DriftDash, driftDashScore);
        allScores.Add(ScoreType.CloseToObject, closeToObjectScore);
        allScores.Add(ScoreType.CloseToEnemy, closeToEnemyScore);
        allScores.Add(ScoreType.MultipleFireflies, multipleFirefliesScore);

       
        timeScore = Mathf.FloorToInt(timeSurvived) * 2;
        totalScore = timeScore + inAirScore + driftDashScore + closeToObjectScore + closeToEnemyScore + multipleFirefliesScore;
    }



    public new string ToString()
    {
        return $"name: {name}, timeSurvived: {timeSurvived}";
    }
   
   
}
