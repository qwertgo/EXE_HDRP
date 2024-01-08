using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;
using UnityEngine.Splines;
using UnityEngine;

public class FireflyWalk : MonoBehaviour
{
    enum FireflyType {StillStanding, Moving}

    [Header("Variables")]
    [SerializeField] private float timeValue;
    [SerializeField] float speed;
    [SerializeField] float timeToRespawn;
    [SerializeField] private float collectSpeed;
    [SerializeField] private FireflyType type = FireflyType.Moving;
    
    private Vector3 rotateAroundPosition;
    private float walkCircleRadius;
    private float destinationPointSpeed;
    private bool moveRight = true;
    
    [Header("References")]
    [SerializeField] private Transform destinationPoint;
    [SerializeField] Transform visuals;
    [SerializeField] SplineAnimate spline;
    
    private NavMeshAgent navMeshAgent;
    private NavMeshQueryFilter navMeshFilter;
    private Collider sphereCollider;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(rotateAroundPosition, Vector3.up);
    }

    void Start()
    {
        destinationPoint.parent = transform.parent;
        navMeshAgent = GetComponent<NavMeshAgent>();
        sphereCollider = GetComponent<SphereCollider>();

        navMeshAgent.nextPosition = destinationPoint.position;
        navMeshAgent.speed = speed;
        navMeshAgent.acceleration = speed;

        navMeshFilter.areaMask = navMeshAgent.areaMask;
        navMeshFilter.agentTypeID = navMeshAgent.agentTypeID;
        
        GameVariables variables = GameVariables.instance;
        variables.onPause.AddListener(PauseMe);

        if(type == FireflyType.Moving)
            StartCoroutine(UpdateCoroutine());
    }

    
    IEnumerator UpdateCoroutine()
    {
        while (enabled)
        {
            yield return null;
            
            if(GameVariables.instance.isPaused)
                continue;

            destinationPoint.RotateAround(rotateAroundPosition, Vector3.up, destinationPointSpeed * Time.deltaTime);
            
            if(NavMesh.SamplePosition(destinationPoint.position, out NavMeshHit hit, 8, navMeshFilter))
                navMeshAgent.SetDestination(hit.position);
            else
            {
                Debug.Log( gameObject.name + " could not find position to walk towards");
            }
                
            
        }
    }

    public void SetFireflyValues(float walkCircleRadius, bool moveRight, int number)
    {
        this.moveRight = moveRight;
        this.walkCircleRadius = walkCircleRadius;
        
        rotateAroundPosition = transform.position;
        destinationPoint.position = rotateAroundPosition + Vector3.right * this.walkCircleRadius;

        destinationPointSpeed = speed / walkCircleRadius * 41;

        destinationPoint.name = "destinationPoint " + string.Format("{0:00}", number);
        gameObject.name = "firefly" + string.Format("{0:00}", number);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag.Equals("FireflyCollector"))
        {
            spline.enabled = false;
            sphereCollider.enabled = false;
            StartCoroutine(MoveToPlayer(other.transform));
        }
    }
    IEnumerator MoveToPlayer(Transform player)
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
    
        StartCoroutine(WaitTillRespawn());
    }
    
    IEnumerator WaitTillRespawn()
    {
        yield return new WaitForSeconds(timeToRespawn);
    
        sphereCollider.enabled = true;
        spline.enabled = true;
        visuals.gameObject.SetActive(true);
    }

    void PauseMe()
    {
        StartCoroutine(WhilePause());
    }

    IEnumerator WhilePause()
    {
        float speed = navMeshAgent.speed;
        navMeshAgent.speed = 0;
        
        yield return new WaitWhile(() => GameVariables.instance.isPaused);

        navMeshAgent.speed = speed;
    }
}
