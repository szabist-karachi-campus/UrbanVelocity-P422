using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class CarSpawnData {
    public GameObject carPrefab;
    public Transform spawnPoint;
    
    [Tooltip("Put -1 for Left, 0 for Center, or 1 for Right")]
    public int startingLane = 0; 
    
    // WE PUT THIS BACK SO THEY CAN ACTUALLY DRIVE FAST
    public float topSpeed = 100f; 
}

public class RaceSpawner : MonoBehaviour {
    [Header("Track Reference")]
    public WaypointSystem trackSystem; 
    
    [Header("Opponents")]
    public List<CarSpawnData> opponents;

    private static bool hasSpawned = false; 

    void Start() {
        if (hasSpawned) return;
        
        if (trackSystem == null) {
            Debug.LogError("FATAL: Assign the WaypointSystem to the Spawner!");
            return;
        }

        hasSpawned = true; 

        // Get difficulty (Defaults to 1/Medium if testing directly in scene)
        int diffLevel = 1; 
        if (GameManager.Instance != null && GameManager.Instance.currentPlayerProfile != null) {
            diffLevel = GameManager.Instance.currentPlayerProfile.difficulty;
        }

        foreach (CarSpawnData data in opponents) {
            if (data.carPrefab != null && data.spawnPoint != null) {
                GameObject car = Instantiate(data.carPrefab, data.spawnPoint.position, data.spawnPoint.rotation);
                AICarController ai = car.GetComponent<AICarController>();
                
                if (ai != null) {
                    // WE PASS BOTH NOW: The speed (100) AND the difficulty (0, 1, or 2)
                    ai.PrepareAI(trackSystem, data.startingLane, data.topSpeed, diffLevel);
                }
            }
        }
    }

    void OnDestroy() {
        hasSpawned = false;
    }
}