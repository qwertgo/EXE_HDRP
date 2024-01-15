using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class DriftPoint : MonoBehaviour
{
    [SerializeField] private Material rightMat;
    [SerializeField] private Material leftMat;
    [SerializeField] private Material defaultMat;

    [FormerlySerializedAs("renderers")] [SerializeField] private MeshRenderer[] meshRenderers;

    public bool isShown { get; private set; } = false;

    private void Start()
    {
        meshRenderers = GetComponentsInChildren<MeshRenderer>();
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

        foreach (var meshRenderer in meshRenderers)
        {
            meshRenderer.sharedMaterial = isRight ? rightMat : leftMat;
        }
    }

    public void HideMe()
    {
        isShown = false;
        foreach (var meshRenderer in meshRenderers)
        {
            meshRenderer.sharedMaterial = defaultMat;
        }
    }
    
}
