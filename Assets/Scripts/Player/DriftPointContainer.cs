using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class DriftPointContainer : MonoBehaviour
{
    List<DriftPoint> driftPoints = new();
    [SerializeField] private float coyoteTimer;

    private Transform playerTransform;

    private DriftPoint visibleDriftPoint;
    private DriftPoint coyoteDriftPoint;
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

    public void UpdateMe()
    {
        if (driftPoints.Count < 1 || driftPoints.First().isShown)
            return;
        
        BubbleSortSingleIteration();

        if (visibleDriftPoint)
        {
            coyoteDriftPoint = visibleDriftPoint;
            StartCoroutine(CoyoteTimer());
            visibleDriftPoint.HideMe();
        }

        visibleDriftPoint = driftPoints.First();
        visibleDriftPoint.ShowMe(isRight);
    }

    IEnumerator CoyoteTimer()
    {
        yield return new WaitForSeconds(coyoteTimer);
        coyoteDriftPoint = null;
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

    public bool HasDriftPoint()
    {
        return driftPoints.Count > 1 || coyoteDriftPoint;
    }

    public DriftPoint GetDriftPoint()
    {
        return driftPoints.Count > 1 ? driftPoints.First() : coyoteDriftPoint;
    }
}
