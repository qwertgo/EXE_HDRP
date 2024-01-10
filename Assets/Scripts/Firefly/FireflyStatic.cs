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
    [SerializeField] protected float collectSpeed;
    
    [Header("References")]
    [SerializeField] protected Transform visuals;
    [SerializeField] protected SplineAnimate spline;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.tag.Equals("FireflyCollector"))
            return;

        StartCoroutine(MoveToPlayer(other.transform, WaitTillRespawnStatic()));
    }
    
    protected IEnumerator MoveToPlayer(Transform player, IEnumerator waitTillRespawn)
    {
        Vector3 startPos = visuals.position;
        float t = 0;
    
        while (t < 1)
        {
            t += Time.deltaTime * collectSpeed;
            visuals.position = Vector3.Lerp(startPos, player.position, t * t);
            yield return null;
        }
    
        GameVariables.instance.gameTimer.AddToTimer(timeValue);
        GameVariables.instance.fireflyCount++;
    
        visuals.gameObject.SetActive(false);
    
        StartCoroutine(waitTillRespawn);
    }
    
    private IEnumerator WaitTillRespawnStatic()
    {
        yield return new WaitForSeconds(timeToRespawn);
        
        spline.enabled = true;
        visuals.gameObject.SetActive(true);
    }
}
