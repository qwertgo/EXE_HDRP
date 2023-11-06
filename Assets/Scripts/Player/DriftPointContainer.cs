using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class DriftPointContainer : MonoBehaviour
{
    public List<DriftPoint> driftPoints { get; private set; } = new();

    private Transform playerTransform;

    private DriftPoint visibleDriftPoint;
    private bool isRight;

    private void Start()
    {
        playerTransform = transform.parent.parent;
    }

    public void SetRightContainer()
    {
        isRight = true;
    }

    public void AddDriftPoint(DriftPoint p)
    {
        driftPoints.Add(p);
        // Debug.Log(driftPoints.Count);
    }

    public void RemoveDriftPoint(DriftPoint p)
    {
        driftPoints.Remove(p);
    }

    public void GetDriftPoints()
    {
        BubbleSortSingleIteration();

        if (driftPoints.Count < 1 || driftPoints.First().isShown)
            return;
        
        if(visibleDriftPoint != null)
            visibleDriftPoint.HideMe();

        visibleDriftPoint = driftPoints.First();
        visibleDriftPoint.ShowMe(isRight);


    }

    void BubbleSortSingleIteration()
    {
        for (int i = driftPoints.Count - 1; i > 0; i--)
        {
            float indexDistance = Vector3.Distance(driftPoints[i].transform.position,playerTransform.position);
            float nextIndexDistance = Vector3.Distance(driftPoints[i - 1].transform.position, playerTransform.position);
            if (indexDistance < nextIndexDistance)
            {
                DriftPoint tmp = driftPoints[i];
                driftPoints[i] = driftPoints[i - 1];
                driftPoints[i - 1] = tmp;
            }
        }
    }
    
}
