using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;
using UnityEngine.Splines;
using UnityEngine;

public class FireflyWalk : MonoBehaviour
{
    [Header("Variables")]
    [SerializeField] float speed;
    [SerializeField] float timeToRespawn;
    [SerializeField] private float collectSpeed;
    
    private Vector3 rotateAroundPosition;
    private float walkCircleRadius;
    private float destinationPointSpeed;
    private bool moveRight = true;
    
    [Header("References")]
    [SerializeField] private Transform destinationPoint;
    [SerializeField] Transform visuals;
    [SerializeField] SplineAnimate spline;
    
    private NavMeshAgent navMeshAgent;
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
    }

    
    void Update()
    {
        destinationPoint.RotateAround(rotateAroundPosition, Vector3.up, destinationPointSpeed * Time.deltaTime);

        navMeshAgent.SetDestination(destinationPoint.position);
    }

    public void SetFireflyValues(float walkCircleRadius, bool moveRight)
    {
        this.moveRight = moveRight;
        this.walkCircleRadius = walkCircleRadius;
        
        rotateAroundPosition = transform.position;
        destinationPoint.position = rotateAroundPosition + Vector3.right * this.walkCircleRadius;

        destinationPointSpeed = speed / walkCircleRadius * 40;
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
    
        player.parent.GetComponent<PlayerController>().CollectFirefly();
    
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
}
