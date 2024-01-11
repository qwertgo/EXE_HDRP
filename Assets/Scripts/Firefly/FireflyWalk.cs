using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;
using UnityEngine.Splines;
using UnityEngine;

public class FireflyWalk : FireflyStatic
{

    [Header("Variables")]
    [SerializeField] float speed;
    [SerializeField] float speedMultiplier = 4;

    private Vector3 rotateAroundPosition;
    private float walkCircleRadius;
    private float destinationPointSpeed;
    

    [Header("References")]
    [SerializeField] private Transform destinationPoint;

    private NavMeshAgent navMeshAgent;
    private NavMeshQueryFilter navMeshFilter;
    private SphereCollider[] colliders;
    private int currentState;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(rotateAroundPosition, Vector3.up);
    }

    new void Start()
    {
        base.Start();
        
        destinationPoint.parent = transform.parent;
        navMeshAgent = GetComponent<NavMeshAgent>();

        colliders = GetComponents<SphereCollider>();

        navMeshAgent.nextPosition = destinationPoint.position;
        navMeshAgent.speed = speed;
        navMeshAgent.acceleration = speed;

        navMeshFilter.areaMask = navMeshAgent.areaMask;
        navMeshFilter.agentTypeID = navMeshAgent.agentTypeID;
        
        StartCoroutine(Idle());
    }
    
    public void SetFireflyValues(float walkCircleRadius, bool moveRight, int number)
    {
        this.walkCircleRadius = walkCircleRadius;
        
        rotateAroundPosition = transform.position;
        destinationPoint.position = rotateAroundPosition + Vector3.right * this.walkCircleRadius;

        destinationPointSpeed = speed / walkCircleRadius * 41;
        destinationPointSpeed = moveRight ? destinationPointSpeed : -destinationPointSpeed;

        destinationPoint.name = "destinationPoint " + string.Format("{0:00}", number);
        gameObject.name = "firefly" + string.Format("{0:00}", number);
    }

    IEnumerator Idle()
    {
        while (enabled)
        {
            yield return null;

            if (GameVariables.instance.isPaused)
                continue;

            RotateAroundPoint(rotateAroundPosition);
        }
    }

    private void RotateAroundPoint(Vector3 position)
    {
        destinationPoint.RotateAround(position, Vector3.up, destinationPointSpeed * Time.deltaTime);
            
        if(NavMesh.SamplePosition(destinationPoint.position, out NavMeshHit hit, 8, navMeshFilter))
            navMeshAgent.SetDestination(hit.position);
        else
            Debug.Log( gameObject.name + " could not find position to walk towards");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.tag.Equals("FireflyCollector"))
            return;

        currentState++;

        if (currentState > 1)
        {
            speed /= speedMultiplier;

            StartCoroutine(MoveToPlayer(other.transform, WaitTillRespawnDynamic()));
        }
        else
        {
            destinationPointSpeed *= speedMultiplier;
            navMeshAgent.speed *= speedMultiplier;
            navMeshAgent.acceleration *= speedMultiplier;
            // navMeshAgent.acceleration = navMeshAgent.speed;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.tag.Equals("FireflyCollector"))
            return;

        currentState--;

        if (currentState == 0)
        {
            speed /= speedMultiplier;
            navMeshAgent.speed /= speedMultiplier;
            // navMeshAgent.acceleration = navMeshAgent.speed;
        }
    }

    private IEnumerator WaitTillRespawnDynamic()
    {
        visuals.gameObject.SetActive(false);
        FireflySpawner.updatePosition -= UpdateVisualsPosition;
        
        foreach (var col in colliders)
            col.enabled = false;
        
        yield return new WaitForSeconds(timeToRespawn);

        visuals.gameObject.SetActive(true);
        FireflySpawner.updatePosition += UpdateVisualsPosition;
        
        foreach (var col in colliders)
            col.enabled = true;
        
    }
}
