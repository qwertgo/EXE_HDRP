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
        // playerTransform = transform.parent.parent;
        playerTransform = GameVariables.instance.player.transform;
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
        if (p == visibleDriftPoint)
        {
            StartCoyoteTimer(p);
            p.HideMe();
            visibleDriftPoint = null;
        }
        
        driftPoints.Remove(p);
    }

    public void UpdateMe()
    {
        if (driftPoints.Count == 0)
            return;

        BubbleSortSingleIteration();
        
        if (driftPoints.First().isShown)
            return;

        if (visibleDriftPoint)
        {
            StartCoyoteTimer(visibleDriftPoint);
            visibleDriftPoint.HideMe();
        }

        visibleDriftPoint = driftPoints.First();
        visibleDriftPoint.ShowMe(isRight);
    }

    private void StartCoyoteTimer(DriftPoint point)
    {
        StopAllCoroutines();
        StartCoroutine(CoyoteTimer(point));
    }

    IEnumerator CoyoteTimer(DriftPoint point)
    {
        coyoteDriftPoint = point;
        yield return new WaitForSeconds(coyoteTimer);
        coyoteDriftPoint = null;
    }

    void BubbleSortSingleIteration()
    {
        float indexDistance = Vector3.Distance(driftPoints[driftPoints.Count - 1].transform.position, playerTransform.position);

        for (int i = driftPoints.Count - 1; i > 0; i--)
        {            
            float nextIndexDistance = Vector3.Distance(driftPoints[i - 1].transform.position, playerTransform.position);
            if (indexDistance < nextIndexDistance)
            {
                DriftPoint tmp = driftPoints[i];
                driftPoints[i] = driftPoints[i - 1];
                driftPoints[i - 1] = tmp;
            }

            indexDistance = nextIndexDistance;
        }
    }

    public bool HasDriftPoint(out Transform driftPoint)
    {
        bool hasPoint = driftPoints.Count > 0 || coyoteDriftPoint;

        driftPoint = hasPoint ? GetDriftPoint().transform : null;

        return hasPoint;
    }

    public DriftPoint GetDriftPoint()
    {
        return coyoteDriftPoint is not null ? coyoteDriftPoint : visibleDriftPoint;
    }

    private void OnDestroy()
    {
        driftPoints.Clear();
        visibleDriftPoint = null;
        coyoteDriftPoint = null;
        
    }
}
