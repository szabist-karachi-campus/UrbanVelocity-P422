using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SmartTrafficCar : MonoBehaviour
{
    [Header("Settings")]
    public float topSpeed = 15f;     // Meters per second
    public float acceleration = 10f; // Engine power
    public float turnSpeed = 5f;     // How fast it steers
    public float reachDistance = 5f; // How close to get to waypoint before switching

    [Header("Route")]
    public TrafficPath path;         // Drag your Route_01 here
    private int currentWaypointIndex = 0;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // Gives the car mass so collisions feel heavy
        rb.mass = 1500f; 
        rb.linearDamping = 0.5f; 
        rb.angularDamping = 1f;
        // Keeps the car upright but allows physics collisions
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    void FixedUpdate()
    {
        if (path == null) return;

        Drive();
        Steer();
        CheckWaypoint();
    }

    void Drive()
    {
        // Only accelerate if under top speed
        if (rb.linearVelocity.magnitude < topSpeed)
        {
            // Apply force forward (Gas pedal)
            rb.AddRelativeForce(Vector3.forward * acceleration * 100f * Time.fixedDeltaTime);
        }
    }

    void Steer()
    {
        Transform targetWaypoint = path.waypoints[currentWaypointIndex];
        
        // Calculate direction to the target
        Vector3 directionToTarget = targetWaypoint.position - transform.position;
        directionToTarget.y = 0; // Don't look up/down, only left/right

        // Calculate the rotation required to look at the target
        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);

        // Smoothly rotate towards that target (Steering wheel)
        // 'Slerp' makes it smooth, not instant
        rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime));
    }

    void CheckWaypoint()
    {
        Transform targetWaypoint = path.waypoints[currentWaypointIndex];
        float distance = Vector3.Distance(transform.position, targetWaypoint.position);

        // If we are close enough, switch to the next point
        if (distance < reachDistance)
        {
            currentWaypointIndex++;
            // Loop back to start if we finished the path
            if (currentWaypointIndex >= path.waypoints.Count)
            {
                currentWaypointIndex = 0;
            }
        }
    }
}