using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;
using UnityEngine;

public class FireflyWalk : MonoBehaviour
{
    private NavMeshAgent navMeshAgent;
    [SerializeField] private Transform destinationPoint;
    [SerializeField] float speed;
    
    float centerDistance;
    Vector3 startPosition; 
    
    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        navMeshAgent.speed = speed * 2;
        navMeshAgent.acceleration = speed * 2;
        centerDistance = Vector3.Distance(Vector3.zero, transform.position);

        startPosition = transform.position;
    }

    
    void Update()
    {
        Quaternion destinationPointRotation = Quaternion.Euler(0, Time.time * speed, 0);
        Vector3 currentDestination = destinationPointRotation * startPosition;

        navMeshAgent.SetDestination(destinationPoint.position = currentDestination);

    }
}
