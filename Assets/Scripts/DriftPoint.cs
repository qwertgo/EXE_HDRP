using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DriftPoint : MonoBehaviour
{
    [SerializeField] private float sizeWhenShown = 2f;
    [SerializeField] private MeshRenderer indicatorRenderer;
    [SerializeField] private Material rightMat;
    [SerializeField] private Material leftMat;

    public bool isShown { get; private set; } = false;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, sizeWhenShown);
    }

    private void Start()
    {
        indicatorRenderer.gameObject.transform.localScale = new Vector3(sizeWhenShown, sizeWhenShown, sizeWhenShown);
    }

    private void OnTriggerEnter(Collider other)
    {
        other.GetComponent<DriftPointContainer>().AddDriftPoint(this);
    }

    private void OnTriggerExit(Collider other)
    { 
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
