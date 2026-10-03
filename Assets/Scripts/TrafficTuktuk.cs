using UnityEngine;

public class TrafficTuktuk : MonoBehaviour
{
    [Header("Car Power")]
    public float motorForce = 1500f; 
    public float brakeForce = 3000f; 
    private float myTopSpeed; 

    [Header("Sensors")]
    public float sensorLen = 3.5f; // Fixed length based on previous advice
    public LayerMask obstacleMask; 
    public bool showDebugGizmos = true;

    [Header("3-Wheel Setup")]
    public WheelCollider frontWheel;
    public WheelCollider rearLeft, rearRight;

    [Header("Meshes")]
    public Transform frontMesh;
    // CHANGED: We now only have ONE slot for the rear wheels
    public Transform rearAxleMesh; 

    private WaypointSystem currentPath;
    private int currentNode;
    private float laneOffset;
    private Rigidbody rb;
    private bool isBraking = false;
    private bool isCornering = false;
    
    // Safety timer
    private float stuckTimer = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0, -0.9f, 0); 
    }

    public void Initialize(WaypointSystem path, int node, float offset, float randomSpeed)
    {
        currentPath = path;
        currentNode = node;
        laneOffset = offset;
        myTopSpeed = randomSpeed;
    }

    void FixedUpdate()
{
    if (currentPath == null) return;

    // CheckSensors();  <-- ADD TWO SLASHES HERE TO DISABLE IT
    CheckCornering(); 
    MoveCar();
    SteerCar();
    AnimateWheels();
    CheckIfStuck();
}

    // --- LOGIC SECTION (Same as before) ---
    void CheckSensors() {
        isBraking = false;
        Vector3 sensorStart = transform.position + (Vector3.up * 1.0f) + (transform.forward * 2.0f);
        RaycastHit hit;
        if (Physics.Raycast(sensorStart, transform.forward, out hit, sensorLen, obstacleMask)) {
            if (hit.collider.transform.root != transform) isBraking = true;
        }
        // Side sensors...
        if (Physics.Raycast(sensorStart, (transform.forward - transform.right * 0.4f).normalized, out hit, sensorLen * 0.6f, obstacleMask)) {
            if (hit.collider.transform.root != transform) isBraking = true;
        }
        if (Physics.Raycast(sensorStart, (transform.forward + transform.right * 0.4f).normalized, out hit, sensorLen * 0.6f, obstacleMask)) {
            if (hit.collider.transform.root != transform) isBraking = true;
        }
    }

    void CheckCornering() {
        isCornering = false;
        if(currentPath == null || currentPath.nodes.Count == 0) return;
        Transform targetNode = currentPath.nodes[currentNode];
        float angle = Vector3.Angle(transform.forward, (targetNode.position - transform.position).normalized);
        if (angle > 10f) isCornering = true;
    }

    void MoveCar() {
        float speed = rb.linearVelocity.magnitude * 3.6f; 
        if (isBraking) {
            ApplyBrakes(brakeForce);
            rearLeft.motorTorque = 0; rearRight.motorTorque = 0;
        } else if (isCornering) {
            rearLeft.motorTorque = 0; rearRight.motorTorque = 0;
            ReleaseBrakes();
        } else {
            ReleaseBrakes();
            if (speed < myTopSpeed) {
                rearLeft.motorTorque = motorForce; 
                rearRight.motorTorque = motorForce;
            } else {
                rearLeft.motorTorque = 0; rearRight.motorTorque = 0;
            }
        }
    }

    void ApplyBrakes(float power) {
        frontWheel.brakeTorque = power;
        rearLeft.brakeTorque = power; rearRight.brakeTorque = power;
    }
    void ReleaseBrakes() {
        frontWheel.brakeTorque = 0;
        rearLeft.brakeTorque = 0; rearRight.brakeTorque = 0;
    }
    
    void CheckIfStuck() {
        if (!isBraking && rb.linearVelocity.magnitude < 1f) {
            stuckTimer += Time.fixedDeltaTime;
            if (stuckTimer > 4f) Destroy(gameObject); 
        } else { stuckTimer = 0f; }
    }

    void SteerCar() {
        Transform targetNode = currentPath.nodes[currentNode];
        Transform nextNode = currentPath.nodes[(currentNode + 1) % currentPath.nodes.Count];
        Vector3 roadDir = (nextNode.position - targetNode.position).normalized;
        Vector3 roadRight = Vector3.Cross(Vector3.up, roadDir).normalized;
        Vector3 exactTarget = targetNode.position + (roadRight * laneOffset);
        Vector3 relativePos = transform.InverseTransformPoint(exactTarget);
        float steer = (relativePos.x / relativePos.magnitude) * 45f; 
        frontWheel.steerAngle = steer;
        if (Vector3.Distance(transform.position, exactTarget) < 6f) {
            currentNode = (currentNode + 1) % currentPath.nodes.Count;
        }
    }

    // --- ANIMATION SECTION (UPDATED) ---

    void AnimateWheels()
    {
        // 1. Animate Front Wheel normally
        ApplyMesh(frontWheel, frontMesh);

        // 2. Animate Rear Axle (The combined mesh)
        ApplyAxleMesh(rearLeft, rearRight, rearAxleMesh);
    }

    void ApplyMesh(WheelCollider col, Transform mesh)
    {
        Vector3 p; Quaternion r;
        col.GetWorldPose(out p, out r);
        mesh.position = p; 
        mesh.rotation = r;
    }

    // NEW FUNCTION for shared rear tires
    void ApplyAxleMesh(WheelCollider leftCol, WheelCollider rightCol, Transform axleMesh)
    {
        if(axleMesh == null) return;

        // Get positions of both physics wheels
        Vector3 pL, pR;
        Quaternion rL, rR;
        
        leftCol.GetWorldPose(out pL, out rL);
        rightCol.GetWorldPose(out pR, out rR);

        // POSITION: Put the mesh exactly in the middle of the two physics wheels
        // This makes it bounce up and down correctly if the car tilts
        axleMesh.position = (pL + pR) / 2f;

        // ROTATION: Just copy the rotation of the Left wheel. 
        // Since it's a solid axle, left and right spin at roughly the same speed.
        axleMesh.rotation = rL;
    }
}