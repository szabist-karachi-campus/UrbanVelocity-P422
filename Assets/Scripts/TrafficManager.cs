using UnityEngine;
using System.Collections.Generic;

public class TrafficManager : MonoBehaviour
{
    [Header("Car Library")]
    public GameObject[] carPrefabs;

    [Header("References")]
    public WaypointSystem track;
    public Transform player;

    [Header("Traffic Density")]
    public int totalCars = 15;      
    public int spawnStartOffset = 2;
    public int minGap = 1;          
    public int maxGap = 3;          

    [Header("Speed Settings")]
    public float minCarSpeed = 30f;
    public float maxCarSpeed = 70f;

    [Header("Safety")]
    public LayerMask trafficLayer;
    public float safetyRadius = 6f;

    private List<GameObject> activeCars = new List<GameObject>();
    private bool hasInitialized = false; 

    void Start()
    {
        // 1. If you manually dragged the player into the Inspector, spawn right now!
        if (player != null && track != null && carPrefabs.Length > 0)
        {
            Debug.Log("<color=green>TRAFFIC MANAGER: Manual player found! Spawning traffic.</color>");
            SpawnInitialTraffic();
            hasInitialized = true;
        }
    }

    void Update()
    {
        // 2. Wait until the player exists, then spawn the traffic exactly once.
        if (!hasInitialized)
        {
            if (player == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) player = p.transform;
            }

            if (player != null && track != null && carPrefabs.Length > 0)
            {
                Debug.Log("<color=green>TRAFFIC MANAGER: Dynamic player found! Spawning traffic.</color>");
                SpawnInitialTraffic();
                hasInitialized = true;
            }
        }
        
        // Removed the Update recycling loop entirely. 
        // The cars will now simply drive the track organically without ever teleporting.
    }

    void SpawnInitialTraffic()
    {
        if (track == null || player == null) return;

        int playerNode = GetClosestNode(player.position);
        int currentNode = (playerNode + 2) % track.nodes.Count;
        int carsSpawned = 0;
        int safetyLoop = 0;

        while (carsSpawned < totalCars && safetyLoop < 500)
        {
            safetyLoop++;
            if (SpawnCarAtNode(currentNode)) carsSpawned++;
            currentNode = (currentNode + 1) % track.nodes.Count;
        }
    }

    private List<int> carShuffleBag = new List<int>();
    
    bool SpawnCarAtNode(int nodeIndex)
    {
        int laneID = Random.Range(-1, 2);
        float laneOffset = laneID * track.laneWidth;
        Vector3 spawnPos = GetRoadPos(nodeIndex, laneOffset);
        
        if (!IsPositionClear(spawnPos)) return false;

        Vector3 roadDir = GetRoadDir(nodeIndex);
        Quaternion spawnRot = Quaternion.LookRotation(roadDir);

        if (carShuffleBag.Count == 0)
        {
            for (int i = 0; i < carPrefabs.Length; i++) carShuffleBag.Add(i);
        }

        int bagIndex = Random.Range(0, carShuffleBag.Count);
        int prefabIndex = carShuffleBag[bagIndex];
        carShuffleBag.RemoveAt(bagIndex);

        GameObject newCar = Instantiate(carPrefabs[prefabIndex], spawnPos, spawnRot);
        TrafficCar carScript = newCar.GetComponent<TrafficCar>();
        
        if(carScript != null)
        {
            float randomSpeed = Random.Range(minCarSpeed, maxCarSpeed);
            carScript.Initialize(track, nodeIndex, laneOffset, randomSpeed);
        }

        SetLayerRecursively(newCar, (int)Mathf.Log(trafficLayer.value, 2));
        activeCars.Add(newCar);
        return true;
    }

    bool IsPositionClear(Vector3 targetPos)
    {
        foreach(GameObject car in activeCars)
        {
            if (car != null && Vector3.Distance(targetPos, car.transform.position) < 12f) return false;
        }
        return true;
    }

    int GetClosestNode(Vector3 pos)
    {
        int best = 0; float closest = Mathf.Infinity;
        for (int i = 0; i < track.nodes.Count; i++)
        {
            float d = Vector3.Distance(pos, track.nodes[i].position);
            if (d < closest) { closest = d; best = i; }
        }
        return best;
    }

    Vector3 GetRoadPos(int index, float offset)
    {
        Transform node = track.nodes[index];
        Transform next = track.nodes[(index + 1) % track.nodes.Count];
        Vector3 dir = (next.position - node.position).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
        Vector3 raw = node.position + (right * offset);
        
        RaycastHit hit;
        if(Physics.Raycast(raw + Vector3.up * 20f, Vector3.down, out hit, 50f))
        {
            return hit.point + Vector3.up * 0.5f;
        }
        return raw + Vector3.up * 0.5f;
    }

    Vector3 GetRoadDir(int index)
    {
        Transform node = track.nodes[index];
        Transform next = track.nodes[(index + 1) % track.nodes.Count];
        return (next.position - node.position).normalized;
    }

    void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform) SetLayerRecursively(child.gameObject, newLayer);
    }
}