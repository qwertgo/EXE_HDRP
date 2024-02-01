using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class EnemyMovement : MonoBehaviour
{
    enum enemyState {Idle, Attack, FollowPlayer, TryReachPlayer}

    [Header("Variables")]
    [SerializeField] enemyState currentState;
    [SerializeField] private float followPlayerRadius = 20f;
    [SerializeField] private float idleSpeed;
    [SerializeField] private float followPlayerSpeed;
    [SerializeField] private float directionLerpFactor;
    [SerializeField] private float lightIntensityCloseToPlayer = 12000f;
    [SerializeField] private float lightIntensityFarFromPlayer = 1.520883e+07f;
    [SerializeField] private LayerMask groundObstacleLayer;
    [HideInInspector] public bool finishedAttack;
    [SerializeField] bool isStartEnemy;
    private int currentIdlePointIndex;

    [Header("References")]
    public List<Vector3> idleDestinationPoints = new ();
    [SerializeField] private Animator animator;
    [SerializeField] private Light spotLight;
    [SerializeField] private SphereCollider attackPlayerCollider;
    [SerializeField] private SphereCollider mouthCollider;


    [Header("Animations")] 
    [SerializeField] private AnimationClip attackClip;
    [SerializeField] private AnimationClip idleClip;
    [SerializeField] private AnimationClip followPlayerClip;
    [SerializeField] private AnimationClip diveIntoWaterClip;
    [SerializeField] private AnimationClip surfaceFromWaterClip;

    [Header("Audio")]
    [SerializeField] private AudioClipDataMultiple growlAudioData;
    [SerializeField] private AudioClipDataSingle waterSplashClipData;
    [SerializeField] private AudioSource mainAudioSource;
    [SerializeField] private AudioSource musicAudioSource;

    private Transform playerTransform;
    private NavMeshAgent navMeshAgent;
    private IEnumerator reachIdlePointCoroutine;
    private IEnumerator reachPlayerCoroutine;

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

        EnemyManager.foundPlayer.AddListener(DiveUnderWater);
        EnemyManager.lostPlayer.AddListener(SurfaceFromWater);
        
        growlAudioData.LoadClips();

        StartCoroutine(Idle());
    }

    private void OnDisable()
    {
        EnemyManager.foundPlayer.RemoveListener(DiveUnderWater);
        EnemyManager.lostPlayer.RemoveListener(SurfaceFromWater);
    }

    #region idle
    private IEnumerator Idle()
    {
        currentState = enemyState.Idle;

        navMeshAgent.speed = idleSpeed;
        navMeshAgent.acceleration = idleSpeed;
        
        while (currentState == enemyState.Idle)
        {
            reachIdlePointCoroutine = ReachRandomIdlePoint();
            yield return reachIdlePointCoroutine;
        }
    }

    private IEnumerator ReachRandomIdlePoint()
    {
        Vector3 destinationPoint = GetRandomDestinationPoint();
        navMeshAgent.SetDestination(destinationPoint);
        yield return new WaitWhile(() => navMeshAgent.remainingDistance > .5f);
    }
    
    private Vector3 GetRandomDestinationPoint()
    {
        int randomIndex = Random.Range(0, idleDestinationPoints.Count - 1); // Zufälliger Index
        randomIndex = (currentIdlePointIndex + randomIndex + 1) % idleDestinationPoints.Count;  //makes sure the same index is not picked twice

        currentIdlePointIndex = randomIndex;
        return idleDestinationPoints[randomIndex];
    }
    #endregion

    #region attack
    private IEnumerator Attack()
    {
        //the startEnemy invokes the foundPlayer Event before other enemies have a chance 
        //assign themselves to the event, Wait a frame to avoid this case
        if(isStartEnemy)
            yield return null;
        
        //If there is no clear sight to Player try to reach him
        if (!HasClearSightToPlayer())
        {
            if(reachPlayerCoroutine != null)
                StopCoroutine(reachPlayerCoroutine);
            
            reachPlayerCoroutine = TryToReachPlayer();
            StartCoroutine(reachPlayerCoroutine);
            yield break;
        }

        AttackPreparation();

        while (!finishedAttack)
        {
            LookAtPlayer();
            navMeshAgent.SetDestination(playerTransform.position);
            yield return null;
        }

        finishedAttack = false;
        StartCoroutine(FollowPlayer());
    }

    private void AttackPreparation()
    {
        currentState = enemyState.Attack;
        animator.CrossFade(attackClip.name,0);
        EnemyManager.foundPlayer.Invoke();
        StopCoroutine(reachIdlePointCoroutine);

        mouthCollider.enabled = true;
        attackPlayerCollider.enabled = false;

        mainAudioSource.PlayRandomOneShot(growlAudioData);
        mainAudioSource.PlayOneShotPitched(waterSplashClipData);

        musicAudioSource.volume = .35f;
        musicAudioSource.Play();
        GameVariables.instance.player.musicAudioSource.volume = 0;
        navMeshAgent.SetDestination(playerTransform.position);
    }

    private bool HasClearSightToPlayer()
    {
        Vector3 vecToPlayer = playerTransform.position - spotLight.transform.position;
        bool gotSight = !Physics.Raycast(spotLight.transform.position, vecToPlayer.normalized,out RaycastHit hit, vecToPlayer.magnitude,
            groundObstacleLayer);

        return gotSight;
    }
    private void LookAtPlayer()
    {
        Vector3 direction = (playerTransform.position - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(direction);
    }


    //tries to reach player as long as its inside the followPlayerRadius
    IEnumerator TryToReachPlayer()
    {
        currentState = enemyState.TryReachPlayer;
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        while (currentState == enemyState.TryReachPlayer && distanceToPlayer < followPlayerRadius)
        {
            navMeshAgent.SetDestination(playerTransform.position);

            if (distanceToPlayer < attackPlayerCollider.radius && HasClearSightToPlayer())
            {
                StartCoroutine(Attack());
                yield break;
            }
            
            yield return null;
            distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position); 
        }

        reachPlayerCoroutine = null;
        yield return ReturnToSearchArea();
    }
    #endregion

    #region follow player
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

            float distancePercentage = Mathf.Clamp01((distanceToPlayer - 30) / followPlayerRadius);
            spotLight.intensity = Mathf.Lerp(lightIntensityCloseToPlayer, lightIntensityFarFromPlayer, distancePercentage);
            musicAudioSource.volume = distancePercentage * -1 + 1;
            
            yield return null;

            distanceToPlayer = Vector3.Distance(playerTransform.position, transform.position);
        }

        mouthCollider.enabled = false;
        spotLight.enabled = false;
        musicAudioSource.Stop();
        GameVariables.instance.player.musicAudioSource.volume = .5f;
        EnemyManager.lostPlayer.Invoke();

        if (isStartEnemy)
        {
            animator.enabled = false;
            gameObject.SetActive(false);
        }
        
        yield return ReturnToSearchArea();
    }
    
    //return to searchArea and disable yourself while returning
    IEnumerator ReturnToSearchArea()
    {
        DiveUnderWater();
        yield return ReachRandomIdlePoint();
        SurfaceFromWater();
        StartCoroutine(Idle());
    }
    #endregion
    
    private void DiveUnderWater()
    {
        if(currentState == enemyState.Attack)
            return;
        
        // Debug.Log(name + " got disabled");
        animator.CrossFade(diveIntoWaterClip.name, 0);
        attackPlayerCollider.enabled = false;
    }

    private void SurfaceFromWater()
    {
        if(currentState == enemyState.FollowPlayer)
            return;
        
        // Debug.Log(name + " got enabled");
        animator.CrossFade(surfaceFromWaterClip.name, 0);
        attackPlayerCollider.enabled = true;
    }

    #region Collision
    private void OnTriggerEnter(Collider other)
    {
        if (!other.tag.Equals("Player"))
            return;

        if(currentState == enemyState.Idle)
            StartCoroutine(Attack());
        else if (currentState > 0)
        {
            musicAudioSource.Stop();
            GameVariables.instance.player.PlayDeathSound();
            GameManager.instance.StopGame();
        }
    }
    #endregion

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