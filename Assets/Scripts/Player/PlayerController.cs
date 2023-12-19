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
        Breaking,
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
    [SerializeField] private float slowFieldSlow;
    [SerializeField] private float adjustToGroundSlopeSpeed = 3;
    [SerializeField] private LayerMask ground;
    [SerializeField] private LayerMask obstacle;

    private float currentMaxSpeed;

    [Header("Jumping/ In Air")]
    [SerializeField] protected float jumpForce;
    [SerializeField] protected float breakForce;
    [SerializeField] protected float gravitationMultiplier;

    [Header("Drifting")] 
    [SerializeField] private float driftTurnSpeed;
    [SerializeField] private float driftMaxTurnAbility = 30;
    [SerializeField] private float tractionWhileDrifting;
    [SerializeField] private float overSteer;
    [SerializeField] private Gradient defaultParticleColor;
    [SerializeField] private Gradient boostParticleColor;
    private float outerDriftRadius;
    private float driftTimeCounter;
    private float currentDriftRotation;
    
    [Header("Boost")]
    [SerializeField] private float boostForce;
    [SerializeField] private float boostSubtractPerSecond;
    [SerializeField] private float timeToGetBoost;

    [Header("Camera")] 
    [SerializeField] private float lookAtMoveAmount;
    [SerializeField] private float horizontalLerpSpeed;
    [SerializeField] private float cameraYawDamping;
    [SerializeField] private float cameraDriftYawDamping;
    [SerializeField] private float maxDutchTilt;

    [Header("EnemyAttack / Slo Mo Boost")] 
    [SerializeField] private float turnToEnemySpeed = 10;
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
    private bool startedDriftBoost;
    private bool lockSteering;

    [Header("References")]
    [SerializeField] protected Transform playerVisuals;
    [SerializeField] private Transform tonguePoint;
    [SerializeField] private Transform lookAt;
    [FormerlySerializedAs("groundSlopeRef")] [SerializeField] private Transform rotationReference;
    [SerializeField] private ParticleSystem groundParticles;
    [SerializeField] private Material material;
    [SerializeField] protected DriftPointContainer rightDriftPointContainer;
    [SerializeField] protected DriftPointContainer leftDriftPointContainer;
    [SerializeField] private AudioSource walkAudioSource;
    [SerializeField] private GameObject refCylinder;
    
    public Rigidbody rb { get; private set; }
    private GameObject groundParticlesObject;
    private Transform currentDriftPoint;
    protected PlayerState currentState = PlayerState.Running;
    
    private PlayerInput controls;
    private CinemachineVirtualCamera virtualCamera;
    private CinemachineTransposer cinemachineTransposer;
    private SphereCollider sphereCollider;
    private Quaternion rotationAtDriftStart;
    
    private LineRenderer lineRenderer;
    private GameVariables gameVariables => GameVariables.instance;

    #region Instantiation and Destroying ------------------------------------------------------------------------------------------------------------------------------------
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        sphereCollider = GetComponent<SphereCollider>();
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0;
        virtualCamera = gameVariables.virtualCamera;
        cinemachineTransposer = virtualCamera.GetCinemachineComponent<CinemachineTransposer>();
        groundParticlesObject = groundParticles.gameObject;
        currentMaxSpeed = baseMaxSpeed;
        
        if (controls == null)
        {
            controls = new PlayerInput();
            controls.Enable();
            controls.P_Controls.SetCallbacks(this);
        }

        cinemachineTransposer.m_YawDamping = 5;

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
    }
    
    private void LateUpdate()
    {
        if (isDrifting)
        {
            lineRenderer.SetPosition(0, tonguePoint.position);
            lineRenderer.SetPosition(1, currentDriftPoint.position);
        }
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
        isGrounded = IsGrounded();
        isFalling = IsFalling();

        bool stateEqualsFalling = currentState == PlayerState.Falling || (currentState == PlayerState.DriftFalling && !justStartedJumping);

        if (!isGrounded && isFalling )
            currentState = isDrifting? PlayerState.DriftFalling : PlayerState.Falling;
        else if (stateEqualsFalling && isGrounded)
        {
            currentState = currentState == PlayerState.DriftFalling ? PlayerState.Drifting : PlayerState.Running;
            groundParticlesObject.SetActive(true);
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
            leftDriftPointContainer.GetDriftPoints();
            rightDriftPointContainer.GetDriftPoints();
        }

        //move LookAt Object
        horizontalLerpTo = Mathf.Lerp(horizontalLerpTo, horizontal + driftHorizontal, Time.deltaTime * horizontalLerpSpeed);
        lookAt.position = playerVisuals.position + playerVisuals.right * (horizontalLerpTo * lookAtMoveAmount);

        virtualCamera.m_Lens.Dutch = horizontalLerpTo * maxDutchTilt;
        
        //traction
        float gravity = rb.velocity.y;
        Vector3 velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        rb.velocity = Vector3.Lerp(velocity.normalized, playerVisuals.forward, currentTraction * Time.deltaTime) * velocity.magnitude;
        rb.velocity += new Vector3(0, gravity, 0);
    }
    
    private void StartDrifting()
    {
        isDrifting = true;
        currentState = isGrounded ? PlayerState.Drifting : PlayerState.DriftJumping;
        lineRenderer.positionCount = 2; //start renderering the tongue line

        outerDriftRadius = Vector3.Distance(currentDriftPoint.position, transform.position);

        rotationAtDriftStart = playerVisuals.rotation;
        currentTraction = tractionWhileDrifting;
        driftTimeCounter = 0;
        currentDriftRotation = 0;
        driftHorizontal = isDriftingRight ? .5f : -.5f;
        
        //change how fast the camera copies the rotation of the player
        cinemachineTransposer.m_YawDamping = cameraDriftYawDamping;

        StartCoroutine(Drift());
        // refCylinder.transform.position = currentDriftPoint.position;
        // refCylinder.transform.localScale = new Vector3(outerDriftRadius * 2, 2, outerDriftRadius * 2);
    }

    private void StopDrifting()
    {
        isDrifting = false;
        arrivedAtDriftPeak = false;
        lineRenderer.positionCount = 0; //stop rendering the tongue line
        driftHorizontal = 0;
        currentState = currentState == PlayerState.DriftJumping ? PlayerState.Jumping : PlayerState.Running;
        currentTraction = traction;

        if (startedDriftBoost)
        {
            Boost();
            startedDriftBoost = false;
            ChangeParticleColor(defaultParticleColor);
        }
        
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

            AdjustToGroundSlope();
            TurnVelocityToPlayerForward();
            ChargeDriftBoost(tongueStretchFactor);

            yield return null;
        }
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
        if (tongueStretchFactor > .5f && !startedDriftBoost)
        {
            driftTimeCounter += Time.deltaTime;
            if (driftTimeCounter > timeToGetBoost)
            {
                ChangeParticleColor(boostParticleColor);
                startedDriftBoost = true;
            }
                
        }
    }
    
    void AdjustToGroundSlope()
    {
        Quaternion wantedRotation;
        // groundSlopeRef.rotation = playerVisuals.rotation;
        
        if (isGrounded)
        {
            Physics.Raycast(transform.position, -playerVisuals.up, out RaycastHit groundHit, sphereCollider.radius + .5f, ground);

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
    void Boost()
    {
        currentMaxSpeed += boostForce;
        rb.velocity = playerVisuals.forward * currentMaxSpeed;
    }
    
    public void StartSlowMoBoost()
    {
        StartCoroutine(SlowMoBoost());
    }
    IEnumerator SlowMoBoost()
    {
        Time.timeScale = .1f;
        maxTurnSpeed *= 10;
        rb.velocity = Vector3.zero;
        cinemachineTransposer.m_YawDamping = 0;

        yield return TurnToEnemy();

        yield return new WaitForSecondsRealtime(timeToWaitTillBoost);
        
        Time.timeScale = 1;
        maxTurnSpeed /= 10;
        cinemachineTransposer.m_YawDamping = cameraYawDamping;
        
        rb.velocity = playerVisuals.forward * currentMaxSpeed;
        Boost();
    }
    #endregion
    
    #region Decceleration ------------------------------------------------------------------------------------------------------------------------------------
    private void ReduceMaxSpeed()
    {
        currentMaxSpeed -= Time.deltaTime * boostSubtractPerSecond;
        currentMaxSpeed = Mathf.Max(baseMaxSpeed, currentMaxSpeed);
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
        if (other.gameObject.layer == 9)
        {
            rb.velocity = -playerVisuals.forward * 50;
            currentMaxSpeed = baseMaxSpeed;

            if (isDrifting)
            {
                StopDrifting();
            }
        }
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
        return rb.velocity.y < -.5f;
    }
    #endregion
    
    #region Various Methods ------------------------------------------------------------------------------------------------------------------------------------

    public void CollectFirefly(int amount)
    {
        fireflyCount += amount;
        material.SetFloat("_fireflyCount", fireflyCount);
    }
    
    // void PlayWalkSound()
    // {
    //     timeUntilNextWalkSound -= Time.fixedDeltaTime;
    //     
    //     if (isGrounded && timeUntilNextWalkSound <= 0)
    //     {
    //         walkAudioSource.pitch = Random.Range(.7f, 1.3f);
    //         walkAudioSource.volume = Random.Range(.8f, 1.2f);
    //         walkAudioSource.Play();
    //         timeUntilNextWalkSound = .25f;
    //     }
    // }
    public void Die()
    {
        Debug.Log("Player Died");
        rb.isKinematic = true;
        // rb.velocity = Vector3.zero;
        enabled = false;
        GameManager.instance.StopGame();
    }

    IEnumerator TurnToEnemy()
    {
        lockSteering = true;
        Vector3 vecToEnemy = gameVariables.enemy.transform.position - transform.position;
        Quaternion fromRotation = playerVisuals.rotation;
        Quaternion toRotation = Quaternion.LookRotation(vecToEnemy, playerVisuals.up);
        float t = 0;

        while (t < 1)
        {
            playerVisuals.rotation = Quaternion.Lerp(fromRotation, toRotation, Mathf.SmoothStep(0, 1, t));
            t += Time.deltaTime * turnToEnemySpeed;
            yield return null;
        }

        playerVisuals.rotation = toRotation;
        lockSteering = false;
    }
    
    void PauseMe()
    {
        StartCoroutine(WhilePaused());
    }

    IEnumerator WhilePaused()
    {
        Vector3 velocity = rb.velocity;
        rb.velocity = Vector3.zero;
        rb.isKinematic = true;

        yield return new WaitWhile(() => gameVariables.isPaused);

        rb.isKinematic = false;
        rb.velocity = velocity;
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
        if (context.started && isGrounded)
        {
            currentState = currentState == PlayerState.Drifting ? PlayerState.DriftJumping : PlayerState.Jumping;
            rb.velocity += playerVisuals.up * jumpForce;
            justStartedJumping = true;
            groundParticlesObject.SetActive(false);
        }
    }

    public void OnBreak(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            currentState = PlayerState.Breaking;
            StartCoroutine(Break());
        }
        else if (context.canceled)
        {
            currentState = PlayerState.Running;
            isBreaking = false;
        }
    }

    IEnumerator Break()
    {
        isBreaking = true;
        while (currentState == PlayerState.Breaking)
        {
            if (isGrounded)
                rb.velocity *= 1 - breakForce;
            yield return 0;
        }
    }

    public void OnSlowMoBoost(InputAction.CallbackContext context)
    {
        // if(context.started)
        //     StartSlowMoBoost();
    }

    public void OnLeftDrift(InputAction.CallbackContext context)
    {
        if (context.started && leftDriftPointContainer.driftPoints.Count > 0)
        {
            isDriftingRight = false;
            currentDriftPoint = leftDriftPointContainer.driftPoints.First().transform;

            StartDrifting();
        }
        else if (context.canceled && isDrifting)
        {
            StopDrifting();
        }
    }

    public void OnRightDrift(InputAction.CallbackContext context)
    {
        if (context.started && rightDriftPointContainer.driftPoints.Count > 0)
        {
            isDriftingRight = true;
            currentDriftPoint = rightDriftPointContainer.driftPoints.First().transform;

            StartDrifting();
        }
        else if (context.canceled && isDrifting)
        {
            StopDrifting();
        }
    }

    #endregion
}