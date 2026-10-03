using UnityEngine;
using System.Collections.Generic;

public class WaypointTraffic : MonoBehaviour
{
    [Header("References")]
    public CarController carController;
    
    // We will assign this automatically when Spawning
    [HideInInspector] public Transform pathContainer; 
    
    [Header("Driving Settings")]
    public float reachDistance = 10.0f; 
    public float laneOffset = 0f; // 0=Center, -3=Left, 3=Right
    public float steerSensitivity = 1.0f;

    // Internal
    private List<Transform> nodes = new List<Transform>();
    private int currentNode = 0;

    void Start()
    {
        carController = GetComponent<CarController>();
        
        // Safety check: if spawned manually without a path
        if(pathContainer == null) 
        {
            GameObject pathObj = GameObject.Find("RoadPath");
            if(pathObj) InitializePath(pathObj.transform, 0);
        }
    }

    // The Spawner calls this to setup the car
    public void InitializePath(Transform path, int startingNodeIndex)
    {
        pathContainer = path;
        currentNode = startingNodeIndex;

        // Gather all nodes
        foreach (Transform t in pathContainer) 
            if (t != pathContainer) nodes.Add(t);
    }

    void FixedUpdate()
    {
        if (nodes.Count == 0) return;
        Drive();
    }

    void Drive()
    {
        // 1. Get the position of the current target node
        Vector3 nodePos = nodes[currentNode].position;

        // 2. Apply Lane Offset (This is the magic part!)
        // We calculate "Right" relative to the road's direction
        Vector3 roadDirection = nodes[currentNode].forward;
        Vector3 rightVector = Vector3.Cross(Vector3.up, roadDirection).normalized; // Proper Right
        
        // The actual target is the Node + Lane Offset
        Vector3 targetPos = nodePos + (nodes[currentNode].right * laneOffset);

        // 3. Steering Logic
        Vector3 steerVector = transform.InverseTransformPoint(targetPos);
        float newSteer = (steerVector.x / steerVector.magnitude) * steerSensitivity;
        
        // 4. Check if we reached the node
        if (Vector3.Distance(transform.position, targetPos) < reachDistance)
        {
            currentNode++;
            if (currentNode >= nodes.Count) currentNode = 0; // Loop
        }

        // 5. Send to Car Controller
        // (Add your sensor/braking logic here if you want obstacle avoidance)
        carController.SetInput(1f, newSteer, false);
    }
}