using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class EnemyMovement : MonoBehaviour
{
    enum enemyState {Idle, Attack, FollowPlayer}

    [Header("Variables")]
    [SerializeField] enemyState currentState;
    [SerializeField] private float followPlayerRadius = 20f;
    [SerializeField] private float idleSpeed;
    [SerializeField] private float followPlayerSpeed;
    [SerializeField] private float directionLerpFactor;
    [SerializeField] private float lightIntensityCloseToPlayer = 12000f;
    [SerializeField] private float lightIntensityFarFromPlayer = 1.520883e+07f;
    [HideInInspector] public bool finishedAttack;
    private int currentIdlePointIndex;

    [Header("References")]
    public List<Vector3> idleDestinationPoints = new ();
    [SerializeField] private Animator animator;
    [SerializeField] private Light spotLight;
    [SerializeField] private Collider attackPlayerCollider;
    [SerializeField] private Collider mouthCollider;


    [Header("Animations")] 
    [SerializeField] private AnimationClip attackClip;
    [SerializeField] private AnimationClip idleClip;
    [SerializeField] private AnimationClip followPlayerClip;
    [SerializeField] private AnimationClip diveIntoWaterClip;
    [SerializeField] private AnimationClip surfaceFromWaterClip;

    [Header("Audio")]
    [SerializeField] private string growlAudioPath;
    [SerializeField] private AudioSource mainAudioSource;
    [SerializeField] private AudioSource musicAudioSource;

    private AudioClip[] growlAudioClips;

    private Transform playerTransform;
    private NavMeshAgent navMeshAgent;

    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        playerTransform = GameVariables.instance.player.transform;
        navMeshAgent.speed = idleSpeed;
        navMeshAgent.acceleration = idleSpeed;
        navMeshAgent.autoBraking = false;

        GameVariables.instance.onPause.AddListener(PauseMe);

        growlAudioClips = Resources.LoadAll<AudioClip>(growlAudioPath);
        mainAudioSource.PlayRandomOneShot(growlAudioClips);
        
        EnemyManager.foundPlayer.AddListener(DiveUnderWater);
        EnemyManager.lostPlayer.AddListener(SurfaceFromWater);

        StartCoroutine(Idle());
    }

    // public void SetEvents(UnityEvent enemyFoundPlayer, UnityEvent enemyLostPlayer)
    // {
    //     foundPlayerEvent = enemyFoundPlayer;
    //     lostPlayerEvent = enemyLostPlayer;
    //     
    //     foundPlayerEvent.AddListener(DiveUnderWater);
    //     lostPlayerEvent.AddListener(SurfaceFromWater);
    // }

    private IEnumerator Idle()
    {
        currentState = enemyState.Idle;

        navMeshAgent.speed = idleSpeed;
        navMeshAgent.acceleration = idleSpeed;
        
        while (currentState == enemyState.Idle)
        {
            yield return ReachRandomIdlePoint();
        }
    }

    private IEnumerator ReachRandomIdlePoint()
    {
        Vector3 destinationPoint = GetRandomDestinationPoint();
        navMeshAgent.SetDestination(destinationPoint);
        yield return new WaitWhile(() => navMeshAgent.remainingDistance > .5f);
    }

    private IEnumerator Attack()
    {
        currentState = enemyState.Attack;
        animator.CrossFade(attackClip.name,0);
        EnemyManager.foundPlayer.Invoke();

        mouthCollider.enabled = true;
        attackPlayerCollider.enabled = false;

        mainAudioSource.PlayRandomOneShot(growlAudioClips);
        
        musicAudioSource.volume = .35f;
        musicAudioSource.Play();
        navMeshAgent.SetDestination(playerTransform.position);

        while (!finishedAttack)
        {
            LookAtPlayer();
            navMeshAgent.SetDestination(playerTransform.position);
            yield return null;
        }


        finishedAttack = false;
        StartCoroutine(FollowPlayer());
    }

    private IEnumerator FollowPlayer()
    {
        currentState = enemyState.FollowPlayer;
        animator.CrossFade(followPlayerClip.name, 0);
        
        spotLight.enabled = true;
        spotLight.range = followPlayerRadius + 3;

        navMeshAgent.speed = followPlayerSpeed;
        navMeshAgent.acceleration = followPlayerSpeed;

        float distanceToPlayer = Vector3.Distance(playerTransform.position, transform.position);
        
        while (distanceToPlayer < followPlayerRadius)
        {
            navMeshAgent.SetDestination(playerTransform.position);
            navMeshAgent.velocity = Vector3.Lerp(transform.forward, navMeshAgent.desiredVelocity.normalized, directionLerpFactor) * navMeshAgent.desiredVelocity.magnitude;
            
            
            spotLight.transform.LookAt(playerTransform);

            float distancePercentage = distanceToPlayer / followPlayerRadius;
            spotLight.intensity = Mathf.Lerp(lightIntensityCloseToPlayer, lightIntensityFarFromPlayer, distancePercentage);
            musicAudioSource.volume = distancePercentage * -1 + 1;
            
            yield return null;

            distanceToPlayer = Vector3.Distance(playerTransform.position, transform.position);
        }

        mouthCollider.enabled = false;
        spotLight.enabled = false;
        musicAudioSource.Stop();
        EnemyManager.lostPlayer.Invoke();

        //return to searchArea and disable yourself while returning
        DiveUnderWater();
        yield return ReachRandomIdlePoint();
        currentState = enemyState.Idle;
        SurfaceFromWater();
        
        StartCoroutine(Idle());
    }
    
    private void DiveUnderWater()
    {
        Debug.Log(name + " got disabled");
        if(currentState == enemyState.Attack)
            return;
        
        animator.CrossFade(diveIntoWaterClip.name, 0);
        attackPlayerCollider.enabled = false;
    }

    private void SurfaceFromWater()
    {
        Debug.Log(name + " got enabled");
        if(currentState == enemyState.FollowPlayer)
            return;
        
        animator.CrossFade(surfaceFromWaterClip.name, 0);
        attackPlayerCollider.enabled = true;
    }

    private void LookAtPlayer()
    {
        Vector3 direction = (playerTransform.position - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(direction);
    }
    private Vector3 GetRandomDestinationPoint()
    {
        int randomIndex = Random.Range(0, idleDestinationPoints.Count - 1); // Zufälliger Index
        randomIndex = (currentIdlePointIndex + randomIndex + 1) % idleDestinationPoints.Count;  //makes sure the same index is not picked twice

        currentIdlePointIndex = randomIndex;
        return idleDestinationPoints[randomIndex];
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (!other.tag.Equals("Player"))
            return;

        if(currentState == enemyState.Idle)
            StartCoroutine(Attack());
        else if(currentState > 0)
            GameManager.instance.StopGame();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, followPlayerRadius);
    }

    private void PauseMe()
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