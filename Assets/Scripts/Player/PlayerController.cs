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
        Falling,
        JumpDrifting,
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
    private bool isFalling;
    private bool justStartedJumping;
    private bool isDriftingRight;


    [Header("References")]
    [SerializeField] private Transform playerVisuals;
    [SerializeField] private Transform tonguePoint;
    [SerializeField] private Transform lookAt;
    [SerializeField] private DriftPointContainer rightDriftPointContainer;
    [SerializeField] private DriftPointContainer leftDriftPointContainer;
    private Rigidbody rb;
    private SphereCollider _collider;
    private PlayerInput controls;
    private LineRenderer lineRenderer;

    //Coroutine slowDown;
    //Coroutine speedUp;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        _collider = GetComponent<SphereCollider>();
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0;

        currentTraction = traction;

        if (controls == null)
        {
            controls = new PlayerInput();
            controls.Enable();
            controls.P_Controls.SetCallbacks(this);
        }

        rightDriftPointContainer.SetRightContainer();
    }

    #region physics
    void FixedUpdate()
    {
        //update current State 
        isGrounded = IsGrounded();
        isFalling = IsFalling();
        CheckState();

        //copy ground rotation
        Vector3 lerpTo;
        if (isGrounded)
        {
            RaycastHit groundHit;
            Physics.Raycast(transform.position, -playerVisuals.up, out groundHit, _collider.radius + 2, ground);
            lerpTo = groundHit.normal;
        }
        else
            lerpTo = Vector3.up;

        float eulerAnglesY = playerVisuals.eulerAngles.y;
        playerVisuals.up = Vector3.Lerp(playerVisuals.up, lerpTo, Time.fixedDeltaTime * 8f);
        playerVisuals.Rotate(0, eulerAnglesY, 0);

        //steer or drift
        if (IsDrifting())
            Drift();
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
        if (rb.velocity.magnitude < maxSpeed && currentState != PlayerState.Breaking)
            rb.AddForce(playerVisuals.forward * acceleration, ForceMode.Acceleration);

        //traction
        Debug.DrawRay(transform.position, playerVisuals.forward * 4);
        Debug.DrawRay(transform.position, rb.velocity.normalized * 4, Color.cyan);

        float gravity = rb.velocity.y;
        Vector3 velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        rb.velocity = Vector3.Lerp(velocity.normalized, playerVisuals.forward, currentTraction * Time.fixedDeltaTime) * velocity.magnitude;
        rb.velocity += new Vector3(0, gravity, 0);



        //multiply Gravity after jump peak
        rb.velocity += (isFalling ? Physics.gravity * gravitationMultiplier : Physics.gravity) * Time.fixedDeltaTime;
    }

    private void LateUpdate()
    {
        if (IsDrifting())
        {
            lineRenderer.SetPosition(0, tonguePoint.position);
            lineRenderer.SetPosition(1, currentDriftPoint.position);
        }
    }

    void CheckState()
    {
        if (!isGrounded && isFalling && !IsDrifting())
            currentState = PlayerState.Falling;
        else if (currentState == PlayerState.Falling && isGrounded)
        {
            currentState = PlayerState.Running;
            // rb.constraints = RigidbodyConstraints.FreezePositionY;
        }
        else if (currentState == PlayerState.JumpDrifting && isGrounded && !justStartedJumping)
            currentState = PlayerState.Drifting;

        justStartedJumping = false;

    }

    void Drift()
    {
        //calculate how much the player should face the wanted drift Rotation 
        float distanceToDriftPoint = Vector3.Distance(currentDriftPoint.position, transform.position);
        float t = Mathf.Max(0, distanceToDriftPoint - innerDriftRadius);
        t /= outerDriftRadius - innerDriftRadius;

        Vector3 vecToPoint = currentDriftPoint.position - transform.position;

        //calculate the angle the player should face at the most outer distance from driftpoint
        float wantedAngle = Vector2.SignedAngle(Vector2.up, new Vector2(vecToPoint.x, vecToPoint.z));
        wantedAngle = -wantedAngle;
        wantedAngle += isDriftingRight ? -90 + driftOversteer : 90 - driftOversteer;

        float currentAngle = playerVisuals.transform.eulerAngles.y;
        wantedAngle = Mathf.Lerp(currentAngle, wantedAngle, t);

        //get current rotation realtive to distance to drift point
        Vector3 wantedEulerAngles = new Vector3(playerVisuals.eulerAngles.x, wantedAngle, playerVisuals.eulerAngles.z);
        playerVisuals.rotation = Quaternion.RotateTowards(playerVisuals.rotation, Quaternion.Euler(wantedEulerAngles), driftTurnSpeed * Time.fixedDeltaTime);
    }
    #endregion

    #region Bools
    bool IsGrounded()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, _collider.radius + .01f, ground);
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
            currentState = currentState == PlayerState.Drifting ? PlayerState.JumpDrifting : PlayerState.Jumping;
            rb.AddForce(playerVisuals.up * jumpForce, ForceMode.Impulse);
            justStartedJumping = true;
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
        currentState = isGrounded ? PlayerState.Drifting : PlayerState.JumpDrifting;
        lineRenderer.positionCount = 2;

        outerDriftRadius = Vector3.Distance(currentDriftPoint.position, transform.position);
        innerDriftRadius = outerDriftRadius * driftRadiusMultiplier;

        currentTraction = tractionWhileDrifting;
    }

    void StopDrifting()
    {
        lineRenderer.positionCount = 0;
        currentState = currentState == PlayerState.JumpDrifting ? PlayerState.Jumping : PlayerState.Running;
        currentTraction = traction;
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

    #region slowBox
    void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.tag == "SlowBox")
        {
            StartCoroutine("slowDown");
        }
    }
    void OnTriggerExit(Collider collider)
    {
        if (collider.gameObject.tag == "SlowBox")
        {
            StopCoroutine("slowDown");
            maxSpeed = 30;
        }
    }
    IEnumerator slowDown()
    {
        float t = 0;
        float from = maxSpeed;
        float To = maxSpeed - 17;

        while(t < 0.5)
        {
            maxSpeed = Mathf.Lerp(from, To, t);
            t += Time.deltaTime;
            yield return null;
        }

        maxSpeed = To;

    }
    #endregion
}