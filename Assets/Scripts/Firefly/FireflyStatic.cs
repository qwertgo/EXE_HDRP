using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Splines;

public class FireflyStatic : MonoBehaviour
{

    [Header("Variables")]
    [SerializeField] protected float timeValue;
    [SerializeField] protected float timeToRespawn;
    [SerializeField] private float timeToCollect;
    [SerializeField] private float visualsYOffset;

    private float collectSpeed;
    
    [Header("References")]
    [SerializeField] protected Transform visuals;
    [SerializeField] private AudioClipDataSingle collectedClipData;
    [SerializeField] private GameObject collectedParticleSystem;
    [SerializeField] private CopyPosition particleCopyPosition;

    private Collider sphereCol;
    private GameVariables gameVariables => GameVariables.instance;
    private HighScoreCounter highScoreCounter => gameVariables.highScoreCounter;
    private FireflyManager fireflyManager => FireflyManager.instance;

    protected void Start()
    {
        FireflyManager.updatePosition += UpdateVisualsPosition;
        GameManager.gameOverEvent += Disable;

        collectSpeed = 1 / timeToCollect;
        sphereCol = GetComponent<SphereCollider>();
        //particleCopyPosition = GetComponentInChildren<CopyPosition>();

    }



    private void OnDestroy()
    {
        FireflyManager.updatePosition -= UpdateVisualsPosition;
        GameManager.gameOverEvent -= Disable;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.tag.Equals("FireflyCollector"))
            return;

        StartCoroutine(MoveToPlayer(false));
    }
    
    protected IEnumerator MoveToPlayer(bool isDynamic)
    {
        if (isDynamic)
            Debug.Log("dynamic firefly move to player " + name);

        RectTransform timeSliderTransform = GameVariables.instance.timeSlider;
        Camera cam = GameVariables.instance.cam;
        Vector3 startPos = visuals.position;
        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime * collectSpeed;
            timeSliderTransform.GetPositionAndRotation(out Vector3 pos, out Quaternion rot);
            Vector3 timeSliderPos = cam.ScreenToWorldPoint(pos + new Vector3(0,0, 1));
            visuals.position = Vector3.Lerp(startPos, timeSliderPos, t * t);
            
            yield return null;
        }
    
        gameVariables.gameTimer.AddToTimer(timeValue);
        gameVariables.fireflyCount++;

        fireflyManager.StartCoroutine(fireflyManager.CollectFireFly(collectedClipData, this));

        collectedParticleSystem.SetActive(true);
        Transform playerTransform = GameVariables.instance.player.transform;
        collectedParticleSystem.transform.position = playerTransform.position;
        particleCopyPosition.transformToCopyPositionFrom = GameVariables.instance.player.transform;

        StartCoroutine(WaitTillRespawn(isDynamic));

        yield return new WaitForSeconds(10f);
        collectedParticleSystem.SetActive(false);
    }
    
    private IEnumerator WaitTillRespawn(bool isDynamic)
    {
        visuals.gameObject.SetActive(false);
        CollidersSetActive(false);
        FireflyManager.updatePosition -= UpdateVisualsPosition;

        if (isDynamic)
            Debug.Log("Visuals have been disabled");
        
        yield return new WaitForSeconds(timeToRespawn);
        
        visuals.gameObject.SetActive(true);
        CollidersSetActive(true);
        FireflyManager.updatePosition += UpdateVisualsPosition;
    }

    protected void CollidersSetActive(bool active)
    {
        sphereCol.enabled = active;
    }

    protected void UpdateVisualsPosition(Vector2 localPos)
    {
        visuals.position = transform.position + new Vector3(0, visualsYOffset, 0)  + transform.rotation * localPos;
    }

    private void Disable()
    {
        StopAllCoroutines();
        enabled = false;
        sphereCol.enabled = false;
        FireflyManager.updatePosition -= UpdateVisualsPosition;
        CollidersSetActive(false);
    }
}
