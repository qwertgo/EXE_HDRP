using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class PlayerController : MonoBehaviour, PlayerInput.IP_ControlsActions
{
    private enum PlayerState
    {
        Running,
        Drifting,
        Jumping,
        Falling,
        DriftJumping,
        DriftFalling
    }

    [Header("Base Movement Values")]
    [SerializeField] private float acceleration;
    [SerializeField] private float maxTurnSpeed;
    [SerializeField] private float minTurnSpeed;
    public float baseMaxSpeed;
    [SerializeField] private float traction;
    [SerializeField] private float adjustToGroundSlopeSpeed = 3;
    [SerializeField] private float animationSpeed = .1f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask obstacleLayer;

    private float currentMaxSpeed;

    [Header("Jumping/ In Air")]
    [SerializeField] protected float jumpForce;
    [SerializeField] protected float gravitationMultiplier;
    [SerializeField] private float jumpBuffer = .2f;
    [SerializeField] private float coyoteTime = .2f;
    private float coyoteTimeCounter = 0;

    [Header("Drifting")] 
    [SerializeField] private float driftTurnSpeed;
    [SerializeField] private float driftMaxTurnAbility = 30;
    [SerializeField] private float tractionWhileDrifting;
    [SerializeField] private float driftCooldown;
    [SerializeField] private Gradient defaultParticleColor;
    [SerializeField] private Gradient boostParticleColor;
    private float outerDriftRadius;
    private float currentDriftRotation;
    private float driftBoostPercentage;
    private bool lockDriftingSlowMoBoost;
    private bool lockDriftingCooldown;
    
    [Header("Boost")]
    public float boostForce;
    // [SerializeField] private float boostSubtractPerSecond;
    [SerializeField] private float boostDuration = 1f;
    [SerializeField] private float boostLerpPerSecond;
    [SerializeField] private float timeToGetBoost;
    [SerializeField] private AnimationCurve boostCurve;
    private float boostPercentagePerSecond;

    [Header("Camera")] 
    [SerializeField] private float lookAtMoveAmount;
    [SerializeField] private float horizontalLerpSpeed;
    [SerializeField] private float cameraYawDamping;
    [SerializeField] private float cameraDriftYawDamping;
    [SerializeField] private float maxDutchTilt;

    [FormerlySerializedAs("turnToEnemySpeed")]
    [Header("EnemyAttack / Slo Mo Boost")] 
    [SerializeField] private float turnToEnemyTime = 10;
    [SerializeField] private float timeToWaitTillBoost = 1.5f;

    private float horizontal;
    private float driftHorizontal;
    private float horizontalLerpTo;
    private float currentTraction;
    private float maxSpeedOriginal;
    private float timeUntilNextWalkSound;

    private bool isGrounded;
    private bool isFalling;
    private bool isDrifting;
    private bool isBreaking;
    private bool isInAir = true;
    private bool justStartedJumping;
    private bool _isDriftingRight;
    private bool arrivedAtDriftPeak;
    private bool stayAtCurrentVelocity;
    private bool getSchilfBoost;

    private bool lockSteering;
    public bool tutorialDriftLock;

    [Header("References")]
    [SerializeField] protected Transform playerVisuals;
    [SerializeField] private Transform tonguePoint;
    [FormerlySerializedAs("lookAt")] [SerializeField] private Transform cameraLookAt;
    [FormerlySerializedAs("groundSlopeRef")] [SerializeField] private Transform rotationReference;
    [SerializeField] private ParticleSystem groundParticles;
    [SerializeField] private Material waterTrailMaterial;
    [SerializeField] private GameObject waterVFX;
    [SerializeField] private Material material;
    [SerializeField] protected DriftPointContainer rightDriftPointContainer;
    [SerializeField] protected DriftPointContainer leftDriftPointContainer;
    [SerializeField] private Transform playerLookAt;
    [SerializeField] private GameObject waterTrail;
     

    [Header("Animations")] 
    [SerializeField] private AnimationClip runningClip;
    [SerializeField] private AnimationClip runningOpenMouthClip;
    [SerializeField] private AnimationClip jumpingClip;
    [SerializeField] private AnimationClip jumpingOpenMouthClip;
    [SerializeField] private AnimationClip tongueShoot;
    [SerializeField] private AnimationClip tongueReturn;
    [SerializeField] private MultiAimConstraint headRotationConstraint;
    
    [Header("Audio Sources")] 
    [SerializeField] private AudioSource mainAudioSource;
    [SerializeField] private AudioSource tongueAudioSource;
    [SerializeField] private AudioSource walkingAudioSource;
    public AudioSource musicAudioSource;
    [SerializeField] private AudioSource playerAudioSource;

    [Header("AudioClipData Multiple")] 
    [SerializeField] private AudioClipDataMultiple landingAudioData;
    [SerializeField] private AudioClipDataMultiple tongueAudioData;
    [SerializeField] private AudioClipDataMultiple grassAudioData;
    [SerializeField] private AudioClipDataMultiple obstacleCrashAudioData;
    [SerializeField] private AudioClipDataMultiple playerBoostAudioData;
    [SerializeField] private AudioClipDataMultiple playerJumpAudioData;
    [SerializeField] private AudioClipDataMultiple playerDeathAudioData;
    [SerializeField] private AudioClipDataMultiple playerCrashAuaAudioData;
    [SerializeField] private AudioClipDataMultiple playerFoundEnemyAudioData;


    [Header("Audioclips")]
    [SerializeField] private AudioClip walkingOnWaterClip;
    [SerializeField] private AudioClip walkingOnGrassClip;
    [SerializeField] private AudioClip winClip;
    

    private float storedVelocityMagnitude;
    public Rigidbody rb { get; private set; }
    private Transform currentDriftPoint;
    private PlayerInput controls;
    private CinemachineVirtualCamera virtualCamera;
    private CinemachineTransposer cinemachineTransposer;
    private SphereCollider sphereCollider;
    private Animator animator;
    private Animator tongueAnimator;
    private PlayerState currentState = PlayerState.Running;
    private Quaternion rotationAtDriftStart;
    private GameVariables gameVariables => GameVariables.instance;
    private HighScoreCounter highScoreCounter => gameVariables.highScoreCounter;
    private ScoreCollider scoreCollider;

    private List<IEnumerator> boostCoroutines = new();

    #region Instantiation and Destroying ------------------------------------------------------------------------------------------------------------------------------------
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        scoreCollider = GetComponentInChildren<ScoreCollider>();
        sphereCollider = GetComponent<SphereCollider>();
        animator = playerVisuals.GetComponentInChildren<Animator>();
        tongueAnimator = tonguePoint.GetComponent<Animator>();
        virtualCamera = gameVariables.virtualCamera;
        cinemachineTransposer = virtualCamera.GetCinemachineComponent<CinemachineTransposer>();
        currentMaxSpeed = baseMaxSpeed;
        boostPercentagePerSecond = 1 / timeToGetBoost;

        if (controls == null)
        {
            controls = new PlayerInput();
            controls.Enable();
            controls.P_Controls.SetCallbacks(this);
        }

        currentTraction = traction;

        rightDriftPointContainer.SetRightContainer();
        gameVariables.onPause.AddListener(PauseMe);
        gameVariables.twoMinutesPassed.AddListener(ShortenSlowMoBoost);
        
        LoadAllAudioCLips();
    }

    private void LoadAllAudioCLips()
    {
        landingAudioData.LoadClips();
        tongueAudioData.LoadClips();
        grassAudioData.LoadClips();
        obstacleCrashAudioData.LoadClips();
        playerBoostAudioData.LoadClips();
        playerJumpAudioData.LoadClips();
        playerDeathAudioData.LoadClips();
        playerCrashAuaAudioData.LoadClips();
        playerFoundEnemyAudioData.LoadClips();
    }

    private void OnDestroy()
    {
        material.SetFloat("_fireflyCount", 0);

        if (controls != null)
        {
            controls.Disable();
            controls.P_Controls.RemoveCallbacks(this);
        }

    }
    #endregion

    #region Physics ------------------------------------------------------------------------------------------------------------------------------------
    void FixedUpdate()
    {
        if(gameVariables.isPaused)
            return;
        
        UpdateState();
        Accelerate();
        // PlayWalkSound();
        
    
        rb.velocity += (isFalling ? Physics.gravity * gravitationMultiplier : Physics.gravity) * Time.fixedDeltaTime;
        animator.SetFloat("runningSpeed", Mathf.Max(.5f, rb.velocity.magnitude * animationSpeed));
        storedVelocityMagnitude = rb.velocity.magnitude;
    }

    private void Update()
    {
        if(gameVariables.isPaused)
            return;
        
        Steer();
        // ReduceMaxSpeed();
        // Debug.Log($"speed: {rb.velocity.magnitude}, maxSpeed: {currentMaxSpeed}");
    }

    #endregion

    #region States ------------------------------------------------------------------------------------------------------------------------------------
    void UpdateState()
    {
        bool wasFallingLastFrame = !isGrounded && isFalling;
        isGrounded = IsGrounded();
        isFalling = IsFalling();

        // bool stateEqualsFalling = currentState == PlayerState.Falling || (currentState == PlayerState.DriftFalling && !justStartedJumping);

        //started falling
        if (!isGrounded && isFalling && !isInAir)
        {
            StartCoroutine(CoyoteTime());
            currentState = isDrifting? PlayerState.DriftFalling : PlayerState.Falling;
            animator.CrossFade(jumpingClip.name, .4f);
            walkingAudioSource.Stop();
            waterVFX.SetActive(false);
            isInAir = true;
        }
        else if (isInAir && !justStartedJumping && isGrounded)  //just landed
        {
            if (isDrifting)
            {
                currentState = PlayerState.Drifting;
                animator.CrossFade(runningOpenMouthClip.name, .1f);
            }
            else
            {
                currentState = PlayerState.Running;
                animator.CrossFade(runningClip.name, .1f);
            }
            
            waterVFX.SetActive(true);
            mainAudioSource.PlayRandomOneShot(landingAudioData);
            isInAir = false;

            highScoreCounter.playerIsInAir = false;
        }

        justStartedJumping = false;
    }

    private IEnumerator CoyoteTime()
    {
        coyoteTimeCounter = 1f;

        yield return DOTween.To(() => coyoteTimeCounter, x => coyoteTimeCounter = x, 0, coyoteTime).WaitForCompletion();

        coyoteTimeCounter = -1;
    }

    #region Steering and Drifting ------------------------------------------------------------------------------------------------------------------------------------
    void Steer()
    {
        if(!isDrifting)
        {
            //steer
            float currentTurnSpeed = Mathf.Lerp(maxTurnSpeed, minTurnSpeed, rb.velocity.magnitude / currentMaxSpeed);
            playerVisuals.Rotate(playerVisuals.up, horizontal * Time.deltaTime * currentTurnSpeed);
            
            AdjustToGroundSlope();

            //update container to get closest drift point
            leftDriftPointContainer.UpdateMe();
            rightDriftPointContainer.UpdateMe();
        }

        //move LookAt Object
        horizontalLerpTo = Mathf.Lerp(horizontalLerpTo, horizontal + driftHorizontal, Time.deltaTime * horizontalLerpSpeed);
        cameraLookAt.position = playerVisuals.position + playerVisuals.right * (horizontalLerpTo * lookAtMoveAmount);

        virtualCamera.m_Lens.Dutch = horizontalLerpTo * maxDutchTilt;
        
        //traction
        float gravity = rb.velocity.y;
        Vector3 velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        float t = 1 - Mathf.Pow(currentTraction, Time.deltaTime);
        rb.velocity = Vector3.Lerp(velocity.normalized, playerVisuals.forward, t) * velocity.magnitude;
        rb.velocity += new Vector3(0, gravity, 0);
    }

    private Vector3 wantedForward;
    private bool useWantedForward;

    public void StartRedirection(Vector3 newForward, float cutOffPoint, bool giveBoost = false)
    {
        if(Vector3.Dot(transform.forward, newForward) > cutOffPoint)
        {
            useWantedForward = true;
            wantedForward = newForward;
            if (giveBoost)
                StartBoost(boostForce * .25f); 
        }
    }

    public void StopRedirection()
    {
        useWantedForward = false;
        wantedForward = playerVisuals.forward;
    }
    
    private void StartDrifting()
    {
        isDrifting = true;
        headRotationConstraint.weight = 1;
        rotationAtDriftStart = playerVisuals.rotation;
        currentTraction = tractionWhileDrifting;
        driftBoostPercentage = 0;
        driftHorizontal = _isDriftingRight ? .5f : -.5f;
        
        
        switch (currentState)
        {
            case PlayerState.Jumping:
                currentState = PlayerState.DriftJumping;
                animator.CrossFade(jumpingOpenMouthClip.name, .1f);
                break;
            case PlayerState.Falling:
                currentState = PlayerState.DriftFalling;
                animator.CrossFade(jumpingOpenMouthClip.name, .1f);
                break;
            default:
                currentState = PlayerState.Drifting;
                animator.CrossFade(runningOpenMouthClip.name, .1f);
                break;
        }

        tongueAnimator.CrossFade(tongueShoot.name, 0f);
        tongueAudioSource.PlayRandomAudioVariation(tongueAudioData);

        outerDriftRadius = Vector3.Distance(currentDriftPoint.position, transform.position);

        //change how fast the camera copies the rotation of the player
        cinemachineTransposer.m_YawDamping = cameraDriftYawDamping;

        StartCoroutine(DriftCooldown());
        StartCoroutine(Drift());
    }

    private void StopDrifting()
    {
        isDrifting = false;
        arrivedAtDriftPeak = false;
        
        tongueAnimator.CrossFade(tongueReturn.name, 0f);
        tongueAudioSource.PlayRandomAudioVariation(tongueAudioData, true);
        
        driftHorizontal = 0;
        horizontal = 0;
        currentDriftRotation = 0;
        headRotationConstraint.weight = 0;
        currentTraction = traction;
        
        switch (currentState)
        {
            case PlayerState.DriftJumping :
                currentState = PlayerState.Jumping;
                animator.CrossFade(jumpingClip.name, .1f);
                break;
            case PlayerState.DriftFalling:
                currentState = PlayerState.Falling;
                animator.CrossFade(jumpingClip.name, .1f);
                break;
            default:
                currentState = PlayerState.Running;
                animator.CrossFade(runningClip.name, .1f);
                break;
        }
        
        
        playerLookAt.position = new Vector3(transform.position.x, 1.5f, transform.position.z) + playerVisuals.forward;

        float currentBoostAmount = boostForce * driftBoostPercentage;
        StartBoost(currentBoostAmount);

        highScoreCounter.AddToScore(HighScoreCounter.ScoreType.DriftDash, driftBoostPercentage);

        ChangeParticleColor(defaultParticleColor, true);

        //change how fast the camera copies the rotation of the player
        cinemachineTransposer.m_YawDamping = cameraYawDamping;
    }
    
    private IEnumerator Drift()
    {
        rotationReference.rotation = rotationAtDriftStart;
        while (isDrifting)
        {
            //variables needed in whole method
            Vector3 vecToDriftPoint = currentDriftPoint.position - transform.position;
            Vector2 vecToDriftPoint2D = new Vector2(vecToDriftPoint.x, vecToDriftPoint.z);
            
            //tongueStretchFactor indicates how much the player should drift
            float tongueStretchFactor = arrivedAtDriftPeak ? 1 : GetTongueStretchFactor(vecToDriftPoint2D);

            //get the rotation at the start of the drift with the current up vector
            Quaternion lerpFromRotation = Quaternion.LookRotation(rotationReference.forward, playerVisuals.up);
            
            //get the direction the player should face at the max tongueStretchFactor
            Vector3 dirToDriftPoint = new Vector3(vecToDriftPoint.x, 0, vecToDriftPoint.z).normalized;
            Quaternion lerpToRotation = GetWantedDriftRotation(dirToDriftPoint);

            playerVisuals.rotation = Quaternion.Lerp(lerpFromRotation, lerpToRotation, tongueStretchFactor);
            playerLookAt.position = currentDriftPoint.position;

            AdjustToGroundSlope();
            TurnVelocityToPlayerForward();
            ChargeDriftBoost(tongueStretchFactor);
            UpdateTongueVisuals(vecToDriftPoint);

            yield return null;
        }
    }

    private IEnumerator DriftCooldown()
    {
        lockDriftingCooldown = true;
        yield return new WaitForSeconds(driftCooldown);
        lockDriftingCooldown = false;
    }

    private Quaternion GetWantedDriftRotation(Vector3 dirToDriftPoint)
    {
        if (horizontal == 0)
            currentDriftRotation = Mathf.Lerp(currentDriftRotation, 0, Time.deltaTime);
        else
            currentDriftRotation += horizontal * driftTurnSpeed * Time.deltaTime;
        
        if (currentDriftRotation > 0)
            currentDriftRotation = Mathf.Min(currentDriftRotation, driftMaxTurnAbility);
        else
            currentDriftRotation = Mathf.Max(currentDriftRotation, -driftMaxTurnAbility);

        Vector3 wantedForward = _isDriftingRight ? dirToDriftPoint.RotateLeft90Deg() : dirToDriftPoint.RotateRight90Deg();
        wantedForward = Quaternion.AngleAxis(currentDriftRotation, Vector3.up) * wantedForward;
        return Quaternion.LookRotation(wantedForward, playerVisuals.up);
    }
    
    private float GetTongueStretchFactor( Vector2 vecToDriftPoint2D)
    {
        //get distance to drift point
        float distanceToDriftPoint = Vector3.Distance(currentDriftPoint.position, transform.position);

        //get rotation between playerForward and vector that goes to drift point (clamped a 90 degrees)
        Vector2 forward2D = new Vector2(playerVisuals.forward.x, playerVisuals.forward.z);

        float angleToDriftPoint = Vector2.SignedAngle(forward2D, vecToDriftPoint2D);
        angleToDriftPoint = Mathf.Min(Mathf.Abs(angleToDriftPoint), 80);

        //get tongueStretchFactor out of distance to drift point and angle 
        float distanceFactor =  Mathf.Clamp01(distanceToDriftPoint / outerDriftRadius - .5f ) * 2;
        float angleFactor = Mathf.Clamp01(angleToDriftPoint / 80 - .5f ) * 2;
        float tongueStretchFactor = distanceFactor * angleFactor;
        tongueStretchFactor = Mathf.Clamp01(tongueStretchFactor);
        
        // Debug.Log($"tongueStretch: {tongueStretchFactor}, distance: {distanceFactor}, angle: {angleFactor}");

        if (tongueStretchFactor > .99f)
            arrivedAtDriftPeak = true;

        return tongueStretchFactor;
    }

    private void TurnVelocityToPlayerForward()
    {
        //make the velocity face the same direction as the player
        if (isGrounded && !(rb.velocity.y > .5f))
        {
            rb.velocity = playerVisuals.forward * rb.velocity.magnitude;
            // Debug.Log("ground Drifting");
        }
        else
        {
            float yVelocity = rb.velocity.y;
            Vector3 velocity2D = new Vector2(rb.velocity.x, rb.velocity.z);
            rb.velocity = playerVisuals.forward * velocity2D.magnitude + new Vector3(0, yVelocity,0);
        }
    }

    private void ChargeDriftBoost(float tongueStretchFactor)
    {
        if(tongueStretchFactor < .25f)
            return;
        
        driftBoostPercentage += Time.deltaTime * boostPercentagePerSecond;
        driftBoostPercentage = Mathf.Min(driftBoostPercentage, 1);
        Gradient tmpGradient = defaultParticleColor.LerpNoAlpha(boostParticleColor, driftBoostPercentage);
        ChangeParticleColor(tmpGradient);
    }

    void AdjustToGroundSlope()
    {
        Quaternion wantedRotation;
        Vector3 forward = useWantedForward ? wantedForward : playerVisuals.forward;

        if (!isInAir)
        {
            //if first Raycast does not hit anything cast a second longer one straight down
            if (!Physics.Raycast(transform.position, -playerVisuals.up, out RaycastHit groundHit, sphereCollider.radius + .5f, groundLayer))
            {
                Physics.Raycast(transform.position, Vector3.down, out groundHit, sphereCollider.radius + 2, groundLayer);
            }

            forward = Vector3.ProjectOnPlane(forward, groundHit.normal);
            wantedRotation = Quaternion.LookRotation(forward, groundHit.normal);
            
            rb.velocity = Vector3.ProjectOnPlane(rb.velocity, groundHit.normal);
        }
        else
        {
            forward = Vector3.ProjectOnPlane(forward, Vector3.up);
            wantedRotation = Quaternion.LookRotation(forward, Vector3.up);
        }

        playerVisuals.rotation = Quaternion.Lerp(playerVisuals.rotation, wantedRotation, Time.deltaTime * adjustToGroundSlopeSpeed);
    }
    
    void ChangeParticleColor(Gradient color, bool invertGradient = false)
    {
        if (invertGradient)
        {
            waterTrailMaterial.SetColor("_Color1", color.colorKeys[1].color);
            waterTrailMaterial.SetColor("_Color2", color.colorKeys[0].color);
        }
        else
        {
            waterTrailMaterial.SetColor("_Color1", color.colorKeys[0].color);
            waterTrailMaterial.SetColor("_Color2", color.colorKeys[1].color);
        }
        ParticleSystem.ColorOverLifetimeModule colOverLifeTime = groundParticles.colorOverLifetime;
        colOverLifeTime.color = color;
    }

    void UpdateTongueVisuals(Vector3 vecToDriftPoint)
    {
        tonguePoint.forward = vecToDriftPoint.normalized;
        Vector3 scale = tonguePoint.localScale;
        tonguePoint.localScale = new Vector3(scale.x, scale.y, vecToDriftPoint.magnitude);
    }
    #endregion
    #endregion
    
    #region Acceleration ------------------------------------------------------------------------------------------------------------------------------------
    void Accelerate()
    {
        //acceleration
        if (!isBreaking)
            rb.velocity += acceleration * Time.fixedDeltaTime * playerVisuals.forward;

        //limit max speed
        if (rb.velocity.magnitude > currentMaxSpeed)
            rb.velocity = rb.velocity.normalized * currentMaxSpeed;
    }
    void Boost(float amount)
    {
        currentMaxSpeed += amount;
        rb.velocity = playerVisuals.forward * currentMaxSpeed;
    }

    private void StartBoost(float boostAmount)
    {
        if (boostAmount >= boostForce && Random.Range(0f, 1f) < .15f)
            playerAudioSource.PlayRandomOneShot(playerBoostAudioData);

        GameManager instance = GameManager.instance;
        if (instance.stoppedGame || instance.gameIsPaused)
            return;

        boostCoroutines.Add(NewBoost(boostAmount));
        StartCoroutine(boostCoroutines.Last());
    }

    private IEnumerator NewBoost(float boostAmount)
    {
        float elapsedTime = 0;
        float addedBoost = 0;
        float velocityMagnitude;

        while (elapsedTime <= 1)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / boostDuration;

            currentMaxSpeed -= addedBoost;
            velocityMagnitude = rb.velocity.magnitude - addedBoost;
            
            addedBoost = boostCurve.Evaluate(t) * boostAmount;
            
            currentMaxSpeed += addedBoost;
            velocityMagnitude += addedBoost;
            rb.velocity = rb.velocity.normalized * velocityMagnitude;
            yield return null;
        }

        if (boostCoroutines.Count == 1)
            currentMaxSpeed = baseMaxSpeed;
        else
        {
            currentMaxSpeed -= addedBoost;
            velocityMagnitude = rb.velocity.magnitude - addedBoost;
            rb.velocity = rb.velocity.normalized * velocityMagnitude;
        }
        
        boostCoroutines.RemoveAt(0);
    }
    
    public void StartSlowMoBoost(EnemyMovement enemy)
    {
        if(isDrifting)
            StopDrifting();
        
        lockDriftingSlowMoBoost = true;
        playerAudioSource.PlayRandomOneShot(playerFoundEnemyAudioData);
        StartCoroutine(SlowMoBoost(enemy));
    }
    IEnumerator SlowMoBoost(EnemyMovement enemy)
    {
        bool isStartEnemy = enemy.isStartEnemy;
        float turnSpeedScale = isStartEnemy ? 60 : 30;
        Time.timeScale = isStartEnemy ? .025f : .05f;
        maxTurnSpeed *= turnSpeedScale;
        rb.velocity = Vector3.zero;
        cinemachineTransposer.m_YawDamping = 0;

        Vector3 vecToEnemy = enemy.transform.position - transform.position;
        vecToEnemy.Scale(new Vector3(1,0,1));
        vecToEnemy.Normalize();

        yield return TurnPlayerInNewDirection(vecToEnemy, turnToEnemyTime * Time.timeScale);


        float boostDelay = isStartEnemy ? timeToWaitTillBoost * 2 : timeToWaitTillBoost;
        yield return new WaitForSecondsRealtime(boostDelay);
        
        Time.timeScale = 1;
        maxTurnSpeed /= turnSpeedScale;
        cinemachineTransposer.m_YawDamping = cameraYawDamping;
        lockDriftingSlowMoBoost = false;
        
        StopBoosting();
        rb.velocity = playerVisuals.forward * currentMaxSpeed;
        boostCoroutines.Add(NewBoost(boostForce));
        StartCoroutine(boostCoroutines.Last());
    }
    #endregion
    
    #region Decceleration ------------------------------------------------------------------------------------------------------------------------------------
    private void ReduceMaxSpeed()
    {
        float t = 1f - Mathf.Pow(boostLerpPerSecond, Time.deltaTime);
        currentMaxSpeed = Mathf.Lerp(currentMaxSpeed, baseMaxSpeed, t);

        // currentMaxSpeed -= Time.deltaTime * boostSubtractPerSecond;
        // currentMaxSpeed = Mathf.Max(baseMaxSpeed, currentMaxSpeed);
    }
    
    public void SlowDown()
    {
        mainAudioSource.PlayRandomOneShot(grassAudioData);
        StartCoroutine(SlowDownCoroutine());
    }
    private IEnumerator SlowDownCoroutine()
    {
        StopBoosting();
        
        isBreaking = true;
        float startSpeed = rb.velocity.magnitude;
        float endSpeed = 10;
        float t = 0;

        while (t < 1)
        {
            float currentSpeed = Mathf.Lerp(startSpeed, endSpeed, t);
            rb.velocity = rb.velocity.normalized * currentSpeed;
            yield return null;
            t += Time.deltaTime * 2;
        }

        isBreaking = false;
    }

    private void StopBoosting()
    {
        foreach (var boost in boostCoroutines)
        {
            StopCoroutine(boost);
        }

        currentMaxSpeed = baseMaxSpeed;
        boostCoroutines.Clear();
    }
    #endregion

    #region Collider ------------------------------------------------------------------------------------------------------------------------------------
    
    private void OnCollisionEnter(Collision other)
    {
        var collidedWithLayer = other.gameObject.layer;
        scoreCollider?.PlayerCollidedWithCollider(collidedWithLayer);
        
        if (collidedWithLayer.IsInsideMask(obstacleLayer))
            BounceOfObstacle(other);
        else if (collidedWithLayer.IsInsideMask(groundLayer))
        {
            rb.velocity = storedVelocityMagnitude * playerVisuals.forward;
            if (other.gameObject.tag.Equals("Schilf"))
            {
                StartCoroutine(StayAtVelocity());

                walkingAudioSource.clip = walkingOnGrassClip;
                walkingAudioSource.volume = .1f;
                walkingAudioSource.Play();
                
                waterTrail.SetActive(false);
            }
            else
            {
                walkingAudioSource.clip = walkingOnWaterClip;
                walkingAudioSource.volume = .1f;
                walkingAudioSource.Play();
            }
        }
    }

    private void OnCollisionExit(Collision other)
    {
        GameObject go = other.gameObject;
        if (go.layer.IsInsideMask(groundLayer) && go.tag.Equals("Schilf"))
        {
            stayAtCurrentVelocity = false;
            waterTrail.SetActive(true);
        }
    }

    IEnumerator StayAtVelocity()
    {
        if(getSchilfBoost)
            StartBoost(boostForce);
        
        stayAtCurrentVelocity = true;
        getSchilfBoost = false;
        while (stayAtCurrentVelocity)
        {
            rb.velocity = rb.velocity.normalized * storedVelocityMagnitude;
            yield return null;
        }

        yield return new WaitForSeconds(1f);
        getSchilfBoost = true;
    }

    private void BounceOfObstacle(Collision other)
    {
        if (isDrifting)
            StopDrifting();
        
        // boostCoroutines.Add(NewBoost(boostForce));
        // StartCoroutine(boostCoroutines.Last());
        
        mainAudioSource.PlayRandomOneShot(obstacleCrashAudioData);
        if(Random.Range(0f, 1f) < .3f)
            playerAudioSource.PlayRandomOneShot(playerCrashAuaAudioData);
        
        
        //get vector to other collider rotate it 90 degrees and turn player in that direction
        Collider col = other.collider;
        Vector3 colliderPos = col is MeshCollider ? other.contacts[0].point : col.ClosestPoint(transform.position);

        Vector3 vecToCollider = colliderPos - transform.position;
        vecToCollider.Scale(new Vector3(1,0,1));
        float signedAngle = Vector3.SignedAngle(playerVisuals.forward, vecToCollider, Vector3.up);

        float rotation = signedAngle > 0 ? -90 : 90;
        Vector3 newForward = Quaternion.AngleAxis(rotation, playerVisuals.up) * vecToCollider.normalized;
        rb.velocity = newForward * storedVelocityMagnitude;
        
        StartCoroutine(TurnPlayerInNewDirection(newForward, .1f));
    }

    IEnumerator TurnPlayerInNewDirection(Vector3 newForward, float timeToTurn)
    {
        float t = 0;
        Vector3 startForward = playerVisuals.forward;
        lockSteering = true;
        float speed = 1 / timeToTurn;
        
        while (t < 1)
        {
            Vector3 tmpForward = Vector3.Lerp(startForward, newForward, t);
            playerVisuals.rotation = Quaternion.LookRotation(tmpForward, playerVisuals.up);
            
            yield return null;
            t += Time.deltaTime * speed;
        }

        playerVisuals.rotation = Quaternion.LookRotation(newForward, playerVisuals.up);
        lockSteering = false;
    }

    #endregion

    #region Bools ------------------------------------------------------------------------------------------------------------------------------------
    bool IsGrounded()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, sphereCollider.radius + .01f, groundLayer);
        return colliders.Length > 0;
    }

    bool IsFalling()
    {
        if (currentState == PlayerState.Jumping || currentState == PlayerState.DriftJumping)
            return rb.velocity.y < -.5f;

        bool hitGround = Physics.Raycast(transform.position, -playerVisuals.up, sphereCollider.radius + 1f, groundLayer);
        return rb.velocity.y < -.5f || !hitGround;

    }
    #endregion
    
    #region Various Methods ------------------------------------------------------------------------------------------------------------------------------------
    public void Die()
    {
        PlayDeathSound();
        StopBoosting();
        StopAllCoroutines();
        walkingAudioSource.Stop();
        musicAudioSource.Play();
        rb.velocity = Vector3.zero;
        rb.isKinematic = true;

        animator.speed = 0;
        enabled = false;
    }

    private void PlayDeathSound()
    {
        playerAudioSource.PlayRandomOneShot(playerDeathAudioData);
    }

    private void ShortenSlowMoBoost()
    {
        timeToWaitTillBoost *= .6f;
        turnToEnemyTime *= .6f;
    }

    void PauseMe()
    {
        StartCoroutine(WhilePaused());
    }

    IEnumerator WhilePaused()
    {
        controls.Disable();
        controls.P_Controls.RemoveCallbacks(this);
        
        Vector3 velocity = rb.velocity;
        rb.velocity = Vector3.zero;
        rb.isKinematic = true;

        yield return new WaitWhile(() => gameVariables.isPaused);

        rb.isKinematic = false;
        rb.velocity = velocity;
        
        controls = new PlayerInput();
        controls.Enable();
        controls.P_Controls.SetCallbacks(this);
    }

    #endregion

    #region Input System ------------------------------------------------------------------------------------------------------------------------------------
    public void OnSteer(InputAction.CallbackContext context)
    {
        if (!lockSteering)
        {
            horizontal = context.ReadValue<Vector2>().x;
        }
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (!context.started || GameManager.instance.gameIsPaused)
            return;

        if (isGrounded || coyoteTimeCounter > 0)
            StartJumping();
        else
            StartCoroutine(JumpBuffer());
    }

    private void StartJumping()
    {
        currentState = currentState == PlayerState.Drifting ? PlayerState.DriftJumping : PlayerState.Jumping;
        rb.velocity += playerVisuals.up * jumpForce;
        justStartedJumping = true;
        isInAir = true;
        waterVFX.SetActive(false);
        AnimationClip tmpJumpCLip = isDrifting ? jumpingOpenMouthClip : jumpingClip;
        animator.CrossFade(tmpJumpCLip.name, .2f);
        walkingAudioSource.Stop();
        highScoreCounter.StartInAirScore();


        if (Random.Range(0f, 1f) < .2f)
            playerAudioSource.PlayRandomOneShot(playerJumpAudioData);
    }

    private IEnumerator JumpBuffer()
    {
        yield return null;

        float t = 0;

        while(t <= jumpBuffer)
        {
            if (isGrounded)
            {
                StartJumping();
                yield break;
            }

            t += Time.deltaTime;
            yield return null;
        }
    }

    public void OnLeftDrift(InputAction.CallbackContext context)
    {
        if (context.canceled && isDrifting)
        {
            StopDrifting();
            return;
        }

        if (context.started)
            CheckIfCanStartDrifting(false);

        //DriftPoint tmpDriftPoint = null;
        //bool driftingIsLocked = lockDriftingSlowMoBoost || lockDriftingCooldown;
        //bool canStartDrift = context.started && !isDrifting && leftDriftPointContainer.HasDriftPoint(out tmpDriftPoint);
        
        //if (!driftingIsLocked && canStartDrift)
        //{
        //    _isDriftingRight = false;
        //    currentDriftPoint = tmpDriftPoint.transform;

        //    StartDrifting();
        //}
    }

    public void OnRightDrift(InputAction.CallbackContext context)
    {
        if (context.canceled && isDrifting)
        {
            StopDrifting();
            return;
        }

        if (context.started)
            CheckIfCanStartDrifting(true);

        //DriftPoint tmpDriftPoint = null;
        //bool driftingIsLocked = lockDriftingSlowMoBoost || lockDriftingCooldown || isDrifting;
        //bool canStartDrift = context.started && rightDriftPointContainer.HasDriftPoint(out tmpDriftPoint);

        //if (!driftingIsLocked && canStartDrift)
        //{
        //    _isDriftingRight = true;
        //    currentDriftPoint = tmpDriftPoint.transform;

        //    StartDrifting();
        //}
    }

    public void CheckIfCanStartDrifting(bool driftRight)
    {
        DriftPointContainer driftPointContainer = driftRight ? rightDriftPointContainer : leftDriftPointContainer;

        bool driftingIsLocked = lockDriftingSlowMoBoost || lockDriftingCooldown || isDrifting || tutorialDriftLock;

        if (!driftingIsLocked && driftPointContainer.HasDriftPoint(out DriftPoint tmpDriftPoint))
        {
            _isDriftingRight = driftRight;
            currentDriftPoint = tmpDriftPoint.transform;

            StartDrifting();
        }
    }
    #endregion
}