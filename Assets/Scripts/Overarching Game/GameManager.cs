using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using TMPro;
using UnityEngine.Events;
using DG.Tweening;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;

public class GameManager : MonoBehaviour, PlayerInput.IGameManagerActions
{
    private static bool restartWithStartScreen = true;
    [SerializeField] private bool startWithStartScreen;
    [SerializeField] private bool spawnStartEnemy;
    [HideInInspector] public static string playerName = "";

    [HideInInspector] public bool gameIsPaused;
    [HideInInspector] public bool stoppedGame;
    [HideInInspector] public static GameManager instance;
    [HideInInspector] public static event Action gameOverEvent;

    private int restartButtonsPressed;
    private bool isInNameSelection;
    private bool canPauseGame = false;

    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject continueButton;
    [SerializeField] private HighScoreTable highScoreTable;
    [SerializeField] private GameObject highScoreRestartButton; 
    [SerializeField] private EnemyMovement startEnemy;
    [SerializeField] private EnemyManager enemyManagaer;
    
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

    [Header("Name Selection Menu")] 
    [SerializeField] private GameObject nameSelectionStartButton;
    [SerializeField] private TMP_InputField nameSelectionInputField;

    [Header("Activate At Game Start")] 
    [SerializeField] private Transform cameraLookAt;
    [SerializeField] private AudioSource playerWalkingAudioSource;
    [SerializeField] private SpeedOMeter speedOMeter;
    [SerializeField] private GameObject timeSlider;
    [SerializeField] private GameObject currentScore;
    [SerializeField] private DayNightCycleController dayNightCycleController;
    [SerializeField] private GameObject playerWaterVFX;
    [SerializeField] private GameTimer gameTimer;
    [SerializeField] private RectTransform turnTutorial;

    [Header("Deactivate on Game Over")]
    [SerializeField] private SphereCollider scoreCollider;
    [SerializeField] private FireflyManager fireflyManager;
    

    private PlayerInput controls;
    private GameVariables gameVariables;
    private HighScoreCounter highScoreCounter;
    private EventSystem eventSystem;
    private IEnumerator restartCoroutine;

    private void Start()
    {
        if (playerName.Equals(""))
            playerName = "Wow Echstrem";
            
        instance = this;

        if (controls == null)
        {
            controls = new PlayerInput();
            controls.Enable();
            controls.GameManager.SetCallbacks(this);
        }

        gameVariables = GameVariables.instance;
        highScoreCounter = gameVariables.highScoreCounter;
        eventSystem = EventSystem.current;

        if (!(startWithStartScreen && restartWithStartScreen))
        {
            ActivateObjectsToStartGame();
            Cursor.visible = false;
            if(spawnStartEnemy)
                startEnemy.gameObject.SetActive(true);
            
            return;
        }
        
        startMenu.SetActive(true);
        eventSystem.SetSelectedGameObject(startMenuPlayButton);
    }
    
    private void OnDestroy()
    {
        restartCoroutine = null;
        instance = null;
        controls.Disable();
        controls.GameManager.RemoveCallbacks(this);
    }
    
    #region menu
    public void EnterNameSelection()
    {
        startMenuMain.SetActive(false);
        nameSelection.SetActive(true);
        SelectUI(nameSelection);
        isInNameSelection = true;
    }
    
    public void StartGame(string playerName)
    {
        GameManager.playerName = playerName;
        isInNameSelection = false;

        Time.timeScale = 1;
        startMenu.SetActive(false);

        StartCoroutine(WaitAndStartRound());
    }

    public void StartGameViaButton()
    {
        StartGame(nameSelectionInputField.text);
    }

    IEnumerator WaitAndStartRound()
    {
        Cursor.visible = false;
        
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
        eventSystem.SetSelectedGameObject(null);
        startEnemy.gameObject.SetActive(spawnStartEnemy);
        
    }

