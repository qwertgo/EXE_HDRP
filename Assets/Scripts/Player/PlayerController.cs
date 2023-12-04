using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class PlayerController : MonoBehaviour, PlayerInput.IP_ControlsActions
{
    private enum PlayerState
    {
        Running,
        Breaking,
        Drifting,
        Jumping,
        Falling,
        JumpDrifting,
    }

    [Header("Base Movement Values")]
    [SerializeField] private float acceleration;
    [SerializeField] private float steerAmount;
    [SerializeField] private float maxSpeed;
    [SerializeField] private float traction;
    [SerializeField] private float slowFieldSlow;
    [SerializeField] private LayerMask ground;
    [SerializeField] private LayerMask obstacle;

    [Header("Jumping/ In Air")]
    [SerializeField] private float jumpForce;
    [SerializeField] private float breakForce;
    [SerializeField] private float gravitationMultiplier;

    [Header("Drifting")]
    [SerializeField] private float tractionWhileDrifting;
    [SerializeField] private float timeToGetBoost;
    [SerializeField] private float boostForce;
    [SerializeField] private float boostTime;
    [SerializeField] private Gradient defaultParticleColor;
    [SerializeField] private Gradient boostParticleColor;
    private float outerDriftRadius;
    private float driftTimeCounter;
    // [SerializeField] private float driftOversteer;
    
    [Header("Camera")] 
    [SerializeField] private float lookAtMoveAmount;
    [SerializeField] private float horizontalLerpSpeed;
    [SerializeField] private float cameraYawDamping;
    [SerializeField] private float cameraDriftYawDamping;
    [SerializeField] private float maxDutchTilt;

    private float horizontalLerpTo;

    private Transform currentDriftPoint;
    private PlayerState currentState = PlayerState.Running;
    private Vector3 forwardVecAtDriftStart;

    private float horizontal;
    
    private float currentTraction;
    private float maxSpeedOriginal;

    public int fireflyCount { get; private set; }
    
    private bool isGrounded;
    private bool isFalling;
    private bool isDrifting;
    private bool justStartedJumping;
    private bool isDriftingRight;
    private bool arrivedAtDriftPeak;
    

    [Header("References")]
    [SerializeField] private Transform playerVisuals;
    [SerializeField] private Transform tonguePoint;
    [SerializeField] private Transform lookAt;
    [SerializeField] private Transform groundSlopeRef;
    [SerializeField] private ParticleSystem groundParticles;
    [SerializeField] private Material material;
    private GameObject groundParticlesObject;
    [SerializeField] private DriftPointContainer rightDriftPointContainer;
    [SerializeField] private DriftPointContainer leftDriftPointContainer;
    // [SerializeField] private GameObject distanceIndicator;
    private CinemachineVirtualCamera virtualCamera;
    private CinemachineTransposer cinemachineTransposer;
    private Rigidbody rb;
    private SphereCollider sphereCollider;
    private PlayerInput controls;
    private LineRenderer lineRenderer;

    //Coroutine slowDown;
    //Coroutine speedUp;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        sphereCollider = GetComponent<SphereCollider>();
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0;
        virtualCamera = Camera.main.GetComponentInChildren<CinemachineVirtualCamera>();
        cinemachineTransposer = virtualCamera.GetCinemachineComponent<CinemachineTransposer>();
        groundParticlesObject = groundParticles.gameObject;

        cinemachineTransposer.m_YawDamping = 5;
        maxSpeedOriginal = maxSpeed;

        currentTraction = traction;

        if (controls == null)
        {
            controls = new PlayerInput();
            controls.Enable();
            controls.P_Controls.SetCallbacks(this);
        }

        rightDriftPointContainer.SetRightContainer();

        GameVariables.instance.player = this;
    }

    private void OnDestroy()
    {
        material.SetFloat("_fireflyCount", 0);
    }

    #region physics
    void FixedUpdate()
    {
        //update current State 
        UpdateState();
        Steer();
        AdjustToGroundSlope();
        Accelerate();

        //multiply Gravity after jump peak
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

    void UpdateState()
    {
        isGrounded = IsGrounded();
        isFalling = IsFalling();

        bool stateEqualsFalling = currentState == PlayerState.Falling || (currentState == PlayerState.JumpDrifting && !justStartedJumping);

        if (!isGrounded && isFalling && !isDrifting)
            currentState = PlayerState.Falling;
        else if (stateEqualsFalling && isGrounded)
        {
            currentState = currentState == PlayerState.JumpDrifting ? PlayerState.Drifting : PlayerState.Running;
            groundParticlesObject.SetActive(true);
        }
        else if (currentState == PlayerState.JumpDrifting && isGrounded && !justStartedJumping)
            currentState = PlayerState.Drifting;

        justStartedJumping = false;
    }

    void AdjustToGroundSlope()
    {
        groundSlopeRef.rotation = playerVisuals.rotation;
        float rotation;
        
        if (isGrounded)
        {
            Physics.Raycast(transform.position, -playerVisuals.up,out RaycastHit groundHit, sphereCollider.radius + 2, ground);
            
            groundSlopeRef.forward = Vector3.ProjectOnPlane(groundSlopeRef.forward, groundHit.normal);
            rotation = Vector3.SignedAngle(groundSlopeRef.up, groundHit.normal, groundSlopeRef.forward);
        }
        else
        {
            groundSlopeRef.forward = Vector3.ProjectOnPlane(groundSlopeRef.forward, Vector3.up);
            rotation = 0;
        }
        
        groundSlopeRef.Rotate(groundSlopeRef.forward, rotation, Space.World);
        playerVisuals.rotation = Quaternion.Lerp(playerVisuals.rotation, groundSlopeRef.rotation, Time.fixedDeltaTime * 8f);
    }

    void Steer()
    {
        //steer or drift
        if (isDrifting)
            Drift();
        else
        {
            //steer
            // playerVisuals.eulerAngles += new Vector3(0, horizontal * Time.deltaTime * steerAmount, 0);
            playerVisuals.Rotate(playerVisuals.up, horizontal * Time.fixedDeltaTime * steerAmount);

            //update container to get closest drift point
            leftDriftPointContainer.GetDriftPoints();
            rightDriftPointContainer.GetDriftPoints();
        }

        //move LookAt Object
        horizontalLerpTo = Mathf.Lerp(horizontalLerpTo, horizontal, Time.deltaTime * horizontalLerpSpeed);
        lookAt.position = playerVisuals.position + playerVisuals.right * (horizontalLerpTo * lookAtMoveAmount);

        virtualCamera.m_Lens.Dutch = horizontalLerpTo * maxDutchTilt;
        
        //traction
        // Debug.DrawRay(transform.position, playerVisuals.forward * 4);
        // Debug.DrawRay(transform.position, rb.velocity.normalized * 4, Color.cyan);
        float gravity = rb.velocity.y;
        Vector3 velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        rb.velocity = Vector3.Lerp(velocity.normalized, playerVisuals.forward, currentTraction * Time.fixedDeltaTime) * velocity.magnitude;
        rb.velocity += new Vector3(0, gravity, 0);
    }

    void Accelerate()
    {
        //acceleration
        if(currentState != PlayerState.Breaking)
            rb.AddForce(playerVisuals.forward * acceleration, ForceMode.Acceleration);

        //limit max speed
        Vector3 xzVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        if (xzVelocity.magnitude > maxSpeed)
        {
            xzVelocity = xzVelocity.normalized * maxSpeed;
            rb.velocity = xzVelocity + new Vector3(0, rb.velocity.y, 0);
        }
    }

    private bool startedDriftBoost;

    void Drift()
    {

        //variables needed
        Vector3 vecToDriftPoint = currentDriftPoint.position - transform.position;
        Vector2 vecToDriftPoint2D = new Vector2(vecToDriftPoint.x, vecToDriftPoint.z);
        
        //tonguestretchfactor indicates how much the player should go into the drift
        float tongueStretchFactor = arrivedAtDriftPeak ? 1 : GetTongeStretchFactor(vecToDriftPoint2D) ;
        
        //get the direction the player should face at the max tonguestretchfactor
        Vector3 dirToDriftPoint = new Vector3(vecToDriftPoint.x, 0, vecToDriftPoint.z);
        dirToDriftPoint.Normalize();
        Vector3 wantedFroward = isDriftingRight ?  dirToDriftPoint.RotateLeft90Deg() : dirToDriftPoint.RotateRight90Deg();

        playerVisuals.forward = Vector3.Lerp(forwardVecAtDriftStart, wantedFroward, tongueStretchFactor);

        //make the velocity face the same direction as the player
        float yVelocity = rb.velocity.y;
        Vector3 velocity2D = new Vector2(rb.velocity.x, rb.velocity.z);

        rb.velocity = playerVisuals.forward * velocity2D.magnitude + new Vector3(0, yVelocity,0);
        
        //drift visuals
        if(tongueStretchFactor < .5f)
            return;
        
        driftTimeCounter += Time.fixedDeltaTime;
        if (driftTimeCounter > timeToGetBoost && !startedDriftBoost)
        {
            ChangeParticleColor(boostParticleColor);
            startedDriftBoost = true;
        }
    }

    float GetTongeStretchFactor( Vector2 vecToDriftPoint2D)
    {
        //get distance to drift point
        float distanceToDriftPoint = Vector3.Distance(currentDriftPoint.position, transform.position);

        //get rotation between playerForward and vector that goes to drift point (clamped a 90 degrees)
        Vector2 forward2D = new Vector2(playerVisuals.forward.x, playerVisuals.forward.z);

        float angleToDriftPoint = Vector2.SignedAngle(forward2D, vecToDriftPoint2D);
        angleToDriftPoint = Mathf.Min(Mathf.Abs(angleToDriftPoint), 80);
        // Debug.Log("angle: " + angleToDriftPoint);

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

    void ChangeParticleColor(Gradient color)
    {
        ParticleSystem.ColorOverLifetimeModule colOverLifeTime = groundParticles.colorOverLifetime;
        colOverLifeTime.color = color;
    }


    IEnumerator Boost()
    {
        ReturnToDefaultSpeed();
        maxSpeed += boostForce;
        rb.velocity += playerVisuals.forward * boostForce;
        yield return new WaitForSeconds(boostTime);

        maxSpeed = maxSpeedOriginal;
    }
    #endregion

    #region Collider

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.layer == 9)
        {
            rb.velocity = Vector3.zero;
            
            rb.AddForce(-playerVisuals.forward * 50, ForceMode.Impulse);

            if (isDrifting)
            {
                StopDrifting();
            }
        }
    }

    #endregion

    #region Bools
    bool IsGrounded()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, sphereCollider.radius + .01f, ground);
        return colliders.Length > 0;
    }

    bool IsFalling()
    {
        return rb.velocity.y < -.5f;
    }

    bool IsDrifting()
    {
        return currentState == PlayerState.Drifting || currentState == PlayerState.JumpDrifting;
    }
    #endregion
    
    #region various Methods

    public void CollectFirefly()
    {
        fireflyCount++;
        material.SetFloat("_fireflyCount", fireflyCount);
        //later there will be UI and Player Changes
    }
    #endregion

    #region handle Input
    public void OnSteer(InputAction.CallbackContext context)
    {
        if (isDrifting)
           return;
        
        horizontal = context.ReadValue<Vector2>().x;
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started && isGrounded)
        {
            currentState = currentState == PlayerState.Drifting ? PlayerState.JumpDrifting : PlayerState.Jumping;
            rb.AddForce(playerVisuals.up * jumpForce, ForceMode.Impulse);
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
        }
    }

    IEnumerator Break()
    {
        while (currentState == PlayerState.Breaking)
        {
            if (isGrounded)
                rb.velocity *= 1 - breakForce;
            yield return 0;
        }
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

    void StartDrifting()
    {
        isDrifting = true;
        currentState = isGrounded ? PlayerState.Drifting : PlayerState.JumpDrifting;
        lineRenderer.positionCount = 2; //start renderering the tongue line

        forwardVecAtDriftStart = playerVisuals.transform.forward;

        outerDriftRadius = Vector3.Distance(currentDriftPoint.position, transform.position);

        currentTraction = tractionWhileDrifting;

        horizontal = isDriftingRight ? 1f : -1f;

        driftTimeCounter = 0;
        
        //change how fast the camera copies the rotation of the player
        cinemachineTransposer.m_YawDamping = cameraDriftYawDamping;
    }

    void StopDrifting()
    {
        isDrifting = false;
        arrivedAtDriftPeak = false;
        lineRenderer.positionCount = 0; //stop rendering the tongue line
        currentState = currentState == PlayerState.JumpDrifting ? PlayerState.Jumping : PlayerState.Running;
        currentTraction = traction;

        horizontal = 0;

        if (driftTimeCounter > timeToGetBoost)
        {
            StartCoroutine(Boost());
            startedDriftBoost = false;
            ChangeParticleColor(defaultParticleColor);
        }
        
        //change how fast the camera copies the rotation of the player
        cinemachineTransposer.m_YawDamping = cameraYawDamping;
    }
    #endregion
    

    #region slowBox
    public void SlowDown()
    {
        StartCoroutine(SlowDownCoroutine());
    }
    IEnumerator SlowDownCoroutine()
    {
        float t = 0;
        float from = maxSpeedOriginal;
        float To = maxSpeedOriginal - slowFieldSlow;

        while (t < 1)
        {
            maxSpeed = Mathf.Lerp(from, To, t);
            t += Time.deltaTime * 2;
            // Debug.Log(t);
            yield return null;
        }

        maxSpeed = To;
    }

    public void ReturnToDefaultSpeed()
    {
        if(maxSpeed >= maxSpeedOriginal)
            return;
        
        StopCoroutine(nameof(SlowDownCoroutine));
        maxSpeed = maxSpeedOriginal;
    }

    #endregion
}