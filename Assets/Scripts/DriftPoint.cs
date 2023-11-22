using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DriftPoint : MonoBehaviour
{
    [SerializeField] private MeshRenderer indicatorRenderer;
    [SerializeField] private Material rightMat;
    [SerializeField] private Material leftMat;



    public bool isShown { get; private set; } = false;
    private void OnTriggerEnter(Collider other)
    {
        // indicator.transform.localScale *= 2;
        Debug.Log(other.name);
        Debug.Log(gameObject.name);
        other.GetComponent<DriftPointContainer>().AddDriftPoint(this);
    }

    private void OnTriggerExit(Collider other)
    {
        // indicator.transform.localScale /= 2;
        other.GetComponent<DriftPointContainer>().RemoveDriftPoint(this);
        
        if(isShown)
            HideMe();
    }

    public void ShowMe(bool isRight)
    {
        isShown = true;
        indicatorRenderer.enabled = true;
        indicatorRenderer.material = isRight ? rightMat : leftMat;
    }

    public void HideMe()
    {
        isShown = false;
        indicatorRenderer.enabled = false;
    }
    
}
