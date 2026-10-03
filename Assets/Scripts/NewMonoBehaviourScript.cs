using UnityEngine;

public class TrafficCarAI : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 15f;
    public float rotationSpeed = 5f;
    public float accuracy = 2.0f; // How close to get before switching to next node

    [Header("Lane Settings")]
    public float laneOffset = 0f; // 0 = center, -3 = left lane, 3 = right lane

    private WaypointSystem currentPath;
    private int currentNodeIndex = 0;

    public void Initialize(WaypointSystem path, int startingIndex, float offset)
    {
        currentPath = path;
        currentNodeIndex = startingIndex;
        laneOffset = offset;
    }

    void Update()
    {
        if (currentPath == null) return;

        Drive();
    }

    void Drive()
    {
        // 1. Get the target position (The node + the lane offset)
        Transform targetNode = currentPath.nodes[currentNodeIndex];
        
        // Calculate the position specifically for THIS lane (Right vector * offset)
        Vector3 targetPosition = targetNode.position + (targetNode.right * laneOffset);

        // 2. Rotate towards the target (Handles Curves)
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0; // Keep car flat

        if (direction != Vector3.zero)
        {
            Quaternion lookRot = Quaternion.LookRotation(direction);
            // Smoothly rotate (Slerp) so it handles curves naturally
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, rotationSpeed * Time.deltaTime);
        }

        // 3. Move Forward
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        // 4. Check if we reached the waypoint
        if (Vector3.Distance(transform.position, targetPosition) < accuracy)
        {
            currentNodeIndex++;
            // If we reach the end, reset to 0 (Loop) or destroy
            if (currentNodeIndex >= currentPath.nodes.Count)
            {
                currentNodeIndex = 0; 
            }
        }
    }
}