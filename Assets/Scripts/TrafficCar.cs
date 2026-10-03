using UnityEngine;



public class TrafficCar : MonoBehaviour

{

    [Header("Car Power")]

    public float motorForce = 2000f;

    public float brakeForce = 6000f;

    private float myTopSpeed;



    private float currentSpeedLimit;



    [Header("Sensors (Eyes)")]

    public float sensorLen = 15f;

    public LayerMask obstacleMask; 

    public bool showDebugGizmos = true;



    [Header("Lane Changing")]

    public float laneChangeSpeed = 5f;

    private float targetLaneOffset;

    private float currentLaneOffset;

    private float laneChangeCooldown = 0f;



    [Header("Safety")]

    public float stuckTimeThreshold = 4f;

    private float stuckTimer = 0f;

    private float initialGracePeriod = 3f;



    [Header("Wheels")]

    public WheelCollider fl, fr, rl, rr;

    public Transform fl_mesh, fr_mesh, rl_mesh, rr_mesh;



    private WaypointSystem currentPath;

    private int currentNode;

    private Rigidbody rb;

    private bool isBraking = false;



    // THE FIX: Pre-allocate the memory array ONCE right here.

    // This stops the 1.1 MB memory leak from happening every frame!

    private Collider[] hitColliders = new Collider[10];



    void Start()

    {

        rb = GetComponent<Rigidbody>();

        rb.centerOfMass = new Vector3(0, -0.5f, 0);

        rb.sleepThreshold = 0f;

        rb.WakeUp();

    }



    public void Initialize(WaypointSystem path, int node, float offset, float randomSpeed)
    {
        currentPath = path;
        currentNode = node;
        targetLaneOffset = offset;
        currentLaneOffset = offset;
        myTopSpeed = randomSpeed;

        // --- THE POOLING FIX ---
        // Wipe the car's memory so it doesn't instantly self-destruct!
        stuckTimer = 0f;
        initialGracePeriod = 3f;
        isBraking = false;
        
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        // -----------------------
    }



    void FixedUpdate()

    {

        if (currentPath == null) return;



        if (laneChangeCooldown > 0) laneChangeCooldown -= Time.fixedDeltaTime;

        if (initialGracePeriod > 0) initialGracePeriod -= Time.fixedDeltaTime;



        // Move Lane Offset

        currentLaneOffset = Mathf.MoveTowards(currentLaneOffset, targetLaneOffset, laneChangeSpeed * Time.fixedDeltaTime);



        CheckSensors();

        CheckCornering();

        MoveCar();

        SteerCar();

        AnimateWheels();

        CheckIfStuck();

    }



    [Header("Sensor Settings")]

    public float sensorHeight = 0.6f;     

    public float forwardOffset = 2.5f;    

    public float sideOffset = 0.8f;       



    // ---------------------------------------------------------

    // SENSORS LOGIC

    // ---------------------------------------------------------

    void CheckSensors()

    {

        isBraking = false;



        // Position of the sensor start (Headlight level)

        Vector3 startPoint = transform.position + (Vector3.up * sensorHeight) + (transform.forward * forwardOffset);



        // Define headlight positions

        Vector3 posRight = startPoint + (transform.right * sideOffset);

        Vector3 posLeft = startPoint - (transform.right * sideOffset);



        bool rightHit = CastSensor(posRight, transform.forward, sensorLen);

        bool leftHit = CastSensor(posLeft, transform.forward, sensorLen);



        // Logic: If ANY sensor hits, brake.

        if (rightHit || leftHit)

        {

            isBraking = true;



            // If we are not already changing lanes, try to switch

            if (laneChangeCooldown <= 0)

            {

                // If right sensor hit -> Try going Left

                if (rightHit && !leftHit) TryChangeLanes(posLeft); 

                

                // If left sensor hit -> Try going Right

                else if (leftHit && !rightHit) TryChangeLanes(posRight);

                

                // If BOTH hit -> Just pick a random side or default to Right logic

                else TryChangeLanes(posRight);

            }

        }

    }



    bool CastSensor(Vector3 start, Vector3 dir, float length)

    {

        RaycastHit hit;

        if (showDebugGizmos) Debug.DrawRay(start, dir * length, Color.green);



        if (Physics.Raycast(start, dir, out hit, length, obstacleMask))

        {

            // Ignore ourselves!

            if (hit.collider.transform.root != transform)

            {

                if (showDebugGizmos) Debug.DrawLine(start, hit.point, Color.red);

                return true;

            }

        }

        return false;

    }



    void TryChangeLanes(Vector3 origin)

    {

        float laneWidth = 4f; 



        bool isLeft = targetLaneOffset < -1f;

        bool isRight = targetLaneOffset > 1f;

        bool isMiddle = !isLeft && !isRight;



        // Try to move to a clear lane

        if (isLeft)

        {

            if (CheckLaneClear(transform.right, laneWidth)) SetLaneChange(laneWidth);

        }

        else if (isRight)

        {

            if (CheckLaneClear(-transform.right, laneWidth)) SetLaneChange(-laneWidth);

        }

        else if (isMiddle)

        {

            // Prefer passing on the Left first (standard driving rule)

            if (CheckLaneClear(-transform.right, laneWidth)) SetLaneChange(-laneWidth);

            else if (CheckLaneClear(transform.right, laneWidth)) SetLaneChange(laneWidth);

        }

    }



