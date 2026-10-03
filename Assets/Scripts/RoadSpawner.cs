using UnityEngine;

using System.Collections;

using System.Collections.Generic;



public class RoadSpawner : MonoBehaviour

{

    public GameObject[] trafficPrefabs;

    public Transform player;

    public Transform pathContainer;



    [Header("Settings")]

    public int spawnNodeDistance = 10;

    public float spawnInterval = 2.0f;

    public float[] laneOffsets = { -4f, 0f, 4f };



    private List<Transform> nodes = new List<Transform>();



    void Start()

    {

        Debug.Log("--- SPAWNER STARTED ---");



        // 1. Check References

        if (player == null) Debug.LogError("❌ ERROR: Player is NOT assigned in RoadSpawner!");

        if (pathContainer == null) Debug.LogError("❌ ERROR: Path Container is NOT assigned!");

        if (trafficPrefabs == null || trafficPrefabs.Length == 0) Debug.LogError("❌ ERROR: Traffic Prefabs list is EMPTY!");



        // 2. Load Nodes

        if (pathContainer != null)

        {

            foreach (Transform t in pathContainer)

            {

                if (t != pathContainer) nodes.Add(t);

            }

            Debug.Log("✅ Nodes Found: " + nodes.Count);

        }



        if (nodes.Count == 0) Debug.LogError("❌ ERROR: RoadPath has no children (Nodes)!");



        // 3. Start Loop

        StartCoroutine(SpawnLoop());

    }



    IEnumerator SpawnLoop()

    {

        Debug.Log("✅ Spawn Loop Started");

        while (true)

        {

            SpawnCar();

            yield return new WaitForSeconds(spawnInterval);

        }

    }



    void SpawnCar()

    {

        // Safety Checks with Logs

        if (player == null) return;

        if (nodes.Count == 0) return;

        if (trafficPrefabs == null || trafficPrefabs.Length == 0) return;



        Debug.Log("🚗 Attempting to spawn car...");



        // Logic

        int closestNodeIndex = GetClosestNode(player.position);

        int spawnIndex = (closestNodeIndex + spawnNodeDistance) % nodes.Count;

        Transform spawnNode = nodes[spawnIndex];



        float selectedLane = laneOffsets[Random.Range(0, laneOffsets.Length)];

        Vector3 spawnPos = spawnNode.position;

        spawnPos += spawnNode.right * selectedLane;

        spawnPos.y += 0.5f;



        // ACTUAL SPAWN

        GameObject newCar = Instantiate(trafficPrefabs[Random.Range(0, trafficPrefabs.Length)], spawnPos, spawnNode.rotation);

       

        Debug.Log("✨ SUCCESS: Spawned " + newCar.name + " at Node " + spawnIndex);



        // Setup AI

        WaypointTraffic ai = newCar.GetComponent<WaypointTraffic>();

        if (ai)

        {

            ai.InitializePath(pathContainer, spawnIndex);

            ai.laneOffset = selectedLane;

        }

        else

        {

            Debug.LogWarning("⚠️ WARNING: Spawned Car does not have 'WaypointTraffic' script!");

        }

    }



    int GetClosestNode(Vector3 pos)

    {

        int closest = 0;

        float minDist = Mathf.Infinity;

        for (int i = 0; i < nodes.Count; i++)

        {

            float d = Vector3.Distance(pos, nodes[i].position);

            if (d < minDist) { minDist = d; closest = i; }

        }

        return closest;

    }

}