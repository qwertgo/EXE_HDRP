using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

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
      if (File.Exists(dataPath))
      {
         BinaryFormatter formatter = new BinaryFormatter();
         FileStream stream = new FileStream(dataPath, FileMode.Open);

         List<HighScoreEntry> list = formatter.Deserialize(stream) as List<HighScoreEntry>;
         stream.Close();

         return list;
      }
      else
      {
         List<HighScoreEntry> list = new List<HighScoreEntry>();
         list.Add(new HighScoreEntry("SAMU", 1200));
         
         return list;
      }
   }
   
}

[System.Serializable]
public class HighScoreEntry
{
   public HighScoreEntry(string name, float timeSurvived)
   {
      this.name = name;
      this.timeSurvived = timeSurvived;
   }

   public new string ToString()
   {
      return $"name: {name}, timeSurvived: {timeSurvived}";
   }
   
   public string name { get; private set; }
   public float timeSurvived { get; private set; }
}
