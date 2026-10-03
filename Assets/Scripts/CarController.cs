using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem; // <-- NEW: Required for Gamepad Controls!

public class CarController : MonoBehaviour
{
    // --- Control Mode ---
    [Header("Control Mode")]
    public bool isAI = false; 

    // --- Controller Setup ---
    [Header("Controller Connections")]
    public CarRewinder rewinder; // Drag your rewinder here
    public AudioSource hornAudio; // Drag an AudioSource here for the horn

    // --- Speed Settings ---
    [Header("Speed Settings")]
    public float maxSpeedKmph = 200f; 

    // --- Wheel Colliders ---
    [Header("Wheel Colliders")]
    public WheelCollider frontLeftW;
    public WheelCollider frontRightW;
    public WheelCollider rearLeftW;
    public WheelCollider rearRightW;

    // --- Visuals ---
    [Header("Wheel Meshes")] 
    public Transform frontLeftMesh;
    public Transform frontRightMesh;
    public Transform rearLeftMesh;
    public Transform rearRightMesh;

    private List<WheelCollider> wheels;

    // --- Specs ---
    [Header("Car Specs")]
    public float maxMotorTorque = 300f; 
    public float brakeForce = 15000f;   
    public float handbrakeForce = 20000f;   

    [Header("Steering Settings")]
    public float maxSteeringAngle = 40f;      
    public float steeringReactionSpeed = 10f; 

    [Range(10, 100)]
    public float steeringCurveFactor = 50f;   

    [Header("Braking Helper")]
    public float brakingDrag = 3.0f; 
    private float defaultDrag = 0f;

    [Header("Drift Tuning")]
    public float driftStiffness = 0.4f; 
    public float driftFrontGrip = 1.2f; 
    public float normalStiffness = 1.0f; 

    [Header("Anti-Roll Bar")]
    public float antiRollForce = 5000f; 

    [Header("ABS Settings")]
    public bool useABS = true;
    public float absThreshold = 0.25f; 

    // --- Private Inputs ---
    private float motorInput;
    private float steerInput;
    private bool isHandbraking;
    
    // Controller Trigger Splits
    private float gasInput;
    private float brakeInput;
    private Vector2 lookInput;
    
    // --- Physics Variables ---
    private float currentTorque;
    private bool isBrakingWithMotor; 
    private Rigidbody rb;
    private float currentSpeedKmph;
    private float currentSteerAngle;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(rb.centerOfMass.x, rb.centerOfMass.y - 0.2f, rb.centerOfMass.z); 
        
        wheels = new List<WheelCollider> { frontLeftW, frontRightW, rearLeftW, rearRightW };
        
        if (rearLeftW != null) 
             normalStiffness = rearLeftW.sidewaysFriction.stiffness;

