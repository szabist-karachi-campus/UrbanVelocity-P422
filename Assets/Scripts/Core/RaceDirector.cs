using UnityEngine;

public class RaceDirector : MonoBehaviour
{
    [Header("Mode Layouts (Checkpoints & Triggers)")]
    public GameObject sprintLayout;
    public GameObject circuitLayout;

    [Header("The Managers (Drag from Hierarchy)")]
    public RaceManager sprintManager;
    public RaceManager circuitManager;

    [Header("Cinematic Cameras")]
    public GameObject sprintCinematicCam;
    public GameObject circuitCinematicCam;

    [Header("Track Waypoints (From Environment)")]
    public WaypointSystem ongoingWaypoints; 
    public WaypointSystem incomingWaypoints; 

    [Header("Sprint Spawns (Ongoing Track)")]
    public Transform sprintPlayerSpawn;
    public Transform[] sprintAISpawns;

    [Header("Circuit Spawns (Incoming Track)")]
    public Transform circuitPlayerSpawn;
    public Transform[] circuitAISpawns;

    [Header("Opponent Cars")]
    public GameObject[] aiCarPrefabs;

    private void Start()
    {
        // 1. Instantly turn off EVERYTHING so there is no confusion
        if (sprintLayout != null) sprintLayout.SetActive(false);
        if (circuitLayout != null) circuitLayout.SetActive(false);
        if (sprintManager != null) sprintManager.gameObject.SetActive(false);
        if (circuitManager != null) circuitManager.gameObject.SetActive(false);

        // 2. Read GameManager Data
        string chosenMode = GameManager.Instance.selectedRaceMode;
        GameObject playerPrefab = GameManager.Instance.selectedCarPrefab;
        int difficulty = GameManager.Instance.currentPlayerProfile.difficulty;        

        if (playerPrefab == null) return;
        if (string.IsNullOrEmpty(chosenMode)) chosenMode = "Sprint";

        // 3. Explicitly crown the correct manager and start the race
        if (chosenMode == "Sprint")
        {
            if (sprintLayout != null) sprintLayout.SetActive(true);
            
            // WAKE UP SPRINT MANAGER
            sprintManager.gameObject.SetActive(true);
            RaceManager.Instance = sprintManager;
            
            RaceManager.Instance.cinematicCamera = sprintCinematicCam;
            SpawnRace(playerPrefab, sprintPlayerSpawn, sprintAISpawns, ongoingWaypoints, difficulty);
        }
        else if (chosenMode == "Circuit")
        {
            if (circuitLayout != null) circuitLayout.SetActive(true);
            
            // WAKE UP CIRCUIT MANAGER
            circuitManager.gameObject.SetActive(true);
            RaceManager.Instance = circuitManager;

            RaceManager.Instance.cinematicCamera = circuitCinematicCam;
            SpawnRace(playerPrefab, circuitPlayerSpawn, circuitAISpawns, incomingWaypoints, difficulty);
        }
    }

    private void SpawnRace(GameObject playerPrefab, Transform playerSpawn, Transform[] aiSpawns, WaypointSystem trackWaypoints, int difficultyLevel)
    {
        // Tell the crowned Manager exactly where to spawn the player
        if (RaceManager.Instance != null)
        {
            RaceManager.Instance.startingLineAnchor = playerSpawn;
            RaceManager.Instance.ForceSpawnPlayer();
        }

        // Spawn AI
        for (int i = 0; i < aiSpawns.Length; i++)
        {
            if (i >= aiCarPrefabs.Length || aiCarPrefabs[i] == null) break; 
            if (aiSpawns[i] == null) continue;

            GameObject spawnedAI = Instantiate(aiCarPrefabs[i], aiSpawns[i].position, aiSpawns[i].rotation);
            
            spawnedAI.tag = "AI"; 
            foreach (Transform childCollider in spawnedAI.GetComponentsInChildren<Transform>(true))
            {
                childCollider.gameObject.tag = "AI"; 
            }

            RacerStats stats = spawnedAI.GetComponent<RacerStats>();
            if (stats == null) stats = spawnedAI.AddComponent<RacerStats>();
            stats.racerName = "Rival " + (i + 1);
            stats.isPlayer = false;

            AICarController aiController = spawnedAI.GetComponent<AICarController>();
            if (aiController != null)
            {
                int startLane = (i % 2 == 0) ? -1 : 1; 
                aiController.PrepareAI(trackWaypoints, startLane, 140f, difficultyLevel);
                
                GameObject activePlayer = GameObject.FindGameObjectWithTag("Player");
                if (activePlayer != null) aiController.playerTarget = activePlayer.transform;
            }
        }
    }
}