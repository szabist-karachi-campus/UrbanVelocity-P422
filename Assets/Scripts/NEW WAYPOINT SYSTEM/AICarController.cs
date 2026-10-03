using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AICarController : MonoBehaviour
{
    [Header("Spawner Integration")]
    public GameObject minimapIcon;
    
    [Header("Lane Navigation")]
    public WaypointSystem trackSystem;
    public int currentWaypointIndex = 0;
    public float waypointThreshold = 12f;

    [Tooltip("0 = Center, 1 = Right Lane, -1 = Left Lane")]
    public int currentLane = 0;
    private float currentLaneOffset = 0f;

    [Header("Car Specs")]
    public float motorForce = 1500f;
    public float maxSteerAngle = 35f;
    public float brakeForce = 15000f;
    public float maxSpeed = 100f;
    public float downforce = 50f;

    [Header("Rubber Banding")]
    public bool enableRubberBanding = true;
    public Transform playerTarget;
    public float maxDistanceThreshold = 150f;
    
    private float rbMaxBoost = 1.25f; 
    private float rbMaxNerf = 0.75f;

    [Header("Stabilization")]
    public float steerSmoothing = 5f;
    private float smoothedSteerInput = 0f;

    [Header("Sensors (7-Point System)")]
    public LayerMask obstacleMask;
    public float sensorHeight = 0.8f;
    public float frontBumperZ = 2.5f;
    public float rearBumperZ = -2.5f;
    public float carWidthX = 1.0f;
    
    [Header("Dual Front Sensor Settings")]
    public float dualSensorForwardZ = 0f; 
    public float frontSensorOffsetX = 0.8f;
    public float upwardSensorAngle = 5f;
    public float baseFrontSensorLength = 10f;
    public float frontAngleLength = 10f;
    public float sideSensorLength = 3f;
    public float rearSensorLength = 5f;
    public float frontSensorAngle = 25f;

    public float laneSwitchCooldown = 2.5f;
    private float laneSwitchTimer = 0f;
    private bool obstacleAhead = false;
    private float immediateAvoidanceSteer = 0f;

    [Header("Stuck & Respawn Settings")]
    public float stuckVelocityThreshold = 1f;
    public float timeBeforeRespawn = 3f;
    private float stuckTimer = 0f;

    [Header("Wheel Colliders")]
    public WheelCollider frontLeftWheel, frontRightWheel;
    public WheelCollider rearLeftWheel, rearRightWheel;

    [Header("Wheel Meshes (Visuals)")]
    public Transform frontLeftWheelMesh;
    public Transform frontRightWheelMesh;
    public Transform rearLeftWheelMesh;
    public Transform rearRightWheelMesh;

    private Rigidbody carRigidbody;
    private float targetAvoidance = 0f;
    private bool blockedByStatic = false;

    private bool isBoxedIn = false;
    private bool sideHit = false;

    void Start()
    {
        carRigidbody = GetComponent<Rigidbody>();
carRigidbody.centerOfMass = new Vector3(0, -0.25f, 0.1f);
        carRigidbody.WakeUp();

        if (trackSystem != null)
        {
            currentLaneOffset = trackSystem.laneWidth * currentLane;
        }
        playerTarget = null; 
    }

    void FixedUpdate()
    {
        if (trackSystem == null || trackSystem.nodes.Count == 0) return;

        if (RaceManager.Instance != null && !RaceManager.Instance.raceIsActive)
        {
            ApplyBrakes(brakeForce);
            return; 
        }

        FindPlayerIfNeeded();
        HandleSensorsAndLanes();
        DriveAndBrake();
        ApplyDownforce();
        CheckWaypointDistance();
        CheckIfStuck();
    }
    
    void Update()
    {
        UpdateWheelPoses();
    }

    private void FindPlayerIfNeeded()
    {
        if (enableRubberBanding && playerTarget == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) 
            {
                playerTarget = p.transform;
            }
        }
    }

    public void PrepareAI(WaypointSystem track, int lane, float newMaxSpeed, int difficultyLevel)
    {
        trackSystem = track;
        currentLane = lane; 
        this.maxSpeed = newMaxSpeed; 

        if (trackSystem != null && trackSystem.nodes.Count > 0)
        {
            float closestDistance = Mathf.Infinity;
            int closestNodeIndex = 0;

            for (int i = 0; i < trackSystem.nodes.Count; i++)
            {
                float distanceToNode = Vector3.Distance(transform.position, trackSystem.nodes[i].position);
                if (distanceToNode < closestDistance)
                {
                    closestDistance = distanceToNode;
                    closestNodeIndex = i;
                }
            }
            currentWaypointIndex = (closestNodeIndex + 1) % trackSystem.nodes.Count;
        }

        if (difficultyLevel == 0) 
        {
            rbMaxBoost = 1.25f; 
            rbMaxNerf = 0.95f;  
            maxDistanceThreshold = 20f;
        }
        else if (difficultyLevel == 1) 
        {
            rbMaxBoost = 1.25f; 
            rbMaxNerf = 0.95f;  
            maxDistanceThreshold = 15f;
        }
        else if (difficultyLevel == 2) 
        {
            // THE FIX: Hard mode is now ruthless. No mercy if winning, massive boost if losing.
            rbMaxBoost = 1.4f; 
            rbMaxNerf = 1.0f;  
            maxDistanceThreshold = 15f; 
        }
    }

    private bool IsValidObstacle(RaycastHit hit)
    {
        if (hit.collider == null) return false;
        if (hit.transform.root == transform) return false;
        
        if (hit.collider.CompareTag("Road") || 
            hit.collider.CompareTag("Wall") || 
            hit.collider.CompareTag("obstacles") || 
            hit.collider.CompareTag("Obstacles")) 
        {
            return false;
        }
        
        return true;
    }

    private void HandleSensorsAndLanes()
    {
        laneSwitchTimer += Time.deltaTime;
        obstacleAhead = false;

        float speed = carRigidbody.linearVelocity.magnitude;
        float dynamicFrontLength = baseFrontSensorLength + (speed * 0.7f);

        Vector3 tiltedForward = Quaternion.AngleAxis(-upwardSensorAngle, transform.right) * transform.forward;
        Vector3 centerFront = transform.position + (transform.forward * frontBumperZ) + (transform.up * sensorHeight);
        Vector3 dualCenterFront = transform.position + (transform.forward * (frontBumperZ + dualSensorForwardZ)) + (transform.up * sensorHeight);
        
        Vector3 dualRightPos = dualCenterFront + (transform.right * frontSensorOffsetX);
        Vector3 dualLeftPos = dualCenterFront - (transform.right * frontSensorOffsetX);
        
        Vector3 headLightRight = centerFront + (transform.right * carWidthX);
        Vector3 headLightLeft = centerFront - (transform.right * carWidthX);
        Vector3 doorRight = transform.position + (transform.right * carWidthX) + (transform.up * sensorHeight);
        Vector3 doorLeft = transform.position - (transform.right * carWidthX) + (transform.up * sensorHeight);
        Vector3 tailLightRight = transform.position + (transform.forward * rearBumperZ) + (transform.right * carWidthX) + (transform.up * sensorHeight);
        Vector3 tailLightLeft = transform.position + (transform.forward * rearBumperZ) - (transform.right * carWidthX) + (transform.up * sensorHeight);

        RaycastHit fsrHit, fslHit, fRightHit, fLeftHit, sRightHit, sLeftHit, rRightHit, rLeftHit;

        bool hitF_StraightRight = Physics.Raycast(dualRightPos, tiltedForward, out fsrHit, dynamicFrontLength, obstacleMask) && IsValidObstacle(fsrHit);
        bool hitF_StraightLeft = Physics.Raycast(dualLeftPos, tiltedForward, out fslHit, dynamicFrontLength, obstacleMask) && IsValidObstacle(fslHit);
        
        bool hitFR = Physics.Raycast(headLightRight, Quaternion.AngleAxis(frontSensorAngle, transform.up) * transform.forward, out fRightHit, frontAngleLength, obstacleMask) && IsValidObstacle(fRightHit);
        bool hitFL = Physics.Raycast(headLightLeft, Quaternion.AngleAxis(-frontSensorAngle, transform.up) * transform.forward, out fLeftHit, frontAngleLength, obstacleMask) && IsValidObstacle(fLeftHit);
        
        bool hitSR = Physics.Raycast(doorRight, transform.right, out sRightHit, sideSensorLength, obstacleMask) && IsValidObstacle(sRightHit);
        bool hitSL = Physics.Raycast(doorLeft, -transform.right, out sLeftHit, sideSensorLength, obstacleMask) && IsValidObstacle(sLeftHit);
        
        bool hitRR = Physics.Raycast(tailLightRight, -transform.forward, out rRightHit, rearSensorLength, obstacleMask) && IsValidObstacle(rRightHit);
        bool hitRL = Physics.Raycast(tailLightLeft, -transform.forward, out rLeftHit, rearSensorLength, obstacleMask) && IsValidObstacle(rLeftHit);

        if (hitF_StraightRight || hitF_StraightLeft || hitFR || hitFL)
        {
            obstacleAhead = true;

            if (laneSwitchTimer > laneSwitchCooldown)
            {
                bool rightClear = !hitSR && !hitRR && !hitFR;
                bool leftClear = !hitSL && !hitRL && !hitFL;

                if (currentLane == 0) 
                {
                    if (rightClear) { currentLane++; laneSwitchTimer = 0f; }
                    else if (leftClear) { currentLane--; laneSwitchTimer = 0f; }
                }
                else if (currentLane == -1) 
                {
                    if (rightClear) { currentLane++; laneSwitchTimer = 0f; }
                    else if (!hitSR && !hitRR) { currentLane = 1; laneSwitchTimer = 0f; }
                }
                else if (currentLane == 1) 
                {
                    if (leftClear) { currentLane--; laneSwitchTimer = 0f; }
                    else if (!hitSL && !hitRL) { currentLane = -1; laneSwitchTimer = 0f; }
                }
            }
        }

        sideHit = hitSR || hitSL;
        
        isBoxedIn = false;
        if (obstacleAhead && hitSR && hitSL)
        {
            float physicalGap = sRightHit.distance + sLeftHit.distance;
            float requiredSafetyWidth = carWidthX * 2.2f; 
            
            if (physicalGap < requiredSafetyWidth)
            {
                isBoxedIn = true;
            }
        }

        float rightPressure = 0f;
        float leftPressure = 0f;

        if (hitF_StraightRight) rightPressure = Mathf.Max(rightPressure, 1f - (fsrHit.distance / dynamicFrontLength));
        if (hitFR) rightPressure = Mathf.Max(rightPressure, 1f - (fRightHit.distance / frontAngleLength));
        if (hitSR) rightPressure = Mathf.Max(rightPressure, 1f - (sRightHit.distance / sideSensorLength));

        if (hitF_StraightLeft) leftPressure = Mathf.Max(leftPressure, 1f - (fslHit.distance / dynamicFrontLength));
        if (hitFL) leftPressure = Mathf.Max(leftPressure, 1f - (fLeftHit.distance / frontAngleLength));
        if (hitSL) leftPressure = Mathf.Max(leftPressure, 1f - (sLeftHit.distance / sideSensorLength));

        targetAvoidance = Mathf.Clamp((leftPressure - rightPressure) * 0.85f, -0.4f, 0.4f); 
        immediateAvoidanceSteer = Mathf.Lerp(immediateAvoidanceSteer, targetAvoidance, Time.deltaTime * 6f);
    }

    private void DriveAndBrake()
    {
        Transform currentNode = trackSystem.nodes[currentWaypointIndex];
        Vector3 forwardDir;

        if (currentWaypointIndex < trackSystem.nodes.Count - 1)
            forwardDir = (trackSystem.nodes[currentWaypointIndex + 1].position - currentNode.position).normalized;
        else
            forwardDir = transform.forward;

        forwardDir.y = 0; 
        forwardDir.Normalize();

        Vector3 rightDir = Vector3.Cross(Vector3.up, forwardDir).normalized;

        float targetLaneOffset = trackSystem.laneWidth * currentLane;
        currentLaneOffset = Mathf.Lerp(currentLaneOffset, targetLaneOffset, Time.deltaTime * 5f);

        Vector3 targetPosition = currentNode.position + (rightDir * currentLaneOffset);
        
        Vector3 toWaypoint = targetPosition - transform.position;
        toWaypoint.y = 0; 
        
        Vector3 relativeVector = transform.InverseTransformPoint(targetPosition);
        relativeVector.y = 0; 

        float steerInput = relativeVector.x / relativeVector.magnitude;
        steerInput += immediateAvoidanceSteer;
        steerInput = Mathf.Clamp(steerInput, -1f, 1f);

        smoothedSteerInput = Mathf.Lerp(smoothedSteerInput, steerInput, Time.deltaTime * steerSmoothing);

        float currentSpeed = carRigidbody.linearVelocity.magnitude * 3.6f;

        float speedFactor = currentSpeed / maxSpeed;
        float dynamicSteerAngle = Mathf.Lerp(maxSteerAngle, maxSteerAngle * 0.35f, speedFactor); 
        
        float currentSteerAngle = smoothedSteerInput * dynamicSteerAngle;
        frontLeftWheel.steerAngle = currentSteerAngle;
        frontRightWheel.steerAngle = currentSteerAngle;

        float angleToWaypoint = Vector3.Angle(transform.forward, toWaypoint);
        
        float upcomingAngle = 0f;
        if (currentWaypointIndex < trackSystem.nodes.Count - 2)
        {
            Vector3 nextNextNodeDir = (trackSystem.nodes[currentWaypointIndex + 2].position - trackSystem.nodes[currentWaypointIndex + 1].position).normalized;
            upcomingAngle = Vector3.Angle(forwardDir, nextNextNodeDir);
        }

        bool isSharpCorner = angleToWaypoint > 30f || upcomingAngle > 35f; 
        
        bool isChangingLanes = Mathf.Abs(currentLaneOffset - targetLaneOffset) > 0.5f;

        float currentMaxSpeed = maxSpeed;
        float currentMotorForce = motorForce;

        if (enableRubberBanding && playerTarget != null)
        {
            float distToPlayer = Vector3.Distance(transform.position, playerTarget.position);
            Vector3 dirToPlayer = (playerTarget.position - transform.position).normalized;
            float isPlayerAhead = Vector3.Dot(forwardDir, dirToPlayer);

            float distanceScale = Mathf.InverseLerp(20f, maxDistanceThreshold, distToPlayer);
            distanceScale = Mathf.SmoothStep(0f, 1f, distanceScale);

            if (isPlayerAhead > 0.1f) 
            {
                float dynamicBoost = Mathf.Lerp(1.0f, rbMaxBoost, distanceScale);
                currentMaxSpeed *= dynamicBoost;
                currentMotorForce *= dynamicBoost;
            }
            else if (isPlayerAhead < -0.1f)
            {
                float dynamicNerf = Mathf.Lerp(1.0f, rbMaxNerf, distanceScale);
                currentMaxSpeed *= dynamicNerf;
                currentMotorForce *= dynamicNerf;
            }
        }

        // THE FIX: Increased to 75% max speed so they blast through corners
        float corneringSpeedLimit = currentMaxSpeed * 0.75f; 

        if (isSharpCorner && currentSpeed > corneringSpeedLimit)
        {
            ApplyBrakes(brakeForce * 0.5f);
        }
        else if (isBoxedIn)
        {
            ApplyBrakes(brakeForce * 1.5f);
            carRigidbody.linearVelocity = Vector3.Lerp(carRigidbody.linearVelocity, Vector3.zero, Time.deltaTime * 3f);
        }
        else if (obstacleAhead && !isChangingLanes && Mathf.Abs(immediateAvoidanceSteer) < 0.15f)
        {
            ApplyBrakes(brakeForce * 0.4f);
        }
        else if (sideHit)
        {
            ReleaseBrakes();
            if (currentSpeed < currentMaxSpeed) ApplyGas(currentMotorForce * 0.85f);
        }
        else
        {
            ReleaseBrakes();
            if (currentSpeed < currentMaxSpeed)
            {
                // THE FIX: Giving them 90% throttle mid-curve instead of 85%
                float cornerSpeedReduction = isSharpCorner ? 0.90f : 1f;
                ApplyGas(currentMotorForce * cornerSpeedReduction);
            }
            else ApplyGas(0);
        }
    }

    private void ApplyDownforce()
    {
        carRigidbody.AddForce(-transform.up * downforce * carRigidbody.linearVelocity.magnitude);
    }

    private void CheckWaypointDistance()
    {
        Vector3 targetPos = trackSystem.nodes[currentWaypointIndex].position;
        Vector3 flatTarget = new Vector3(targetPos.x, transform.position.y, targetPos.z);
        float distToCurrent = Vector3.Distance(transform.position, flatTarget);

        bool reachedByRadius = distToCurrent < waypointThreshold;

        Vector3 dirToWaypoint = (flatTarget - transform.position).normalized;
        bool drovePastIt = Vector3.Dot(transform.forward, dirToWaypoint) < 0f && distToCurrent < (waypointThreshold * 3f);

        if (reachedByRadius || drovePastIt)
        {
            if (currentWaypointIndex < trackSystem.nodes.Count - 1)
            {
                currentWaypointIndex++;
            }
        }
    }

    private void CheckIfStuck()
    {
        blockedByStatic = false;

        if (obstacleAhead)
        {
            float speed = carRigidbody.linearVelocity.magnitude;
            float dynamicFrontLength = baseFrontSensorLength + (speed * 0.7f);
            Vector3 tiltedForward = Quaternion.AngleAxis(-upwardSensorAngle, transform.right) * transform.forward;
            Vector3 centerFront = transform.position + (transform.forward * (frontBumperZ + dualSensorForwardZ)) + (transform.up * sensorHeight);
            
            Vector3 dualRightPos = centerFront + (transform.right * frontSensorOffsetX);
            Vector3 dualLeftPos = centerFront - (transform.right * frontSensorOffsetX);

            bool rightHit = Physics.Raycast(dualRightPos, tiltedForward, out RaycastHit hitR, dynamicFrontLength, obstacleMask);
            bool leftHit = Physics.Raycast(dualLeftPos, tiltedForward, out RaycastHit hitL, dynamicFrontLength, obstacleMask);

            if ((rightHit && hitR.collider.attachedRigidbody == null) ||
                (leftHit && hitL.collider.attachedRigidbody == null))
            {
                blockedByStatic = true;
            }
        }

        if (carRigidbody.linearVelocity.magnitude < stuckVelocityThreshold && (!obstacleAhead || blockedByStatic))
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer >= timeBeforeRespawn) Respawn();
        }
        else
        {
            stuckTimer = 0f;
        }
    }

    private void Respawn()
    {
        stuckTimer = 0f;
        Vector3 respawnPos = trackSystem.nodes[currentWaypointIndex].position;

        Vector3 forwardDir;
        if (currentWaypointIndex < trackSystem.nodes.Count - 1)
            forwardDir = (trackSystem.nodes[currentWaypointIndex + 1].position - respawnPos).normalized;
        else
            forwardDir = transform.forward; 

        forwardDir.y = 0;
        forwardDir.Normalize();

        Vector3 rightDir = Vector3.Cross(Vector3.up, forwardDir).normalized;
        respawnPos += (rightDir * trackSystem.laneWidth * currentLane);

        respawnPos.y += 1.5f;
        transform.position = respawnPos;

        if (forwardDir != Vector3.zero) 
        {
            transform.rotation = Quaternion.LookRotation(forwardDir);
        }

        carRigidbody.linearVelocity = Vector3.zero;
        carRigidbody.angularVelocity = Vector3.zero;
    }

    private void ApplyBrakes(float force)
    {
        rearLeftWheel.motorTorque = 0;
        rearRightWheel.motorTorque = 0;
        frontLeftWheel.brakeTorque = force;
        frontRightWheel.brakeTorque = force;
        rearLeftWheel.brakeTorque = force;
        rearRightWheel.brakeTorque = force;
    }

    private void ReleaseBrakes()
    {
        frontLeftWheel.brakeTorque = 0;
        frontRightWheel.brakeTorque = 0;
        rearLeftWheel.brakeTorque = 0;
        rearRightWheel.brakeTorque = 0;
    }

    private void ApplyGas(float torque)
    {
        rearLeftWheel.motorTorque = torque;
        rearRightWheel.motorTorque = torque;
    }

    private void UpdateWheelPoses()
    { 
        UpdateSingleWheel(frontLeftWheel, frontLeftWheelMesh);
        UpdateSingleWheel(frontRightWheel, frontRightWheelMesh);
        UpdateSingleWheel(rearLeftWheel, rearLeftWheelMesh);
        UpdateSingleWheel(rearRightWheel, rearRightWheelMesh);
    }

    private void UpdateSingleWheel(WheelCollider collider, Transform mesh)
    {
        if (mesh == null) return;
        
        collider.GetWorldPose(out Vector3 pos, out Quaternion rot);
        mesh.position = pos;
        mesh.rotation = rot;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;

        float speed = (Application.isPlaying && carRigidbody != null) ? carRigidbody.linearVelocity.magnitude : 0f;
        float dynamicFrontLength = baseFrontSensorLength + (speed * 0.7f);

        Vector3 tiltedForward = Quaternion.AngleAxis(-upwardSensorAngle, transform.right) * transform.forward;
        Vector3 centerFront = transform.position + (transform.forward * frontBumperZ) + (transform.up * sensorHeight);
        
        Vector3 dualCenterFront = transform.position + (transform.forward * (frontBumperZ + dualSensorForwardZ)) + (transform.up * sensorHeight);
        Vector3 dualRightPos = dualCenterFront + (transform.right * frontSensorOffsetX);
        Vector3 dualLeftPos = dualCenterFront - (transform.right * frontSensorOffsetX);
        
        Vector3 headLightRight = centerFront + (transform.right * carWidthX);
        Vector3 headLightLeft = centerFront - (transform.right * carWidthX);
        Vector3 doorRight = transform.position + (transform.right * carWidthX) + (transform.up * sensorHeight);
        Vector3 doorLeft = transform.position - (transform.right * carWidthX) + (transform.up * sensorHeight);
        Vector3 tailLightRight = transform.position + (transform.forward * rearBumperZ) + (transform.right * carWidthX) + (transform.up * sensorHeight);
        Vector3 tailLightLeft = transform.position + (transform.forward * rearBumperZ) - (transform.right * carWidthX) + (transform.up * sensorHeight);

        void DrawSensorRay(Vector3 start, Vector3 dir, float length)
        {
            if (Physics.Raycast(start, dir, out RaycastHit hit, length, obstacleMask) && IsValidObstacle(hit))
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(start, hit.point);
            }
            else
            {
                Gizmos.color = Color.green;
                Gizmos.DrawRay(start, dir * length);
            }
        }

        DrawSensorRay(dualRightPos, tiltedForward, dynamicFrontLength);
        DrawSensorRay(dualLeftPos, tiltedForward, dynamicFrontLength);
        
        DrawSensorRay(headLightRight, Quaternion.AngleAxis(frontSensorAngle, transform.up) * transform.forward, frontAngleLength);
        DrawSensorRay(headLightLeft, Quaternion.AngleAxis(-frontSensorAngle, transform.up) * transform.forward, frontAngleLength);
        
        DrawSensorRay(doorRight, transform.right, sideSensorLength);
        DrawSensorRay(doorLeft, -transform.right, sideSensorLength);
        
        DrawSensorRay(tailLightRight, -transform.forward, rearSensorLength);
        DrawSensorRay(tailLightLeft, -transform.forward, rearSensorLength);
    }
}