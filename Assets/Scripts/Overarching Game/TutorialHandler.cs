using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static PlayerInput;

public class TutorialHandler : MonoBehaviour
{
    [SerializeField] private RectTransform turnTutorial;
    [SerializeField] private GameObject jumpTutorial;
    [SerializeField] private DriftPointContainer rightDriftPointContainer;
    [SerializeField] private DriftPointContainer leftDriftPointContainer;
    [SerializeField] private DriftTutorialUI driftTutorialUI;
    [SerializeField] private Transform UICanvas;
    [SerializeField] private Sprite leftDriftTutorialSprite;
    [SerializeField] private Sprite rightDriftTutorialSprite;
    [SerializeField] private Color leftColor;
    [SerializeField] private Color rightColor;
    public PlayerInput controls;
    private PlayerController player => GameVariables.instance.player;

    private bool isHoldingTriggerLeft;
    private bool isHoldingTriggerRight;

    private DriftPoint rightDriftPoint;
    private DriftPoint leftDriftPoint;
    private Camera cam;

    private DriftTutorialUI leftPointUI;
    private DriftTutorialUI rightPointUI;
    private RectTransform leftPointTransform;
    private RectTransform rightPointTransform;
    private Slider leftPointSlider;
    private Slider rightPointSlider;

    private void Awake()
    {
        GameManager.gameOverEvent += Disable;
    }


    public void StartTutorials()
    {
        PlayerPrefs.SetInt("PlayedTutorial", 1);
        Debug.Log(PlayerPrefs.GetInt("PlayedTutorial"));
        cam = Camera.main;
        controls = new PlayerInput();
        controls.Enable();


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

        StartCoroutine(WaitForDriftTutorialStart());
    }


    private IEnumerator WaitForDriftTutorialStart()
    {
        yield return new WaitUntil(() => CanStartDriftTutorial());

        yield return ChangeTimeScaleOverTime(0, .2f, Ease.OutSine);

        StartDriftTutorial();
    }

    private IEnumerator ChangeTimeScaleOverTime(float changeTo, float duration, Ease easinType)
    {
        var tween = DOTween.To(() => changeTo, x => changeTo = x, 0, duration).SetEase(easinType).SetUpdate(true);

        while (tween.active)
        {
            Time.timeScale = changeTo;
            yield return null;
        }
    }

    private bool CanStartDriftTutorial()
    {
        bool hasDriftPointLeft = leftDriftPointContainer.HasDriftPoint(out leftDriftPoint);
        bool hasDriftPointRight = rightDriftPointContainer.HasDriftPoint(out rightDriftPoint);
        bool isSamePoint = leftDriftPoint == rightDriftPoint;
        return hasDriftPointLeft && hasDriftPointRight && leftDriftPoint.isShown && rightDriftPoint.isShown && !isSamePoint;
    }

    private void StartDriftTutorial()
    {
        player.tutorialDriftLock = true;
        Time.timeScale = 0;

        Vector2 leftPointUIPosition = cam.WorldToScreenPoint(leftDriftPoint.transform.position);
        Vector2 rightPointUIPosition = cam.WorldToScreenPoint(rightDriftPoint.transform.position);

        leftPointUI = Instantiate(driftTutorialUI, UICanvas);
        rightPointUI = Instantiate(driftTutorialUI, UICanvas);

        leftPointTransform = leftPointUI.rectTransform;
        rightPointTransform = rightPointUI.rectTransform;

        Vector2 yOffset = new Vector2(0, 100);
        leftPointTransform.position = leftPointUIPosition + yOffset;
        rightPointTransform.position = rightPointUIPosition + yOffset;

        leftPointUI.image.sprite = leftDriftTutorialSprite;
        rightPointUI.image.sprite = rightDriftTutorialSprite;

        leftPointUI.image.color = leftColor;
        rightPointUI.image.color = rightColor;

        leftPointUI.textBox.color = leftColor;
        rightPointUI.textBox.color = rightColor;

        leftPointSlider = leftPointUI.slider;
        rightPointSlider = rightPointUI.slider;

        controls.P_Controls.LeftDrift.started += OnLeftDrift;
        controls.P_Controls.LeftDrift.canceled += OnLeftDrift;

        controls.P_Controls.RightDrift.started += OnRightDrift;
        controls.P_Controls.RightDrift.canceled += OnRightDrift;
    }

    private IEnumerator IsHoldingDriftButtonChck(bool rightButton)
    {
        float t = 0;
        var tween = DOTween.To(() => t, x => t = x, 1, .25f).SetUpdate(true).SetEase(Ease.Linear);

        Slider slider = rightButton ? rightPointSlider : leftPointSlider;
        slider.gameObject.SetActive(true);

        while(tween.active && isHoldingTrigger(rightButton))
        {
            slider.value = t;
            if (t >= 1)
                break;
            yield return null;
        }

        if (t < 1)
        {
            slider.value = 0;
            slider.gameObject.SetActive(false);
            tween.Kill();
            yield break;
        }

        CancelDriftSubscriptions();

        leftPointTransform.gameObject.SetActive(false);
        rightPointTransform.gameObject.SetActive(false);

        yield return ChangeTimeScaleOverTime(1, .25f, Ease.OutSine);
        
        Time.timeScale = 1f;
        player.tutorialDriftLock = false;
        player.CheckIfCanStartDrifting(rightButton);

        yield return JumpTutorial();
    }

    private IEnumerator JumpTutorial()
    {
        yield return new WaitForSecondsRealtime(10f);
        
        jumpTutorial.SetActive(true);
        yield return new WaitForSecondsRealtime(7f);

        jumpTutorial.SetActive(false);

    }

    private bool isHoldingTrigger(bool rightButton)
    {
        return rightButton ? isHoldingTriggerRight : isHoldingTriggerLeft;

    }

    private void OnLeftDrift(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            
            isHoldingTriggerLeft = true;
            StartCoroutine(IsHoldingDriftButtonChck(false));
        }
        else if (context.canceled)
        {
            isHoldingTriggerLeft = false;
        }
    }

    private void OnRightDrift(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            isHoldingTriggerRight = true;
            StartCoroutine(IsHoldingDriftButtonChck(true));
        }
        else if (context.canceled)
        {
            isHoldingTriggerRight = false;
        }
    }

    private void CancelDriftSubscriptions()
    {
        controls.P_Controls.LeftDrift.started -= OnLeftDrift;
        controls.P_Controls.LeftDrift.canceled -= OnLeftDrift;

        controls.P_Controls.RightDrift.started -= OnRightDrift;
        controls.P_Controls.RightDrift.canceled -= OnRightDrift;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        DisableAllUI();
        GameManager.gameOverEvent -= Disable;

        if (controls is null)
            return;

        CancelDriftSubscriptions();
        controls.Disable();
    }

    private void Disable()
    {
        gameObject.SetActive(false);
    }

    private void DisableAllUI()
    {
        if(leftPointUI is not null)
            leftPointUI?.gameObject.SetActive(false);
        if(rightPointUI is not null)
            rightPointUI?.gameObject.SetActive(false);

        turnTutorial.gameObject.SetActive(false);
        jumpTutorial.gameObject.SetActive(false);
    }


}