        #if UNITY_6000_0_OR_NEWER
            defaultDrag = rb.linearDamping; 
        #else
            defaultDrag = rb.drag;
        #endif
    }

    // --- NEW CONTROLLER LOGIC (Receives Input System Messages) ---
    public void OnSteer(InputValue value) { if (!isAI) steerInput = value.Get<Vector2>().x; }
    public void OnAccelerate(InputValue value) { if (!isAI) gasInput = value.Get<float>(); }
    public void OnBrake(InputValue value) { if (!isAI) brakeInput = value.Get<float>(); }
    public void OnHandbrake(InputValue value) { if (!isAI) isHandbraking = value.isPressed; }
    public void OnLook(InputValue value) { if (!isAI) lookInput = value.Get<Vector2>(); }

    public void OnHorn(InputValue value) 
    { 
        if (isAI || hornAudio == null) return;
        if (value.isPressed) hornAudio.Play();
        else hornAudio.Stop();
    }

    public void OnChangeCamera(InputValue value)
    {
        if (isAI) return;
        if (value.isPressed)
        {
            Debug.Log("Camera Change Button Pressed!");
            // Insert camera swap logic here
        }
    }

   public void OnRewind(InputValue value)
    {
        if (isAI) return;
        
        // This acts like a radio tower. It tells EVERY car in the game that 'Y' is pressed!
        CarRewinder.isGlobalRewindPressed = value.isPressed;
    }

    void Update()
    {
        // THE FIX: Listen to the Radio Signal to stop updating wheels during time travel!
        if (CarRewinder.isGlobalRewindPressed) return; 

        if (!isAI)
        {
            motorInput = gasInput - brakeInput;
        }
        UpdateWheelVisuals();
    }

    void FixedUpdate()
    {

        if (RaceManager.Instance != null && !RaceManager.Instance.raceIsActive) return;
        // Freeze physics entirely if we are time traveling
        if (rewinder != null && rewinder.isRewinding) return;

        #if UNITY_6000_0_OR_NEWER
            Vector3 velocity = rb.linearVelocity;
        #else
            Vector3 velocity = rb.velocity;
        #endif

        float forwardSpeed = Vector3.Dot(transform.forward, velocity);
        currentSpeedKmph = velocity.magnitude * 3.6f;

        isBrakingWithMotor = false;
        if (motorInput < 0 && forwardSpeed > 1f) isBrakingWithMotor = true;
        else if (motorInput > 0 && forwardSpeed < -1f) isBrakingWithMotor = true;

        HandleMotor(isBrakingWithMotor);
        HandleSteering();
        HandleBraking(isBrakingWithMotor); 
        HandleAntiRoll(frontLeftW, frontRightW); 
        HandleAntiRoll(rearLeftW, rearRightW);   
        LimitSpeed();
        
        rb.AddForce(-transform.up * 50f * velocity.magnitude); 
    }

    private void LimitSpeed()
    {
        #if UNITY_6000_0_OR_NEWER
            if (currentSpeedKmph > maxSpeedKmph)
                rb.linearVelocity = rb.linearVelocity.normalized * (maxSpeedKmph / 3.6f);
        #else
            if (currentSpeedKmph > maxSpeedKmph)
                rb.velocity = rb.velocity.normalized * (maxSpeedKmph / 3.6f);
        #endif
    }

    public void SetInput(float motor, float steer, bool handbrake)
    {
        motorInput = motor;
        steerInput = steer;
        isHandbraking = handbrake;
    }

    private void HandleMotor(bool isBraking)
    {
        if (isBraking || isHandbraking) currentTorque = 0;
        else currentTorque = motorInput * maxMotorTorque;

        foreach(var wheel in wheels) wheel.motorTorque = currentTorque;
    }

    private void HandleSteering()
    {
        float speedFactor = currentSpeedKmph / steeringCurveFactor;
        float currentMaxAngle = maxSteeringAngle / (1.0f + speedFactor);

        float targetAngle = steerInput * currentMaxAngle;

        currentSteerAngle = Mathf.Lerp(currentSteerAngle, targetAngle, Time.deltaTime * steeringReactionSpeed);

        frontLeftW.steerAngle = currentSteerAngle;
        frontRightW.steerAngle = currentSteerAngle;
    }

    private void HandleBraking(bool isBraking)
    {
        if (isHandbraking)
        {
            rearLeftW.brakeTorque = handbrakeForce;
            rearRightW.brakeTorque = handbrakeForce;
            frontLeftW.brakeTorque = 0f; 
            frontRightW.brakeTorque = 0f;
            SetStiffness(driftFrontGrip, driftStiffness);
            SetDrag(defaultDrag + 1f);
        }
        else if (isBraking)
        {
            SetDrag(defaultDrag + brakingDrag);
            foreach (var wheel in wheels)
            {
                if (useABS)
                {
                    WheelHit hit;
                    if(wheel.GetGroundHit(out hit) && Mathf.Abs(hit.forwardSlip) > absThreshold)
                        wheel.brakeTorque = 0; 
                    else
                        wheel.brakeTorque = brakeForce;
                }
                else wheel.brakeTorque = brakeForce;
            }
            SetStiffness(normalStiffness, normalStiffness);
        }
        else
        {
            SetDrag(defaultDrag);
            foreach (var wheel in wheels) wheel.brakeTorque = 0f;
            SetStiffness(normalStiffness, normalStiffness);
        }
    }

    private void SetDrag(float dragValue)
    {
        #if UNITY_6000_0_OR_NEWER
            rb.linearDamping = dragValue;
        #else
            rb.drag = dragValue;
        #endif
    }
    
    private void SetStiffness(float frontStiffness, float rearStiffness)
    {
        UpdateFriction(frontLeftW, frontStiffness);
        UpdateFriction(frontRightW, frontStiffness);
        UpdateFriction(rearLeftW, rearStiffness);
        UpdateFriction(rearRightW, rearStiffness);
    }

    private void UpdateFriction(WheelCollider wheel, float stiffness)
    {
        WheelFrictionCurve friction = wheel.sidewaysFriction;
        friction.stiffness = stiffness;
        wheel.sidewaysFriction = friction;
    }

    private void HandleAntiRoll(WheelCollider leftW, WheelCollider rightW)
    {
        WheelHit hit;
        float leftTravel = 1.0f; 
        float rightTravel = 1.0f; 

        if (leftW.GetGroundHit(out hit))
            leftTravel = (-leftW.transform.InverseTransformPoint(hit.point).y - leftW.radius) / leftW.suspensionDistance;

        if (rightW.GetGroundHit(out hit))
            rightTravel = (-rightW.transform.InverseTransformPoint(hit.point).y - rightW.radius) / rightW.suspensionDistance;

        float roll = leftTravel - rightTravel;
        float appliedForce = roll * antiRollForce;

        if (leftW.isGrounded)
            leftW.attachedRigidbody.AddForceAtPosition(leftW.transform.up * -appliedForce, leftW.transform.position);

        if (rightW.isGrounded)
            rightW.attachedRigidbody.AddForceAtPosition(rightW.transform.up * appliedForce, rightW.transform.position);
    }
    
    private void UpdateWheelVisuals()
    {
        UpdateWheelPose(frontLeftW, frontLeftMesh);
        UpdateWheelPose(frontRightW, frontRightMesh);
        UpdateWheelPose(rearLeftW, rearLeftMesh);
        UpdateWheelPose(rearRightW, rearRightMesh);
    }

    private void UpdateWheelPose(WheelCollider collider, Transform transform)
    {
        Vector3 pos;
        Quaternion quat;
        collider.GetWorldPose(out pos, out quat);
        transform.position = pos;
        transform.rotation = quat;
    }

}