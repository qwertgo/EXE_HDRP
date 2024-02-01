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
            coyoteDriftPoint = p;
            StartCoroutine(CoyoteTimer());
            p.HideMe();
            visibleDriftPoint = null;
        }
        
        driftPoints.Remove(p);
    }

    public void UpdateMe()
    {
        BubbleSortSingleIteration();
        
        if (driftPoints.Count == 0 || driftPoints.First().isShown)
            return;

        if (visibleDriftPoint)
            visibleDriftPoint.HideMe();

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
            // Debug.Log(driftPoints);
            // Debug.Log(driftPoints[i]);
            // Debug.Log(playerTransform);
            
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

    public bool HasDriftPoint(out Transform driftPoint)
    {
        bool hasPoint = driftPoints.Count > 0 || coyoteDriftPoint;

        driftPoint = hasPoint ? GetDriftPoint().transform : null;

        return hasPoint;
    }

    public DriftPoint GetDriftPoint()
    {
        return driftPoints.Count > 0 ? driftPoints.First() : coyoteDriftPoint;
    }

    private void OnDestroy()
    {
        driftPoints.Clear();
        visibleDriftPoint = null;
        coyoteDriftPoint = null;
        
    }
}
