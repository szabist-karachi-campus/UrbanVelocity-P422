using UnityEngine;

public class PhysicsTrafficCar : MonoBehaviour
{
    [Header("Route Settings")]
    public TrafficRoute currentRoute;
    public float waypointThreshold = 5f; // How close to get before switching to next node

    [Header("Car Settings")]
    public float maxMotorTorque = 400f;
    public float maxSteeringAngle = 35f;
    public float brakeTorque = 2000f;
    public float topSpeed = 60f; // km/h
    public Vector3 centerOfMassOffset = new Vector3(0, -0.5f, 0);

    [Header("Wheel Colliders")]
    public WheelCollider flCollider;
    public WheelCollider frCollider;
    public WheelCollider rlCollider;
    public WheelCollider rrCollider;

    [Header("Visual Wheels")]
    public Transform flMesh;
    public Transform frMesh;
    public Transform rlMesh;
    public Transform rrMesh;

    private int currentWaypointIndex = 0;
    private Rigidbody rb;
    private bool isBraking = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = centerOfMassOffset; // CRITICAL: Prevents flipping
    }

    void FixedUpdate()
    {
        if (currentRoute == null) return;

        Drive();
        Steer();
        CheckWaypointDistance();
        ApplyWheelVisuals();
    }

    void Drive()
    {
        float currentSpeed = rb.linearVelocity.magnitude * 3.6f; // Convert m/s to km/h

        // Simple Speed Limiter
        if (currentSpeed < topSpeed && !isBraking)
        {
            rlCollider.motorTorque = maxMotorTorque;
            rrCollider.motorTorque = maxMotorTorque;
            flCollider.motorTorque = maxMotorTorque; // AWD for stability
            frCollider.motorTorque = maxMotorTorque;
        }
        else
        {
            rlCollider.motorTorque = 0;
            rrCollider.motorTorque = 0;
            flCollider.motorTorque = 0;
            frCollider.motorTorque = 0;
        }
    }

    void Steer()
    {
        Transform targetNode = currentRoute.GetWaypoint(currentWaypointIndex);
        
        // Calculate the vector to the target in the car's local space
        Vector3 relativeVector = transform.InverseTransformPoint(targetNode.position);
        
        // Calculate steering angle based on horizontal offset (x)
        // If target is to the right, x is positive. If left, x is negative.
        float newSteer = (relativeVector.x / relativeVector.magnitude) * maxSteeringAngle;
        
        flCollider.steerAngle = newSteer;
        frCollider.steerAngle = newSteer;
    }

    void CheckWaypointDistance()
    {
        Transform targetNode = currentRoute.GetWaypoint(currentWaypointIndex);
        float distance = Vector3.Distance(transform.position, targetNode.position);

        if (distance < waypointThreshold)
        {
            currentWaypointIndex++;
        }
    }

    void ApplyWheelVisuals()
    {
        UpdateWheel(flCollider, flMesh);
        UpdateWheel(frCollider, frMesh);
        UpdateWheel(rlCollider, rlMesh);
        UpdateWheel(rrCollider, rrMesh);
    }

    void UpdateWheel(WheelCollider col, Transform mesh)
    {
        Vector3 pos;
        Quaternion rot;
        col.GetWorldPose(out pos, out rot);
        mesh.position = pos;
        mesh.rotation = rot;
    }
}