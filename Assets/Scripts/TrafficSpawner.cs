using UnityEngine;
using System.Collections.Generic;

public class TrafficSpawner : MonoBehaviour
{
    [Header("Track Integration")]
    [Tooltip("Type 'Sprint' or 'Circuit' - matches what GameManager expects")]
    public string requiredRaceMode = "Sprint";
    public WaypointSystem trackWaypoints;

    [Header("Traffic Settings")]
    public GameObject[] trafficCarPrefabs;
    public int poolSize = 15; // Total number of cars on this track
    public float laneWidth = 4f; 
    
    [Header("Speed Settings")]
    public float minTopSpeed = 35f;
    public float maxTopSpeed = 55f;
    public float spawnHeight = 1.5f;

    private List<TrafficCar> trafficPool = new List<TrafficCar>();

    void Start()
    {
        // 1. The Bouncer: Instantly kill this spawner if it's the wrong track!
        if (GameManager.Instance != null && GameManager.Instance.selectedRaceMode != requiredRaceMode)
        {
            Debug.Log($"<color=yellow>Disabled {gameObject.name}. Player chose {GameManager.Instance.selectedRaceMode}</color>");
            gameObject.SetActive(false);
            return;
        }

        // 2. Failsafe Check
        if (trackWaypoints == null || trackWaypoints.nodes.Count == 0)
        {
            Debug.LogError($"<color=red>TrafficSpawner on {gameObject.name} has no Waypoints assigned!</color>");
            return;
        }

        if (trafficCarPrefabs.Length == 0)
        {
            Debug.LogError($"<color=red>TrafficSpawner needs at least one Car Prefab!</color>");
            return;
        }

        BuildTrafficPool();
        DeployTrafficToTrack();
    }

    private void BuildTrafficPool()
    {
        // Create all the cars securely inside this manager's folder to keep the Hierarchy clean
        for (int i = 0; i < poolSize; i++)
        {
            GameObject prefab = trafficCarPrefabs[Random.Range(0, trafficCarPrefabs.Length)];
            GameObject carObj = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            
            carObj.transform.SetParent(this.transform);
            carObj.SetActive(false); // Keep them hidden until deployment

            TrafficCar tc = carObj.GetComponent<TrafficCar>();
            if (tc != null) trafficPool.Add(tc);
        }
    }

    private void DeployTrafficToTrack()
    {
        // Calculate the spacing so cars don't spawn on top of each other
        int spacingStep = trackWaypoints.nodes.Count / poolSize; 
        if (spacingStep < 1) spacingStep = 1;

        float[] lanes = { -laneWidth, 0f, laneWidth }; // Left, Center, Right

        for (int i = 0; i < trafficPool.Count; i++)
        {
            int nodeIndex = (i * spacingStep) % trackWaypoints.nodes.Count;
            Transform spawnNode = trackWaypoints.nodes[nodeIndex];
            Transform nextNode = trackWaypoints.nodes[(nodeIndex + 1) % trackWaypoints.nodes.Count];

            Vector3 dir = (nextNode.position - spawnNode.position).normalized;
            Vector3 rightDir = Vector3.Cross(Vector3.up, dir).normalized;

            // Pick a random lane
            float chosenLaneOffset = lanes[Random.Range(0, lanes.Length)];

            // Calculate exact starting position
            Vector3 spawnPos = spawnNode.position + (rightDir * chosenLaneOffset) + (Vector3.up * spawnHeight);
            
            TrafficCar car = trafficPool[i];
            car.transform.position = spawnPos;
            
            if (dir != Vector3.zero) 
            {
                car.transform.rotation = Quaternion.LookRotation(dir);
            }
            
            car.gameObject.SetActive(true);
            
            // Give the car its brain instructions using your flawless Initialize function
            float randomSpeed = Random.Range(minTopSpeed, maxTopSpeed);
            car.Initialize(trackWaypoints, nodeIndex, chosenLaneOffset, randomSpeed);
        }

        Debug.Log($"<color=cyan>SUCCESS: Deployed {trafficPool.Count} Traffic Cars on {requiredRaceMode} track!</color>");
    }
}