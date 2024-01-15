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
    [SerializeField] private float visualsYOffset;
    
    [Header("References")]
    [SerializeField] protected Transform visuals;

    private MeshRenderer meshRenderer;
    private LODGroup lodGroup;

    protected void Start()
    {
        FireflySpawner.updatePosition += UpdateVisualsPosition;
        meshRenderer = visuals.GetComponent<MeshRenderer>();
    }

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

        StartCoroutine(waitTillRespawn);
    }
    
    private IEnumerator WaitTillRespawnStatic()
    {
        visuals.gameObject.SetActive(false);
        FireflySpawner.updatePosition -= UpdateVisualsPosition;
        
        yield return new WaitForSeconds(timeToRespawn);
        
        visuals.gameObject.SetActive(true);
        FireflySpawner.updatePosition += UpdateVisualsPosition;
    }

    protected void UpdateVisualsPosition(Vector2 localPos)
    {
        visuals.position = transform.position + new Vector3(0, visualsYOffset, 0)  + transform.rotation * localPos;
    }
}
