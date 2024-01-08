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
    [SerializeField] private float followPlayerRadius = 20f;
    [SerializeField] private float idleSpeed;
    [SerializeField] private float followPlayerSpeed;
    [SerializeField] private float lightIntensityCloseToPlayer = 14852.37f;
    [SerializeField] private float lightIntensityFarFromPlayer = 1.520883e+07f;
    [HideInInspector] public bool finishedAttack;

    
    [Header("References")]
    [SerializeField] public List<Transform> idleDestinationPoints = new (); // Liste für Transforms
    [SerializeField] private Animator animator;
    [SerializeField] private Light spotLight;
    [SerializeField] private AudioSource attackPlayerSource;

    [SerializeField] private Collider mouthCollider;
    [SerializeField] private Collider attackPlayerCollider;
    
    private Transform playerTransform;
    private NavMeshAgent navMeshAgent;

    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {

        playerTransform = GameVariables.instance.player.transform;
        navMeshAgent.speed = idleSpeed;
        navMeshAgent.acceleration = idleSpeed;
        
        GameVariables.instance.onPause.AddListener(PauseMe);
        StartCoroutine(Idle());
    }

    protected IEnumerator Idle()
    {
        currentState = enemyState.Idle;
        Vector3 destinationPoint = GetRandomTransform().position;
        navMeshAgent.SetDestination(destinationPoint);
        
        navMeshAgent.speed = idleSpeed;
        navMeshAgent.acceleration = idleSpeed;
        
        while (currentState == enemyState.Idle)
        {
            if (Vector3.Distance(destinationPoint, transform.position) <= 0.5f) //ist es schon da?
            {
                destinationPoint = GetRandomTransform().position;

                navMeshAgent.SetDestination(destinationPoint);
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
        attackPlayerSource.Play();
        LookAtPlayer();

        while (!finishedAttack)
        {
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
        spotLight.intensity = 
        
        navMeshAgent.speed = followPlayerSpeed;
        navMeshAgent.acceleration = followPlayerSpeed;

        float distanceToPlayer = Vector3.Distance(playerTransform.position, transform.position);
        
        while (distanceToPlayer < followPlayerRadius)
        {
            SetMovementTarget(playerTransform);
            LookAtPlayer();
            spotLight.transform.LookAt(playerTransform);
            spotLight.intensity = Mathf.Lerp(lightIntensityCloseToPlayer, lightIntensityFarFromPlayer, distanceToPlayer / followPlayerRadius);
            
            yield return null;

            distanceToPlayer = Vector3.Distance(playerTransform.position, transform.position);
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

    private Transform GetTransformClosestToPlayer()
    {
        float[] distancesToPlayer = new float[idleDestinationPoints.Count];
        
        for (int i = 0; i < distancesToPlayer.Length; i++)
        {
            distancesToPlayer[i] = Vector3.Distance(idleDestinationPoints[i].position, playerTransform.position);
        }

        for (int i = distancesToPlayer.Length - 1; i > 0; i--)
        {
            if (distancesToPlayer[i] < distancesToPlayer[i - 1])
            {
                float tmpDistance = distancesToPlayer[i];
                distancesToPlayer[i] = distancesToPlayer[i - 1];
                distancesToPlayer[i - 1] = tmpDistance;

                Transform tmpDestinationPoint = idleDestinationPoints[i];
                idleDestinationPoints[i] = idleDestinationPoints[i - 1];
                idleDestinationPoints[i - 1] = tmpDestinationPoint;
            }
        }
        Debug.Log(idleDestinationPoints[0].name);
        return idleDestinationPoints[0];
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, followPlayerRadius);
    }

    void PauseMe()
    {
        StartCoroutine(WhilePaused());
    }

    IEnumerator WhilePaused()
    {
        float speed = navMeshAgent.speed;
        navMeshAgent.speed = 0;

        yield return new WaitWhile(() => GameVariables.instance.isPaused);

        navMeshAgent.speed = speed;
    }
}