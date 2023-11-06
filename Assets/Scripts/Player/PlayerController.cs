using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        Falling
    }
    
    [Header("Base Movement Values")] 
    [SerializeField] private float acceleration;
    [SerializeField] private float steerAmount;
    [SerializeField] private float maxSpeed;
    [SerializeField] private float traction;
    [SerializeField] private LayerMask ground;
    
    [Header("Jumping/ In Air")]
    [SerializeField] private float jumpForce;
    [SerializeField] private float breakForce;
    [SerializeField] private float gravitationMultiplier;
    
    [Header("Drifting")]
    [SerializeField] private float driftTurnSpeed;
    [SerializeField] private float driftRadiusMultiplier;
    [SerializeField] private float tractionWhileDrifting;
    [SerializeField] private float driftOversteer;

    [Header("Camera")] 
    [SerializeField] private float lookAtDistance;
    [SerializeField] private float lookAtSpeed;

    private float lookAtLerpTo;

    private PlayerState currentState = PlayerState.Running;
    private Transform currentDriftPoint;

    private float horizontal;
    private float innerDriftRadius;
    private float outerDriftRadius;
    private float currentTraction;
    
    private int fireflyCount;
    private bool isGrounded;
    private bool isDriftingRight;
    

    [Header("References")]
    [SerializeField] private Transform playerVisuals;
    [SerializeField] private Transform tonguePoint;
    [SerializeField] private Transform lookAt;
    [SerializeField] private DriftPointContainer rightDriftPointContainer;
    [SerializeField] private DriftPointContainer leftDriftPointContainer;
    private Rigidbody rb;
    private SphereCollider _collider;
    private PlayerInput _controls;
    private LineRenderer lineRenderer;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        _collider = GetComponent<SphereCollider>();
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0;

        currentTraction = traction;

        if (_controls == null)
        {
            _controls = new PlayerInput();
            _controls.Enable();
            _controls.P_Controls.SetCallbacks(this);
        }
        
        rightDriftPointContainer.SetRightContainer();
    }

    #region physics
    void FixedUpdate()
    {
        //update current State 
        isGrounded = IsGrounded();
        CheckState();
        
        if (currentState == PlayerState.Drifting)
        {
            Drift();
        }
        else
        {
            //steer
            playerVisuals.eulerAngles += new Vector3(0, horizontal * Time.deltaTime * steerAmount, 0);
            
            //update container to get closest drift point
            leftDriftPointContainer.GetDriftPoints();
            rightDriftPointContainer.GetDriftPoints();
        }
        
        //move LookAt Object
        lookAtLerpTo = Mathf.Lerp(lookAtLerpTo, horizontal * lookAtDistance, Time.deltaTime * lookAtSpeed);
        lookAt.position = playerVisuals.position + playerVisuals.right * lookAtLerpTo;

        //acceleration
        if(rb.velocity.magnitude < maxSpeed && currentState != PlayerState.Breaking)
            rb.AddForce(playerVisuals.forward * acceleration, ForceMode.Acceleration);

        //traction
        Debug.DrawRay(transform.position, playerVisuals.forward * 4);
        Debug.DrawRay(transform.position, rb.velocity.normalized * 4, Color.cyan);
        
        float gravity = rb.velocity.y;
        Vector3 velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        rb.velocity = Vector3.Lerp(velocity.normalized, playerVisuals.forward, currentTraction * Time.fixedDeltaTime) * velocity.magnitude;
        rb.velocity += new Vector3(0, gravity, 0);
        
        //multiply Gravity after jump peak
        bool isFalling = currentState == PlayerState.Falling;
        rb.velocity += (isFalling ? Physics.gravity * gravitationMultiplier : Physics.gravity) * Time.fixedDeltaTime;
    }

    private void LateUpdate()
    {
        if (currentState == PlayerState.Drifting)
        {
            lineRenderer.SetPosition(0, tonguePoint.position);
            lineRenderer.SetPosition(1, currentDriftPoint.position);
        }
    }

    void CheckState()
    {
        if (!isGrounded && rb.velocity.y < 0 && currentState != PlayerState.Drifting)
            currentState = PlayerState.Falling;
        else if (currentState == PlayerState.Falling && isGrounded)
        {
            currentState = PlayerState.Running;
            // rb.constraints = RigidbodyConstraints.FreezePositionY;
        }

    }

    void Drift()
    {
        float distanceToDriftPoint = Vector3.Distance(currentDriftPoint.position, transform.position);
        float t = Mathf.Max(0,  distanceToDriftPoint - innerDriftRadius);
        t /= outerDriftRadius - innerDriftRadius;
        // Debug.Log($"inner radius: {innerDriftRadius}, outer Radius: {innerDriftRadius + driftRadiusAddition}, distance: {distanceToDriftPoint}, t: {t}");

        Vector3 vecToPoint = currentDriftPoint.position - transform.position;
        
        float wantedAngle = Vector2.SignedAngle(Vector2.up, new Vector2(vecToPoint.x, vecToPoint.z));
        wantedAngle = -wantedAngle;
        wantedAngle += isDriftingRight ? -90 + driftOversteer : 90 - driftOversteer;

        float currentAngle = playerVisuals.transform.eulerAngles.y;
        wantedAngle = Mathf.Lerp(currentAngle, wantedAngle, t);

        Vector3 wantedEulerAngles = new Vector3(playerVisuals.eulerAngles.x, wantedAngle, playerVisuals.eulerAngles.z);
        playerVisuals.rotation = Quaternion.RotateTowards(playerVisuals.rotation, Quaternion.Euler(wantedEulerAngles), driftTurnSpeed * Time.fixedDeltaTime);
    }
    #endregion

    #region Bools
    // float GetUpwardsVelocity()
    // {
    //     Vector3 v = Vector3.down * Vector3.Dot(rb.velocity, Vector3.down);
    //     return v.y;
    // }

    bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, _collider.radius + .01f, ground);
    }
    #endregion

    
    #region various Methods
    void CreateHingeJoint()
    {
        HingeJoint hingeJoint = gameObject.AddComponent<HingeJoint>();
            
        Vector3 anchorPos = currentDriftPoint.position;
        anchorPos = new Vector3(anchorPos.x, transform.position.y, anchorPos.z);
        hingeJoint.anchor = transform.InverseTransformPoint(anchorPos);
            
        hingeJoint.axis = transform.InverseTransformDirection(new Vector3(0, 1, 0));
        
        
        //spring
        JointSpring spring = hingeJoint.spring;
        spring.spring = 8f;
        spring.damper = .2f;
        hingeJoint.spring = spring;
        hingeJoint.useSpring = true;
    }

    void CreateSpringJoint()
    {
        SpringJoint springJoint = gameObject.AddComponent<SpringJoint>();
        springJoint.autoConfigureConnectedAnchor = false;
        springJoint.spring = 5;
        
        //set connected Anchor (point player rotates around)
        Vector3 anchorPos = currentDriftPoint.position;
        anchorPos = new Vector3(anchorPos.x, transform.position.y, anchorPos.z);
        springJoint.connectedAnchor = anchorPos;

        //set own Anchor
        springJoint.anchor = transform.InverseTransformPoint(tonguePoint.position);
        
        //set min/max Distance
        float distance = Vector3.Distance(currentDriftPoint.position, transform.position);
        
        float minDistance = Mathf.Max(5, distance - 5);
        springJoint.minDistance = minDistance;

        float maxDistance = distance + 5;
        springJoint.maxDistance = maxDistance;
    }

    void CreateConfigurableJoint()
    {
        ConfigurableJoint configurableJoint = gameObject.AddComponent<ConfigurableJoint>();
        configurableJoint.autoConfigureConnectedAnchor = false;
        
        //set connected Anchor (point player rotates around)
        Vector3 anchorPos = currentDriftPoint.position;
        anchorPos = new Vector3(anchorPos.x, transform.position.y, anchorPos.z);
        configurableJoint.connectedAnchor = anchorPos;
        
        //set own Anchor
        configurableJoint.anchor = new Vector3(0, 0, 0);

        //Limit joint so it doesnt move on the y axis
        configurableJoint.xMotion = ConfigurableJointMotion.Limited;
        configurableJoint.yMotion = ConfigurableJointMotion.Locked;
        configurableJoint.zMotion = ConfigurableJointMotion.Limited;
        
        //Set Spring Parameters
        SoftJointLimitSpring springParameters = configurableJoint.linearLimitSpring;
        springParameters.spring = 1f;
        springParameters.damper = .2f;
        configurableJoint.linearLimitSpring = springParameters;

        SoftJointLimit softJointLimit = configurableJoint.linearLimit;
        softJointLimit.limit = 1;
        configurableJoint.linearLimit = softJointLimit;


    }

    void DestroyJoint()
    {
        Joint joint = GetComponent<Joint>();
        Destroy(joint);
    }
    
    public void CollectFirefly()
    {
        fireflyCount++;
        //later there will be UI and Player Changes
    }
    #endregion

    #region handle Input
    public void OnSteer(InputAction.CallbackContext context)
    {
        if (currentState == PlayerState.Drifting)
            horizontal = isDriftingRight ? 2 : -2;
        else
            horizontal = context.ReadValue<Vector2>().x;
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started && isGrounded)
        {
            currentState = PlayerState.Jumping;
            rb.AddForce(playerVisuals.up * jumpForce, ForceMode.Impulse);
        }
    }

    public void OnBreak(InputAction.CallbackContext context)
    {
        if(context.started)
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
        while(currentState == PlayerState.Breaking)
        {
            if(IsGrounded())
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
        else if (context.canceled)
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
        else if (context.canceled)
        {
            StopDrifting();
        }
    }

    void StartDrifting()
    {
        currentState = PlayerState.Drifting;
        lineRenderer.positionCount = 2;

        outerDriftRadius = Vector3.Distance(currentDriftPoint.position, transform.position);
        innerDriftRadius =  outerDriftRadius* driftRadiusMultiplier;

        currentTraction = tractionWhileDrifting;
    }

    void StopDrifting()
    {
        lineRenderer.positionCount = 0;
        currentState = PlayerState.Running;
        currentTraction = traction;
        
        DestroyJoint();
    }
    #endregion

    #region Math helper
    
    Vector3 RotateRight(Vector3 v)
    {
        return new Vector3(v.y, -v.x);
    }

    Vector3 RotateLeft(Vector3 v)
    {
        return new Vector3(-v.y, v.x);
    }
    #endregion
}