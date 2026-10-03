using UnityEngine;

public class TrafficMover : MonoBehaviour
{
    [Header("Speed Settings")]
    public float minSpeed = 10f;
    public float maxSpeed = 20f;
    private float currentSpeed; // calculated on spawn

    [Header("Movement Settings")]
    public float rotationSpeed = 10f; // Increased for better cornering
    public float accuracy = 1.0f; // Lower = follows path more strictly

    private float laneOffset = 0f;
    private WaypointSystem currentPath;
    private int currentNodeIndex = 0;

    public void Initialize(WaypointSystem path, int startingIndex, float offset)
    {
        currentPath = path;
        currentNodeIndex = startingIndex;
        laneOffset = offset;
        
        // RANDOM SPEED: Pick a random speed for this specific car
        currentSpeed = Random.Range(minSpeed, maxSpeed);
    }

    void Update()
    {
        if (currentPath == null) return;

        Drive();
    }

    void Drive()
    {
        // Safety check
        if (currentNodeIndex >= currentPath.nodes.Count) currentNodeIndex = 0;
        
        // 1. Identify where we are going (Target Node)
        Transform currentWaypoint = currentPath.nodes[currentNodeIndex];
        Transform nextWaypoint = currentPath.nodes[(currentNodeIndex + 1) % currentPath.nodes.Count];

        // 2. Calculate the Lane Position
        // We calculate the direction of the road segment
        Vector3 roadDir = (nextWaypoint.position - currentWaypoint.position).normalized;
        // We find the mathematical "Right" side of that road segment
        Vector3 roadRight = Vector3.Cross(Vector3.up, roadDir).normalized;

        // The target is the Current Waypoint + Lane Offset
        // We target the *next* point's lane position to drive towards it
        Vector3 destination = currentWaypoint.position + (roadRight * laneOffset);

        // 3. Move and Rotate
        Vector3 directionToTarget = destination - transform.position;
        directionToTarget.y = 0; // Ignore height differences

        // Rotate
        if (directionToTarget != Vector3.zero)
        {
            Quaternion lookRot = Quaternion.LookRotation(directionToTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, rotationSpeed * Time.deltaTime);
        }

        // Move
        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);

        // 4. Check if we reached the point
        // We ignore Y axis for distance check to prevent bugs on slopes
        float distance = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), 
                                          new Vector3(destination.x, 0, destination.z));

        if (distance < accuracy)
        {
            currentNodeIndex++;
        }
    }
}