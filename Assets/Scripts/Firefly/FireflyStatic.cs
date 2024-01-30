using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class FireflyStatic : MonoBehaviour
{

    [Header("Variables")]
    [SerializeField] protected float timeValue;
    [SerializeField] protected float timeToRespawn;
    [SerializeField] private float timeToCollect;
    private float collectSpeed;
    [SerializeField] private float visualsYOffset;
    
    [Header("References")]
    [SerializeField] protected Transform visuals;
    [SerializeField] private AudioClipDataSingle collectedClipData;


    private MeshRenderer meshRenderer;
    private LODGroup lodGroup;

    protected void Start()
    {
        FireflyManager.updatePosition += UpdateVisualsPosition;

        collectSpeed = 1 / timeToCollect;
    }

    private void OnDestroy()
    {
        FireflyManager.updatePosition -= UpdateVisualsPosition;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.tag.Equals("FireflyCollector"))
            return;

        StartCoroutine(MoveToPlayer(WaitTillRespawnStatic()));
    }
    
    protected IEnumerator MoveToPlayer(IEnumerator waitTillRespawn)
    {
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
    
        GameVariables.instance.gameTimer.AddToTimer(timeValue);
        GameVariables.instance.fireflyCount++;
        
        FireflyManager fireflyManager = FireflyManager.instance;

        if (this is FireflyDynamic)
        {
            // fireflyManager.audioSource.PlayOneShotVariation(collectedClip, new Vector2(.8f, 1.2f), new Vector2(.85f, 1.15f));
            fireflyManager.audioSource.PlayOneShotVariation(collectedClipData);
        }
        else
            StartCoroutine(fireflyManager.PlayStaticFireflySound(collectedClipData));
            
        

        StartCoroutine(waitTillRespawn);
    }
    
    private IEnumerator WaitTillRespawnStatic()
    {
        visuals.gameObject.SetActive(false);
        FireflyManager.updatePosition -= UpdateVisualsPosition;
        
        yield return new WaitForSeconds(timeToRespawn);
        
        visuals.gameObject.SetActive(true);
        FireflyManager.updatePosition += UpdateVisualsPosition;
    }

    protected void UpdateVisualsPosition(Vector2 localPos)
    {
        visuals.position = transform.position + new Vector3(0, visualsYOffset, 0)  + transform.rotation * localPos;
    }
}
