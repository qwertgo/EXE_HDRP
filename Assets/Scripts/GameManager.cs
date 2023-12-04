using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public bool gameIsPaused;

    private void Start()
    {
        if (instance)
            Destroy(this);
        else
            instance = this;
    }

    public void TooglePause()
    {
        if (gameIsPaused)
        {
            Time.timeScale = 1;
            gameIsPaused = false;
        }
        else
        {
            Time.timeScale = 0;
            gameIsPaused = true;
        }
        
    }
}
