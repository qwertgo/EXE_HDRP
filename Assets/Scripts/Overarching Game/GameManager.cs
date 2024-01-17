using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour, PlayerInput.IGameManagerActions
{
    public static GameManager instance;
    [HideInInspector]
    public bool gameIsPaused;
    public string playerName = "Wow Echstrem";

    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject continueButton;
    [SerializeField] private HighScoreTable highScoreTable;

    private PlayerInput controls;
    private GameVariables gameVariables;
    private EventSystem eventSystem;

    private void Start()
    {
        if (instance)
            Destroy(this);
        else
            instance = this;

        if (controls == null)
        {
            controls = new PlayerInput();
            controls.Enable();
            controls.GameManager.SetCallbacks(this);
        }

        gameVariables = GameVariables.instance;
        eventSystem = EventSystem.current;
    }

    public void TooglePause()
    {
        if (gameIsPaused)
        {
            Time.timeScale = 1;
            gameIsPaused = false;
            pauseMenu.SetActive(false);
            gameVariables.isPaused = false;
            gameVariables.onUnpause.Invoke();
            eventSystem.SetSelectedGameObject(null);
        }
        else
        {
            Time.timeScale = 0;
            gameIsPaused = true;
            pauseMenu.SetActive(true);
            gameVariables.isPaused = true;
            gameVariables.onPause.Invoke();
            eventSystem.SetSelectedGameObject(continueButton);
        }
        
    }

    public void StopGame()
    {
        Time.timeScale = 0;
        
        HighScoreEntry newEntry = new HighScoreEntry(playerName, Time.time);
        highScoreTable.CreateHighScoreVisuals(newEntry);
    }

    public void OnPauseGame(InputAction.CallbackContext context)
    {
        if (context.started)
            TooglePause();
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }

    public void RestartLevel()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