    void SetLaneChange(float amount)

    {

        targetLaneOffset += amount;

        laneChangeCooldown = 4f; 

    }



    // ---------------------------------------------------------

    // CRITICAL FIX: BLIND SPOT CHECK

    // ---------------------------------------------------------

    bool CheckLaneClear(Vector3 dir, float width)

    {

        // 1. Where is the lane we want to go to?

        Vector3 laneCheckPos = transform.position + (dir * width);



        // 2. BLIND SPOT: Use NonAlloc to dump hits into our existing array! Zero Garbage generated.

        int hitCount = Physics.OverlapSphereNonAlloc(laneCheckPos, 2.5f, hitColliders, obstacleMask);

        for (int i = 0; i < hitCount; i++)

        {

            if (hitColliders[i].transform.root != transform) // If it's NOT me

            {

                return false; // Lane blocked by someone else

            }

        }



        // 3. LOOK AHEAD in that lane

        Vector3 rayStart = laneCheckPos + (Vector3.up * sensorHeight) + (transform.forward * forwardOffset);

        if (Physics.Raycast(rayStart, transform.forward, 20f, obstacleMask))

        {

             return false; // Someone is ahead in that lane

        }



        return true; // Lane is safe!

    }



    void MoveCar()

    {

        float speed = rb.linearVelocity.magnitude * 3.6f;



        if (isBraking)

        {

            ApplyBrakes(brakeForce); // FULL BRAKE

            SetMotorTorque(0f);      // CUT ENGINE

        }

        else if (speed > currentSpeedLimit + 2f)

        {

            ApplyBrakes(brakeForce * 0.2f);

            SetMotorTorque(0f);

        }

        else if (speed < currentSpeedLimit - 2f)

        {

            ReleaseBrakes();

            SetMotorTorque(motorForce);

        }

        else

        {

            ReleaseBrakes();

            SetMotorTorque(0f);

        }

    }



    void CheckCornering()

    {

        if (currentPath == null) return;

        currentSpeedLimit = myTopSpeed;



        int nodeA = currentNode;

        int nodeB = (currentNode + 1) % currentPath.nodes.Count;

        int nodeC = (currentNode + 2) % currentPath.nodes.Count;



        Vector3 dir1 = (currentPath.nodes[nodeB].position - currentPath.nodes[nodeA].position).normalized;

        Vector3 dir2 = (currentPath.nodes[nodeC].position - currentPath.nodes[nodeB].position).normalized;



        if (Vector3.Angle(dir1, dir2) > 20f) currentSpeedLimit = 25f; // Slow down on turns

    }



    void CheckIfStuck()

    {

        if (initialGracePeriod > 0) return;

        if (!isBraking && rb.linearVelocity.magnitude < 0.5f)

        {

            stuckTimer += Time.fixedDeltaTime;

            if (stuckTimer > 5f) gameObject.SetActive(false); // Make sure you are using object pooling!

        }

        else stuckTimer = 0f;

    }



    void SetMotorTorque(float torque) { fl.motorTorque = torque; fr.motorTorque = torque; rl.motorTorque = torque; rr.motorTorque = torque; }

    void ApplyBrakes(float power) { fl.brakeTorque = power; fr.brakeTorque = power; rl.brakeTorque = power; rr.brakeTorque = power; }

    void ReleaseBrakes() { fl.brakeTorque = 0; fr.brakeTorque = 0; rl.brakeTorque = 0; rr.brakeTorque = 0; }



    void SteerCar()

    {

        Transform targetNode = currentPath.nodes[currentNode];

        Transform nextNode = currentPath.nodes[(currentNode + 1) % currentPath.nodes.Count];



        Vector3 roadDir = (nextNode.position - targetNode.position).normalized;

        Vector3 roadRight = Vector3.Cross(Vector3.up, roadDir).normalized;

        

        Vector3 exactTarget = targetNode.position + (roadRight * currentLaneOffset);



        Vector3 relativePos = transform.InverseTransformPoint(exactTarget);

        float steer = (relativePos.x / relativePos.magnitude) * 45f;



        fl.steerAngle = steer; fr.steerAngle = steer;



        if (Vector3.Distance(transform.position, exactTarget) < 6f)

        {

            currentNode = (currentNode + 1) % currentPath.nodes.Count;

        }

    }



    void AnimateWheels()

    {

        ApplyMesh(fl, fl_mesh); ApplyMesh(fr, fr_mesh);

        ApplyMesh(rl, rl_mesh); ApplyMesh(rr, rr_mesh);

    }



    void ApplyMesh(WheelCollider col, Transform mesh)

    {

        Vector3 p; Quaternion r;

        col.GetWorldPose(out p, out r);

        mesh.position = p; mesh.rotation = r;

    }

}