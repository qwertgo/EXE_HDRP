using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;
using UnityEngine.Splines;
using UnityEngine;

public class FireflyWalk : MonoBehaviour
{
    private NavMeshAgent navMeshAgent;
    [SerializeField] private Transform destinationPoint;
    [SerializeField] float speed;

    [SerializeField] Transform visuals;
    [SerializeField] SplineAnimate spline;

    [SerializeField] float timeToRespawn;

    [SerializeField] private float collectSpeed;

    private Collider _collider;

    float destinationPointSpeed;    
    float centerDistance;

    bool moveRight = true;

    Vector3 startPosition; 
    
    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        navMeshAgent.speed = speed * 2;
        navMeshAgent.acceleration = speed * 2;

        centerDistance = Vector3.Distance(Vector3.zero, transform.position);
        speed /= centerDistance;
        speed *= 50;

        destinationPointSpeed = moveRight ? speed : -speed;

        startPosition = transform.position;

        _collider = GetComponent<SphereCollider>();
    }

    
    void Update()
    {
        Quaternion destinationPointRotation = Quaternion.Euler(0, Time.time * destinationPointSpeed, 0);
        Vector3 currentDestination = destinationPointRotation * startPosition;

        navMeshAgent.SetDestination(destinationPoint.position = currentDestination);
    }

    public void SetDirection (bool moveRight)
    {
        this.moveRight = moveRight;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag.Equals("FireflyCollector"))
        {
            spline.enabled = false;
            _collider.enabled = false;
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
        float TimeElapsed = 0;

        while (TimeElapsed < timeToRespawn)
        {
            TimeElapsed += Time.deltaTime;
            yield return null;
        }

        _collider.enabled = true;
        spline.enabled = true;
        visuals.gameObject.SetActive(true);
    }
}
