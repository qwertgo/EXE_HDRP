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
    [SerializeField] private float adjustToGroundSlopeSpeed = 3;
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

    [Header("Camera")] 
    [SerializeField] private float lookAtMoveAmount;
    [SerializeField] private float horizontalLerpSpeed;
    [SerializeField] private float cameraYawDamping;
    [SerializeField] private float cameraDriftYawDamping;
    [SerializeField] private float maxDutchTilt;

    private Transform currentDriftPoint;
    private PlayerState currentState = PlayerState.Running;
    private Quaternion rotationAtDriftStart;

    private float horizontal;
    private float horizontalLerpTo;
    private float currentTraction;
    private float maxSpeedOriginal;

    public int fireflyCount { get; private set; }
    
    private bool isGrounded;
    private bool isFalling;
    private bool isDrifting;
    private bool justStartedJumping;
    private bool isDriftingRight;
    private bool arrivedAtDriftPeak;
    private bool startedDriftBoost;
    

    [Header("References")]
    [SerializeField] private Transform playerVisuals;
    [SerializeField] private Transform tonguePoint;
    [SerializeField] private Transform lookAt;
    [SerializeField] private Transform groundSlopeRef;
    [SerializeField] private ParticleSystem groundParticles;
    [SerializeField] private Material material;
    [SerializeField] private DriftPointContainer rightDriftPointContainer;
    [SerializeField] private DriftPointContainer leftDriftPointContainer;
    
    private GameObject groundParticlesObject;
    private CinemachineVirtualCamera virtualCamera;
    private CinemachineTransposer cinemachineTransposer;
    private Rigidbody rb;
    private SphereCollider sphereCollider;
    private PlayerInput controls;
    private LineRenderer lineRenderer;

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

    #region Physics
    void FixedUpdate()
    {
        UpdateState();
        Accelerate();
    
        //multiply Gravity after jump peak
        rb.velocity += (isFalling ? Physics.gravity * gravitationMultiplier : Physics.gravity) * Time.fixedDeltaTime;
        // Debug.Log(rb.velocity.magnitude);
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
        Steer();
    }

    #endregion

    #region States
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

    void Steer()
    {
        //steer or drift
        if (isDrifting)
            Drift();
        else
        {
            //steer
            playerVisuals.Rotate(playerVisuals.up, horizontal * Time.deltaTime * steerAmount);
            
            groundSlopeRef.rotation = playerVisuals.rotation;
            playerVisuals.rotation = AdjustToGroundSlope();

            //update container to get closest drift point
            leftDriftPointContainer.GetDriftPoints();
            rightDriftPointContainer.GetDriftPoints();
        }

        //move LookAt Object
        horizontalLerpTo = Mathf.Lerp(horizontalLerpTo, horizontal, Time.deltaTime * horizontalLerpSpeed);
        lookAt.position = playerVisuals.position + playerVisuals.right * (horizontalLerpTo * lookAtMoveAmount);

        virtualCamera.m_Lens.Dutch = horizontalLerpTo * maxDutchTilt;
        
        //traction
        float gravity = rb.velocity.y;
        Vector3 velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        rb.velocity = Vector3.Lerp(velocity.normalized, playerVisuals.forward, currentTraction * Time.deltaTime) * velocity.magnitude;
        rb.velocity += new Vector3(0, gravity, 0);
    }
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
        
        groundSlopeRef.forward = isDriftingRight ?  dirToDriftPoint.RotateLeft90Deg() : dirToDriftPoint.RotateRight90Deg();
        Quaternion wantedRotation = AdjustToGroundSlope();

        playerVisuals.rotation = Quaternion.Lerp(rotationAtDriftStart, wantedRotation, tongueStretchFactor);

        // //make the velocity face the same direction as the player
        if (isGrounded)
        {
            rb.velocity = playerVisuals.forward * rb.velocity.magnitude;
        }
        else
        {
            float yVelocity = rb.velocity.y;
            Vector3 velocity2D = new Vector2(rb.velocity.x, rb.velocity.z);
            rb.velocity = playerVisuals.forward * velocity2D.magnitude + new Vector3(0, yVelocity,0);
        }

        
        //drift visuals
        if(tongueStretchFactor < .5f)
            return;
        
        driftTimeCounter += Time.deltaTime;
        if (driftTimeCounter > timeToGetBoost && !startedDriftBoost)
        {
            ChangeParticleColor(boostParticleColor);
            startedDriftBoost = true;
        }
    }
    
    Quaternion AdjustToGroundSlope()
    {
        if (isGrounded)
        {
            Physics.Raycast(transform.position, -playerVisuals.up, out RaycastHit groundHit, sphereCollider.radius + .5f, ground);
            groundSlopeRef.forward = Vector3.ProjectOnPlane(groundSlopeRef.forward, groundHit.normal);
            
            float rotation = Vector3.SignedAngle(groundSlopeRef.up, groundHit.normal, groundSlopeRef.forward);
            groundSlopeRef.Rotate(groundSlopeRef.forward, rotation, Space.World);
        }
        else
        {
            groundSlopeRef.forward = Vector3.ProjectOnPlane(groundSlopeRef.forward, Vector3.up);
        }
        
        
        return Quaternion.Lerp(playerVisuals.rotation, groundSlopeRef.rotation, Time.deltaTime * adjustToGroundSlopeSpeed);
    }

    void Accelerate()
    {
        //acceleration
        if (currentState != PlayerState.Breaking)
            rb.velocity += acceleration * Time.fixedDeltaTime * playerVisuals.forward;

        //limit max speed
        if (rb.velocity.magnitude > maxSpeed)
        {
            rb.velocity = rb.velocity.normalized * maxSpeed;
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
    public void Die()
    {
        Debug.Log("Player Died");
        rb.isKinematic = true;
        // rb.velocity = Vector3.zero;
        enabled = false;
        GameManager.instance.TooglePause();
    }
    
    IEnumerator Boost()
    {
        ReturnToDefaultSpeed();
        maxSpeed += boostForce;
        rb.velocity += playerVisuals.forward * boostForce;
        yield return new WaitForSeconds(boostTime);

        maxSpeed = maxSpeedOriginal;
    }
    
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

    public void StartSlowMoBoost()
    {
        StartCoroutine(SlowMoBoost());
    }
    IEnumerator SlowMoBoost()
    {
        Time.timeScale = .1f;
        steerAmount *= 10;
        cinemachineTransposer.m_YawDamping = 0;
        rb.velocity = Vector3.zero;
        
        yield return new WaitForSecondsRealtime(2);
        
        Time.timeScale = 1;
        steerAmount /= 10;
        cinemachineTransposer.m_YawDamping = cameraYawDamping;
        
        rb.velocity = playerVisuals.forward * maxSpeed;
        StartCoroutine(Boost());
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

    public void OnSlowMoBoost(InputAction.CallbackContext context)
    {
        if(context.started)
            StartSlowMoBoost();
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

        rotationAtDriftStart = playerVisuals.rotation;

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
}