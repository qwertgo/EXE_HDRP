using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class PlayerController : MonoBehaviour, PlayerInput.IP_ControlsActions
{
    protected enum PlayerState
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
    [SerializeField] private LayerMask ground;

    private float currentMaxSpeed;

    [Header("Jumping/ In Air")]
    [SerializeField] protected float jumpForce;
    [SerializeField] protected float gravitationMultiplier;

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
    [SerializeField] private float boostForce;
    [SerializeField] private float boostSubtractPerSecond;
    [SerializeField] private float boostLerpPerSecond;
    [SerializeField] private float timeToGetBoost;
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

    public int fireflyCount { get; private set; }
    
    private bool isGrounded;
    private bool isFalling;
    private bool isDrifting;
    private bool isBreaking;
    private bool justStartedJumping;
    private bool isDriftingRight;
    private bool arrivedAtDriftPeak;

    private bool lockSteering;
    

    [Header("References")]
    [SerializeField] protected Transform playerVisuals;
    [SerializeField] private Transform tonguePoint;
    [SerializeField] private Material tongueMaterial;
    [FormerlySerializedAs("lookAt")] [SerializeField] private Transform cameraLookAt;
    [FormerlySerializedAs("groundSlopeRef")] [SerializeField] private Transform rotationReference;
    [SerializeField] private ParticleSystem groundParticles;
    [SerializeField] private GameObject waterVFX;
    [SerializeField] private Material material;
    [SerializeField] protected DriftPointContainer rightDriftPointContainer;
    [SerializeField] protected DriftPointContainer leftDriftPointContainer;
    [SerializeField] private AudioSource walkAudioSource;
    [SerializeField] private Transform playerLookAt;

    [Header("Animations")] 
    [SerializeField] private AnimationClip runningClip;
    [SerializeField] private AnimationClip jumpingUpClip;
    [SerializeField] private AnimationClip inAirClip;
    [SerializeField] private AnimationClip tongueShoot;
    [SerializeField] private AnimationClip tongueReturn;
    
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

    #region Instantiation and Destroying ------------------------------------------------------------------------------------------------------------------------------------
    void Start()
    {
        rb = GetComponent<Rigidbody>();
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

    }

    private void OnDestroy()
    {
        material.SetFloat("_fireflyCount", 0);
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
    }

    private void Update()
    {
        if(gameVariables.isPaused)
            return;
        
        Steer();
        ReduceMaxSpeed();
        // Debug.Log($"speed: {rb.velocity.magnitude}, maxSpeed: {currentMaxSpeed}");
    }

    #endregion

    #region States ------------------------------------------------------------------------------------------------------------------------------------
    void UpdateState()
    {
        bool wasFallingLastFrame = !isGrounded && isFalling;
        isGrounded = IsGrounded();
        isFalling = IsFalling();

        bool stateEqualsFalling = currentState == PlayerState.Falling || (currentState == PlayerState.DriftFalling && !justStartedJumping);

        if (!isGrounded && isFalling && !wasFallingLastFrame)
        {
            currentState = isDrifting? PlayerState.DriftFalling : PlayerState.Falling;
            animator.CrossFade(inAirClip.name, .4f);
            waterVFX.SetActive(false);
        }
        else if (stateEqualsFalling && isGrounded)
        {
            currentState = currentState == PlayerState.DriftFalling ? PlayerState.Drifting : PlayerState.Running;
            waterVFX.SetActive(true);
            animator.CrossFade(runningClip.name, .5f);
        }

        justStartedJumping = false;
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
    
    private void StartDrifting()
    {
        isDrifting = true;

        switch (currentState)
        {
            case PlayerState.Jumping:
                currentState = PlayerState.DriftJumping;
                break;
            case PlayerState.Falling:
                currentState = PlayerState.DriftFalling;
                break;
            default:
                currentState = PlayerState.Drifting;
                break;
        }

        tongueAnimator.CrossFade(tongueShoot.name, 0f);

        outerDriftRadius = Vector3.Distance(currentDriftPoint.position, transform.position);

        rotationAtDriftStart = playerVisuals.rotation;
        currentTraction = tractionWhileDrifting;
        driftBoostPercentage = 0;
        driftHorizontal = isDriftingRight ? .5f : -.5f;
        
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
        driftHorizontal = 0;
        horizontal = 0;
        currentDriftRotation = 0;
        currentState = currentState == PlayerState.DriftJumping ? PlayerState.Jumping : PlayerState.Running;
        currentTraction = traction;
        playerLookAt.position = new Vector3(transform.position.x, 1.5f, transform.position.z) + playerVisuals.forward;

        float currentBoostAmount = boostForce * driftBoostPercentage;
        Boost(currentBoostAmount);
        ChangeParticleColor(defaultParticleColor);

        //change how fast the camera copies the rotation of the player
        cinemachineTransposer.m_YawDamping = cameraYawDamping;
    }
    
    IEnumerator Drift()
    {
        rotationReference.rotation = rotationAtDriftStart;
        while (isDrifting)
        {
            //variables needed in whole method
            Vector3 vecToDriftPoint = currentDriftPoint.position - transform.position;
            Vector2 vecToDriftPoint2D = new Vector2(vecToDriftPoint.x, vecToDriftPoint.z);
            
            //tongueStretchFactor indicates how much the player should drift
            float tongueStretchFactor = arrivedAtDriftPeak ? 1 : GetTongeStretchFactor(vecToDriftPoint2D);

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

    IEnumerator DriftCooldown()
    {
        lockDriftingCooldown = true;
        yield return new WaitForSeconds(driftCooldown);
        lockDriftingCooldown = false;
    }

    Quaternion GetWantedDriftRotation(Vector3 dirToDriftPoint)
    {
        if (horizontal == 0)
            currentDriftRotation = Mathf.Lerp(currentDriftRotation, 0, Time.deltaTime);
        else
            currentDriftRotation += horizontal * driftTurnSpeed * Time.deltaTime;
        
        if (currentDriftRotation > 0)
            currentDriftRotation = Mathf.Min(currentDriftRotation, driftMaxTurnAbility);
        else
            currentDriftRotation = Mathf.Max(currentDriftRotation, -driftMaxTurnAbility);

        Vector3 wantedForward = isDriftingRight ? dirToDriftPoint.RotateLeft90Deg() : dirToDriftPoint.RotateRight90Deg();
        wantedForward = Quaternion.AngleAxis(currentDriftRotation, Vector3.up) * wantedForward;
        return Quaternion.LookRotation(wantedForward, playerVisuals.up);
    }
    
    float GetTongeStretchFactor( Vector2 vecToDriftPoint2D)
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

        if (isGrounded)
        {
            //if first Raycast does not hit anything cast a second longer one straight down
            if (!Physics.Raycast(transform.position, -playerVisuals.up, out RaycastHit groundHit, sphereCollider.radius + .5f, ground))
            {
                Physics.Raycast(transform.position, Vector3.down, out groundHit, sphereCollider.radius + 2, ground);
            }

            Vector3 forward = Vector3.ProjectOnPlane(playerVisuals.forward, groundHit.normal);
            wantedRotation = Quaternion.LookRotation(forward, groundHit.normal);
        }
        else
        {
            Vector3 forward = Vector3.ProjectOnPlane(playerVisuals.forward, Vector3.up);
            wantedRotation = Quaternion.LookRotation(forward, Vector3.up);
        }

        playerVisuals.rotation = Quaternion.Lerp(playerVisuals.rotation, wantedRotation, Time.deltaTime * adjustToGroundSlopeSpeed);
    }
    
    void ChangeParticleColor(Gradient color)
    {
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

    #region Acceleration ------------------------------------------------------------------------------------------------------------------------------------
    void Accelerate()
    {
        //acceleration
        if (!isBreaking)
            rb.velocity += acceleration * Time.fixedDeltaTime * playerVisuals.forward;

        //limit max speed
        if (rb.velocity.magnitude > currentMaxSpeed)
        {
            rb.velocity = rb.velocity.normalized * currentMaxSpeed;
        }
    }
    void Boost(float amount)
    {
        currentMaxSpeed += amount;
        rb.velocity = playerVisuals.forward * currentMaxSpeed;
    }
    
    public void StartSlowMoBoost(EnemyMovement enemy)
    {
        StopDrifting();
        lockDriftingSlowMoBoost = true;
        StartCoroutine(SlowMoBoost(enemy));
    }
    IEnumerator SlowMoBoost(EnemyMovement enemy)
    {
        Time.timeScale = .05f;
        maxTurnSpeed *= 15;
        rb.velocity = Vector3.zero;
        cinemachineTransposer.m_YawDamping = 0;

        Vector3 vecToEnemy = enemy.transform.position - transform.position;
        vecToEnemy.Scale(new Vector3(1,0,1));
        vecToEnemy.Normalize();

        yield return TurnPlayerInNewDirection(vecToEnemy, turnToEnemyTime * Time.timeScale);

        yield return new WaitForSecondsRealtime(timeToWaitTillBoost);
        
        Time.timeScale = 1;
        maxTurnSpeed /= 15;
        cinemachineTransposer.m_YawDamping = cameraYawDamping;
        lockDriftingSlowMoBoost = false;
        
        rb.velocity = playerVisuals.forward * currentMaxSpeed;
        Boost(boostForce);
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
        StartCoroutine(SlowDownCoroutine());
    }
    private IEnumerator SlowDownCoroutine()
    {
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
    #endregion
    #endregion

    #region Collider ------------------------------------------------------------------------------------------------------------------------------------
    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.layer != 9)
            return;
        
        if (isDrifting)
            StopDrifting();
        
        currentMaxSpeed = Mathf.Max(baseMaxSpeed,currentMaxSpeed / 1.5f);
        
        //get vector to other collider rotate it 90 degrees and turn player in that direction
        Vector3 colliderPos = other.collider.ClosestPoint(transform.position);
        Vector3 vecToCollider = colliderPos - transform.position;
        vecToCollider.Scale(new Vector3(1,0,1));
        float signedAngle = Vector3.SignedAngle(playerVisuals.forward, vecToCollider, Vector3.up);

        float rotation = signedAngle > 0 ? -90 : 90;
        Vector3 newForward = Quaternion.AngleAxis(rotation, playerVisuals.up) * vecToCollider.normalized;
        rb.velocity = newForward * rb.velocity.magnitude;
        
        StartCoroutine(TurnPlayerInNewDirection(newForward, .2f));
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
        Collider[] colliders = Physics.OverlapSphere(transform.position, sphereCollider.radius + .01f, ground);
        return colliders.Length > 0;
    }

    bool IsFalling()
    {
        if (currentState == PlayerState.Jumping || currentState == PlayerState.DriftJumping)
            return rb.velocity.y < -.5f;

        bool hitGround = Physics.Raycast(transform.position, -playerVisuals.up, sphereCollider.radius + 1f, ground);
        return rb.velocity.y < -.5f || !hitGround;

    }
    #endregion
    
    #region Various Methods ------------------------------------------------------------------------------------------------------------------------------------

    public void CollectFirefly(int amount)
    {
        fireflyCount += amount;
        material.SetFloat("_fireflyCount", fireflyCount);
    }
    
    public void Die()
    {
        Debug.Log("Player Died");
        rb.isKinematic = true;
        // rb.velocity = Vector3.zero;
        enabled = false;
        GameManager.instance.StopGame();
    }

    void PauseMe()
    {
        StartCoroutine(WhilePaused());
    }

    IEnumerator WhilePaused()
    {
        controls = null;
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
        if (context.started && isGrounded && !GameManager.instance.gameIsPaused)
        {
            currentState = currentState == PlayerState.Drifting ? PlayerState.DriftJumping : PlayerState.Jumping;
            rb.velocity += playerVisuals.up * jumpForce;
            justStartedJumping = true;
            waterVFX.SetActive(false);
            animator.CrossFade(jumpingUpClip.name, .2f);
        }
    }

    public void OnLeftDrift(InputAction.CallbackContext context)
    {
        if (context.canceled && isDrifting)
        {
            StopDrifting();
            return;
        }

        Transform tmpDriftPoint = null;
        bool driftingIsLocked = lockDriftingSlowMoBoost || lockDriftingCooldown;
        bool canStartDrift = context.started && !isDrifting && leftDriftPointContainer.HasDriftPoint(out tmpDriftPoint);
        
        if (!driftingIsLocked && canStartDrift)
        {
            isDriftingRight = false;
            currentDriftPoint = tmpDriftPoint;

            StartDrifting();
        }
    }

    public void OnRightDrift(InputAction.CallbackContext context)
    {
        if (context.canceled && isDrifting)
        {
            StopDrifting();
            return;
        }

        Transform tmpDriftPoint = null;
        bool driftingIsLocked = lockDriftingSlowMoBoost || lockDriftingCooldown;
        bool canStartDrift = context.started && !isDrifting && rightDriftPointContainer.HasDriftPoint(out tmpDriftPoint);

        if (!driftingIsLocked && canStartDrift)
        {
            isDriftingRight = true;
            currentDriftPoint = tmpDriftPoint;

            StartDrifting();
        }
    }
    #endregion
}