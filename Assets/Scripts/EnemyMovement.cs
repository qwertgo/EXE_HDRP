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
    [SerializeField] private float searchTime = 9f;
    [SerializeField] private float followPlayerRadius = 20f;

    
    [Header("References")]
    [SerializeField] public List<Transform> transformList = new List<Transform>(); // Liste für Transforms
    [SerializeField] private Transform visuals;
    [SerializeField] private Animator animator;
    
    private NavMeshAgent navMeshAgent;
    private Transform movementTarget;
    private bool aboveSurface;
    
    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        movementTarget = GetRandomTransform();
        SetMovementTarget(movementTarget);
    }
    private void Update()
    {
        if (currentState == enemyState.Idle)
        {
            if (Vector3.Distance(movementTarget.position, transform.position) <= 0.5f) //ist es schon da?
            {
                //destinationReached = true;
                movementTarget = GetRandomTransform();
                SetMovementTarget(movementTarget);
            }
        }
    }

    IEnumerator Attack()
    {
        Debug.Log("Attack");
        currentState = enemyState.Attack;

        if (!aboveSurface)
        {
            transform.LookAt(GameVariables.instance.player.transform.position, Vector3.up);
            animator.CrossFade("Attack",0);
            aboveSurface = true;
        }
        
        navMeshAgent.SetDestination(transform.position);
        yield return new WaitForSeconds(1);
        StartCoroutine(FollowPlayer());
    }

    IEnumerator FollowPlayer()
    {
        Debug.Log("Follow");
        currentState = enemyState.FollowPlayer;
        Transform playerTransform = GameVariables.instance.player.transform;
        
        while (Vector3.Distance(playerTransform.position, transform.position) < followPlayerRadius)
        {
            SetMovementTarget(playerTransform);
            yield return null;
        }
        
        StartCoroutine(SearchPlayer());
    }
    
    IEnumerator SearchPlayer()
    {
        Debug.Log("Search");
        currentState = enemyState.SearchPlayer;
        float startTime = Time.realtimeSinceStartup;
        bool foundPlayer = false;
        Transform playerTransform = GameVariables.instance.player.transform;
        
        SetNewSearchDestination(out Vector3 randomPosition);

        while (Time.realtimeSinceStartup - startTime < searchTime && !foundPlayer)
        {
            if (Vector3.Distance(randomPosition, transform.position) <= 1f)
            {
                //destinationReached = true;
                SetNewSearchDestination(out randomPosition);       
            }
            else if (Vector3.Distance(playerTransform.position, transform.position) < followPlayerRadius)
            {
                foundPlayer = true;
                StartCoroutine(FollowPlayer());
            }
            yield return null;
        }

        if (!foundPlayer)
        {
            animator.CrossFade("Idle", 0);
            aboveSurface = false;
            
            currentState = enemyState.Idle;
            movementTarget = GetRandomTransform();
            SetMovementTarget(movementTarget);
        }
    }

    private void SetNewSearchDestination(out Vector3 randomPosition)
    {
        randomPosition = transform.position + new Vector3(Random.Range(-20f, 20f), 0f, Random.Range(-20f, 20f));
        navMeshAgent.SetDestination(randomPosition);
        //Debug.Log("Enemy Pos: " + enemyTransform.position);
        //Debug.Log("Movement Target: " + movementTarget.position);
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag.Equals("Player"))
        {
            StopCoroutine(nameof(FollowPlayer));
            // StopCoroutine(nameof(SearchPlayer));
            StartCoroutine(Attack());
        }
        // if(other.tag.Equals("PlayerGlowArea"))
        // {
        //     collidedWithGlowAreaCounter++;
        //     
        //     if(collidedWithGlowAreaCounter == 1)
        //         currentState = enemyState.FollowPlayer;
        //     // if(collidedWithCounter > 1)
        //     // {
        //     //     Debug.Log("ded");
        //     // }
        //     // else
        //     // {
        //     //     
        //     //     Debug.Log("Current State: Follow Player");
        //     // }
        //     // Debug.Log(collidedWithCounter);
        // }
        // else if (other.tag.Equals("Player"))
        // {
        //     collidedWithPlayerCounter++;
        //     if(collidedWithPlayerCounter > 1)
        //         Debug.Log("You Died");
        // }
    }
    private void OnTriggerExit(Collider other)
    {
        // if (other.tag.Equals("PlayerGlowArea"))
        // {
        //     collidedWithGlowAreaCounter--;
        //     if (collidedWithGlowAreaCounter == 0)
        //     {
        //         currentState = enemyState.SearchPlayer;
        //         Debug.Log("Current State: Search Player");
        //     }
        // }
        // else if (other.tag.Equals("Player"))
        // {
        //     // collidedWithPlayerCounter--;
        // }
        //
        // if (other.tag.Equals("PlayerGlowArea"))
        // {
        //     currentState = enemyState.SearchPlayer;
        //     StopCoroutine(nameof(FollowAndAttackPlayer));
        // }
    }

    internal void SetMovementTarget(Transform movementTarget)
    {
        //movementTarget = GetRandomTransform();
        navMeshAgent.SetDestination(movementTarget.position);

    }
    public Transform GetRandomTransform()
    {
        int randomIndex = Random.Range(0, transformList.Count); // Zufälliger Index
        return transformList[randomIndex]; // Gib den zufälligen Transform zurück
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, followPlayerRadius);
    }
}

//Jetziges Problem: Da einer der Cubes in der Liste das movement Target ist, und dieses immer versetzt wird, wird bei jedem SeachPlayer State
//einer der fixed Punkte auf der Map versetzt. Das wäre auch an sich gar nicht schlimm, aber es besteht die Möglichkeit, dass dieser Punkt
//außerhalb der NavMesh Fläche oder in einem nicht begehbaren Objekt gesetzt wird, was beim nächsten Idle State dazu führen könnte,
//dass der Enemy irgendwo nicht hinkommt, und dann "feststeckt", was ein Game Breaking Bug wäre.