    private void ActivateObjectsToStartGame()
    {
        gameVariables.player.enabled = true;
        dayNightCycleController.enabled = true;
        speedOMeter.enabled = true;
        gameTimer.enabled = true;
        
        timeSlider.SetActive(true);
        currentScore.SetActive(true);
        playerWaterVFX.SetActive(false);
        
        playerWalkingAudioSource.Play();
        cameraLookAt.position = new Vector3(0, .4f, 0);
        canPauseGame = true;

        StartCoroutine(ActivateTurnTutorial());
    }

    private IEnumerator ActivateTurnTutorial()
    {
        turnTutorial.gameObject.SetActive(true);

        float xSize = 350;
        float ySize = turnTutorial.sizeDelta.y;

        var tween = DOTween.To(() => xSize, x => xSize = x, 500, .6f).SetEase(Ease.InOutQuad).SetLoops(12, LoopType.Yoyo).SetUpdate(true);

        while (tween.active)
        {
            turnTutorial.sizeDelta = new Vector2(xSize, ySize);
            yield return null;
        }

        turnTutorial.gameObject.SetActive(false);
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
        startMenuMain.SetActive(true);
        creditsMenu.SetActive(false);
        controlsMenu.SetActive(false);
        nameSelection.SetActive(false);
        isInNameSelection = false;
        
        SelectUI(startMenuPlayButton);
    }

    public void EnterHighScore()
    {
        startMenuMain.SetActive(false);
        highScoreTable.CreateHighScoreVisuals();
        SelectUI(highScoreRestartButton);
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
            gameVariables.player.enabled = true;
            gameVariables.isPaused = false;
            gameVariables.onUnpause.Invoke();
            eventSystem.SetSelectedGameObject(null);
            Cursor.visible = false;
        }
        else
        {
            Time.timeScale = 0;
            gameIsPaused = true;
            pauseMenu.SetActive(true);
            gameVariables.isPaused = true;
            gameVariables.onPause.Invoke();
            gameVariables.player.enabled = false;
            SelectUI(continueButton);
            Cursor.visible = true;
        }
        
    }

    public void GameOver()
    {
        if(stoppedGame)
            return;

        stoppedGame = true;

        gameOverEvent.Invoke();
        
        gameVariables.player.Die();
        canPauseGame = false;
        Cursor.visible = true;
        timeSlider.SetActive(false);
        currentScore.SetActive(false);
        gameTimer.playTickingSound = false;
        highScoreCounter.enabled = false;
        scoreCollider.enabled = false;
        fireflyManager.enabled = false;

        HighScoreEntry newEntry = highScoreCounter.CreateHighscoreEntry(playerName);
        highScoreTable.CreateHighScoreVisuals(newEntry);
        //highScoreTable.StartCoroutine(highScoreTable.CreateScoreVisualsAnimated(newEntry));
        highScoreTable.GameOverUIAnimation(newEntry);
        SelectUI(highScoreRestartButton);
    }

    public void OnPauseGame(InputAction.CallbackContext context)
    {
        if (context.started && canPauseGame)
            TooglePause();
    }

    public void OnEnter(InputAction.CallbackContext context)
    {
        if (isInNameSelection && context.started)
        {
            StartGameViaButton();
        }
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }

    public void RestartLevel(bool completeRestart)
    {
        restartWithStartScreen = completeRestart;

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
        {
            if (restartCoroutine == null)
            {
                restartCoroutine = CheckForAllRestartButtons();
                StartCoroutine(restartCoroutine);
            }
            else
            {
                restartButtonsPressed++;
            }
        }
    }

    IEnumerator CheckForAllRestartButtons()
    {
        restartButtonsPressed++;
        while (restartButtonsPressed > 0)
        {
            if (restartButtonsPressed >= 4)
            {
                yield return new WaitForSeconds(1f);
                if(restartButtonsPressed >=4)
                    RestartLevel(false);
            }

            yield return null;
        }
        
        restartCoroutine = null;
    }

    

    #endregion
    
}
