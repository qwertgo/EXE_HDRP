using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class EnemyMovement : MonoBehaviour
{
    enum enemyState {Idle, Attack, FollowPlayer, SearchPlayer}

    [Header("Variables")]
    [SerializeField] enemyState currentState;
    // [SerializeField] private float searchTime = 9f;
    [SerializeField] private float followPlayerRadius = 20f;
    [HideInInspector] public bool finishedAttack;

    
    [Header("References")]
    [SerializeField] public List<Transform> idleDestinationPoints = new (); // Liste für Transforms
    [SerializeField] private Animator animator;
    [SerializeField] private Light spotLight;

    [SerializeField] private Collider mouthCollider;
    [SerializeField] private Collider attackPlayerCollider;
    
    private Transform playerTransform;
    private NavMeshAgent navMeshAgent;
    private Transform movementTarget;

    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        StartCoroutine(Idle());

        playerTransform = GameVariables.instance.player.transform;
    }

    protected IEnumerator Idle()
    {
        currentState = enemyState.Idle;
        movementTarget = GetRandomTransform();
        navMeshAgent.SetDestination(movementTarget.position);
        
        while (currentState == enemyState.Idle)
        {
            if (Vector3.Distance(movementTarget.position, transform.position) <= 0.5f) //ist es schon da?
            {
                movementTarget = GetRandomTransform();
                SetMovementTarget(movementTarget);
            }

            yield return null;
        }
    }

    IEnumerator Attack()
    {
        // Debug.Log("Attack");
        currentState = enemyState.Attack;
        mouthCollider.enabled = true;
        attackPlayerCollider.enabled = false;

        animator.CrossFade("Attack",0);

        while (!finishedAttack)
        {
            LookAtPlayer();
            navMeshAgent.SetDestination(playerTransform.position);
            yield return null;
        }

        StartCoroutine(FollowPlayer());
        finishedAttack = false;
    }

    IEnumerator FollowPlayer()
    {
        // Debug.Log("Follow");
        currentState = enemyState.FollowPlayer;
        spotLight.enabled = true;
        spotLight.range = followPlayerRadius + 3;
        
        while (Vector3.Distance(playerTransform.position, transform.position) < followPlayerRadius)
        {
            SetMovementTarget(playerTransform);
            LookAtPlayer();
            spotLight.transform.LookAt(playerTransform);
            yield return null;
        }

        animator.CrossFade("Idle", 0);
        mouthCollider.enabled = false;
        attackPlayerCollider.enabled = true;
        spotLight.enabled = false;
        
        StartCoroutine(Idle());
    }

    void LookAtPlayer()
    {
        Vector3 direction = (playerTransform.position - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(direction);
    }
    
    // IEnumerator SearchPlayer()
    // {
    //     Debug.Log("Search");
    //     currentState = enemyState.SearchPlayer;
    //     float startTime = Time.realtimeSinceStartup;
    //     bool foundPlayer = false;
    //     Transform playerTransform = GameVariables.instance.player.transform;
    //     
    //     SetNewSearchDestination(out Vector3 randomPosition);
    //
    //     while (Time.realtimeSinceStartup - startTime < searchTime && !foundPlayer)
    //     {
    //         if (Vector3.Distance(randomPosition, transform.position) <= 1f)
    //         {
    //             //destinationReached = true;
    //             SetNewSearchDestination(out randomPosition);       
    //         }
    //         else if (Vector3.Distance(playerTransform.position, transform.position) < followPlayerRadius)
    //         {
    //             foundPlayer = true;
    //             StartCoroutine(FollowPlayer());
    //         }
    //         yield return null;
    //     }
    //
    //     if (!foundPlayer)
    //     {
    //         animator.CrossFade("Idle", 0);
    //         aboveSurface = false;
    //         
    //         currentState = enemyState.Idle;
    //         movementTarget = GetRandomTransform();
    //         SetMovementTarget(movementTarget);
    //     }
    // }

    // private void SetNewSearchDestination(out Vector3 randomPosition)
    // {
    //     randomPosition = transform.position + new Vector3(Random.Range(-20f, 20f), 0f, Random.Range(-20f, 20f));
    //     navMeshAgent.SetDestination(randomPosition);
    //     //Debug.Log("Enemy Pos: " + enemyTransform.position);
    //     //Debug.Log("Movement Target: " + movementTarget.position);
    // }
    
    private void OnTriggerEnter(Collider other)
    {
        if (!other.tag.Equals("Player"))
            return;

        if(currentState == enemyState.Idle)
            StartCoroutine(Attack());
        else if(currentState > 0)
            GameVariables.instance.player.Die();
    }

    private void SetMovementTarget(Transform movementTarget)
    {
        navMeshAgent.SetDestination(movementTarget.position);
    }
    
    private Transform GetRandomTransform()
    {
        int randomIndex = Random.Range(0, idleDestinationPoints.Count); // Zufälliger Index
        return idleDestinationPoints[randomIndex]; // Gib den zufälligen Transform zurück
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, followPlayerRadius);
    }
}