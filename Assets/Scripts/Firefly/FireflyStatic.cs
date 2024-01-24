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
    [SerializeField] private AudioClip collectedClip;


    private MeshRenderer meshRenderer;
    private LODGroup lodGroup;
    private AudioSource audioSource;

    protected void Start()
    {
        FireflyManager.updatePosition += UpdateVisualsPosition;

        collectSpeed = 1 / timeToCollect;

        audioSource = GetComponent<AudioSource>();
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

        if (this is FireflyWalk)
            audioSource.PlayOneShotVariation(collectedClip, new Vector2(.8f, 1.2f), new Vector2(.85f, 1.15f));
        else
            StartCoroutine(FireflyManager.PlayStaticFireflySound(audioSource, collectedClip));
            
        

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
