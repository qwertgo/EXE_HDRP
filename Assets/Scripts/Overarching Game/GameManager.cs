using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class GameManager : MonoBehaviour, PlayerInput.IGameManagerActions
{
    public static GameManager instance;
    [HideInInspector] public bool gameIsPaused;
    public string playerName = "Wow Echstrem";
    [SerializeField] private bool startWithStartScreen;
    [SerializeField] private bool spawnStartEnemy;

    private int restartButtonsPressed;
    private bool stoppedGame;

    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject continueButton;
    [SerializeField] private HighScoreTable highScoreTable;
    [SerializeField] private GameObject highScoreRestartButton; 
    [SerializeField] private EnemyMovement startEnemy;
    
    [Header("Start Menu")]
    [SerializeField] private GameObject startMenu;
    [SerializeField] private GameObject startMenuPlayButton;
    [SerializeField] private GameObject startMenuMain;
    [FormerlySerializedAs("startMenuName")] [SerializeField] private GameObject nameSelection;

    [Header("Controls Menu")] 
    [SerializeField] private GameObject controlsMenu;
    [SerializeField] private GameObject controlsBackButton;

    [Header("Credits Menu")]
    [SerializeField] private GameObject creditsMenu;
    [SerializeField] private GameObject creditsBackButton;

    [Header("Activate At Game Start")] 
    [SerializeField] private Transform cameraLookAt;
    [SerializeField] private AudioSource playerWalkingAudioSource;
    [SerializeField] private SpeedOMeter speedOMeter;
    [SerializeField] private GameObject timeSlider;
    [SerializeField] private DayNightCycleController dayNightCycleController;
    [SerializeField] private GameObject playerWaterVFX;
    

    private PlayerInput controls;
    private GameVariables gameVariables;
    private EventSystem eventSystem;
    private IEnumerator restartCoroutine;

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


        if (!startWithStartScreen)
        {
            ActivateObjectsToStartGame();
            if(spawnStartEnemy)
                startEnemy.gameObject.SetActive(true);
            
            return;
        }
        
        startMenu.SetActive(true);
        eventSystem.SetSelectedGameObject(startMenuPlayButton);
    }

    #region menu
    public void EnterNameSelection()
    {
        startMenuMain.SetActive(false);
        nameSelection.SetActive(true);
        SelectUI(nameSelection);
    }
    
    public void StartGame(string playerName)
    {
        this.playerName = playerName;

        Time.timeScale = 1;
        startMenu.SetActive(false);
        eventSystem.SetSelectedGameObject(null);

        StartCoroutine(WaitAndStartRound());
    }

    IEnumerator WaitAndStartRound()
    {
        float t = 0;
        float speed = 1f / 2;
        Vector3 startPosition = cameraLookAt.position;
        Vector3 endPosition = new Vector3(0, .4f, 0);

        while (t <= 1)
        {
            t += Time.deltaTime * speed;
            cameraLookAt.position = Vector3.Lerp(startPosition, endPosition, Mathf.SmoothStep(0, 1, t));
            yield return null;
        }

        yield return new WaitForSeconds(.5f);
        ActivateObjectsToStartGame();
        
        if(spawnStartEnemy)
            startEnemy.gameObject.SetActive(true);
    }

    private void ActivateObjectsToStartGame()
    {
        gameVariables.player.enabled = true;
        dayNightCycleController.enabled = true;
        speedOMeter.enabled = true;
        
        timeSlider.SetActive(true);
        playerWaterVFX.SetActive(false);
        
        playerWalkingAudioSource.Play();
        cameraLookAt.position = new Vector3(0, .4f, 0);
    }

    public void EnterControlsMenu()
    {
        startMenuMain.SetActive(false);
        controlsMenu.SetActive(true);
        
        SelectUI(controlsBackButton);
    }

    public void EnterCreditsMenu()
    {
        startMenuMain.SetActive(false);
        creditsMenu.SetActive(true);
        
        SelectUI(creditsBackButton);
    }

    public void EnterMainMenu()
    {
        creditsMenu.SetActive(false);
        controlsMenu.SetActive(false);
        startMenuMain.SetActive(true);
        
        SelectUI(startMenuPlayButton);
    }

    private void SelectUI(GameObject o)
    {
        eventSystem.SetSelectedGameObject(null);
        eventSystem.SetSelectedGameObject(o);
    }
    
    #endregion

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
            eventSystem.SetSelectedGameObject(null);
            eventSystem.SetSelectedGameObject(continueButton);
        }
        
    }
    

    public void StopGame()
    {
        if(stoppedGame)
            return;

        stoppedGame = true;
        
        Time.timeScale = 0;
        GameVariables.instance.player.Die();
        
        HighScoreEntry newEntry = new HighScoreEntry(playerName, Time.time);
        highScoreTable.CreateHighScoreVisuals(newEntry);
        
        eventSystem.SetSelectedGameObject(null);
        eventSystem.SetSelectedGameObject(highScoreRestartButton);
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
    
    #region restart
    public void OnRestart_1(InputAction.CallbackContext context)
    {
        RestartButtonAction(context);
    }

    public void OnRestart_2(InputAction.CallbackContext context)
    {
        RestartButtonAction(context);
    }

    public void OnRestart_3(InputAction.CallbackContext context)
    {
        RestartButtonAction(context);
    }

    public void OnRestart_4(InputAction.CallbackContext context)
    {
        RestartButtonAction(context);
    }

    private void RestartButtonAction(InputAction.CallbackContext context)
    {
        if (context.canceled)
        {
            restartButtonsPressed--;
            return;
        }
        if (!context.started)
            return;
        
        restartButtonsPressed++;
        
        if (restartCoroutine != null)
            return;
        
        restartCoroutine = CheckForAllRestartButtons();
        StartCoroutine(restartCoroutine);
    }

    IEnumerator CheckForAllRestartButtons()
    {
        while (restartButtonsPressed > 0)
        {
            if(restartButtonsPressed >= 4)
                RestartLevel();

            yield return null;
        }

        restartCoroutine = null;
    }
    
    #endregion
    
}
