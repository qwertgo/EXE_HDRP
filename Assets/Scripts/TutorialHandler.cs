using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static PlayerInput;

public class TutorialHandler : MonoBehaviour
{
    [SerializeField] private RectTransform turnTutorial;
    [SerializeField] private DriftPointContainer rightDriftPointContainer;
    [SerializeField] private DriftPointContainer leftDriftPointContainer;
    [SerializeField] private RectTransform driftTutorialUI;
    [SerializeField] private Transform UICanvas;
    [SerializeField] private Sprite leftDriftTutorialSprite;
    [SerializeField] private Sprite rightDriftTutorialSprite;
    public PlayerInput controls;


    private DriftPoint rightDriftPoint;
    private DriftPoint leftDriftPoint;
    private Camera cam;
    private IEnumerator leftDriftCoroutine;
    private IEnumerator rightDriftCoroutine;

    private RectTransform leftPointTransform;
    private RectTransform rightPointTransform;

    public void StartTutorials()
    {
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
        return hasDriftPointLeft && hasDriftPointRight && leftDriftPoint.isShown && rightDriftPoint.isShown;
    }

    private void StartDriftTutorial()
    {
        Time.timeScale = 0;

        Vector2 leftPointUIPosition = cam.WorldToScreenPoint(leftDriftPoint.transform.position);
        Vector2 rightPointUIPosition = cam.WorldToScreenPoint(rightDriftPoint.transform.position);

        leftPointTransform = Instantiate(driftTutorialUI, UICanvas);
        rightPointTransform = Instantiate(driftTutorialUI, UICanvas);

        Vector2 yOffset = new Vector2(0, 100);
        leftPointTransform.position = leftPointUIPosition + yOffset;
        rightPointTransform.position = rightPointUIPosition + yOffset;

        leftPointTransform.GetComponentInChildren<Image>().sprite = leftDriftTutorialSprite;
        rightPointTransform.GetComponentInChildren<Image>().sprite = rightDriftTutorialSprite ;


        controls.P_Controls.LeftDrift.started += OnLeftDrift;
        controls.P_Controls.LeftDrift.canceled += OnLeftDrift;

        controls.P_Controls.RightDrift.started += OnRightDrift;
        controls.P_Controls.RightDrift.canceled += OnRightDrift;
    }

    

    private IEnumerator IsHoldingDriftButtonChck(bool rightButton)
    {
        yield return new WaitForSecondsRealtime(1f);

        Debug.Log("Return To Game " + (rightButton ? "right" : "left") + " Drift");
        CancelDriftSubscriptions();

        leftPointTransform.gameObject.SetActive(false);
        rightPointTransform.gameObject.SetActive(false);

        yield return ChangeTimeScaleOverTime(1, 1, Ease.InSine);
        
        Time.timeScale = 1f;
    }

    private void OnLeftDrift(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            leftDriftCoroutine = IsHoldingDriftButtonChck(false);
            StartCoroutine(leftDriftCoroutine);
        }
        else if (context.canceled)
        {
            StopCoroutine(leftDriftCoroutine);
        }
    }

    private void OnRightDrift(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            rightDriftCoroutine = IsHoldingDriftButtonChck(true);
            StartCoroutine(rightDriftCoroutine);
        }
        else if (context.canceled)
        {
            StopCoroutine(rightDriftCoroutine);
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
        if (controls is null)
            return;

        CancelDriftSubscriptions();
        controls.Disable();
    }


}
