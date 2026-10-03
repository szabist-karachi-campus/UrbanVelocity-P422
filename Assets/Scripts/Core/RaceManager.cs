using UnityEngine;
using UnityEngine.UI; 
using TMPro; 
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems; // <--- Required for Controller Navigation

public class RaceManager : MonoBehaviour
{
    public static RaceManager Instance;

    [Header("Gear Shift Simulation")]
    public AudioSource gearShiftSound; 
    public AudioSource downshiftSound; 
    private int previousGear = 1;
    private bool isShifting = false;
    private float shiftRpmDrop = 0f;

    [Header("Analog Speedometer")]
    public RectTransform analogNeedle; 
    public float minNeedleAngle = 135f;  
    public float maxNeedleAngle = -135f; 
    public float maxSpeedometerSpeed = 250f;

    [Header("Audio")]
    public AudioSource cameraShutterAudio;

    [Header("Spawn Settings")]
    public Transform startingLineAnchor;
    public SmoothFollowCamera raceCamera; 
    public GameObject cinematicCamera; 

    [Header("Race Settings")]
    public int totalLaps = 1; 
    public bool isCircuit = false;
    public bool raceIsActive = false; 

    [Header("UI - Dashboard")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI countdownText; 
    public TextMeshProUGUI speedText;     
    public TextMeshProUGUI gearText;      
    public TextMeshProUGUI rpmText;       
    public TextMeshProUGUI livePositionText; 
    public GameObject dashboardPanel;
    
    [Header("Checkpoints & Respawn")]
    public AudioSource checkpointSound; 
    public Transform[] checkpoints; 
    public GameObject missedCheckpointWarning; 
    private int nextExpectedCheckpoint = 0;
    private int currentLap = 1;
    private bool isRespawning = false;
    private float closestDistanceToNextCP = Mathf.Infinity; 

    [Header("Pause Menu Elements")]
    public GameObject pauseMenuPanel;
    public GameObject settingsPanel;
    public GameObject loadingScreenPanel;
    private bool isPaused = false;

    [Header("NFS Finish Screens")]
    public Image cameraFlashImage; 
    public GameObject finishScreenPanel;
    public TextMeshProUGUI titleText; 
    public TextMeshProUGUI placementText;
    public TextMeshProUGUI payoutText;
    public TextMeshProUGUI finalTimeText;
    public Vector3 finishCameraOffset = new Vector3(2.5f, 0.5f, 5.5f); 
    public Vector3 finishCameraLookAtOffset = new Vector3(0f, 0.5f, 0f);

    [Header("Leaderboard Screen")]
    public GameObject leaderboardPanel;
    public TextMeshProUGUI lbPositionsText; 
    public TextMeshProUGUI lbNamesText;     
    public TextMeshProUGUI lbTimesText;     

    // --- NEW: UI NAVIGATION TARGETS ---
    [Header("UI Navigation Targets")]
    public GameObject pauseMenuFirstButton;
    public GameObject finishScreenFirstButton;
    public GameObject leaderboardFirstButton;

    [Header("Traffic System")]
    public GameObject[] trafficPrefabs; 
    public Transform[] trafficSpawnPoints; 
    private List<GameObject> activeTraffic = new List<GameObject>();

    // Call this when clicking "Back to Main Menu"
    public void DespawnTraffic()
    {
        foreach(GameObject car in activeTraffic)
        {
            if (car != null) Destroy(car);
        }
        activeTraffic.Clear();
    }

    // Call this when the player clicks "Start Race"
    public void SpawnTraffic()
    {
        DespawnTraffic(); // Safety wipe
        
        foreach(Transform spawn in trafficSpawnPoints)
        {
            // Pick a random traffic car and spawn it
            GameObject randomCar = trafficPrefabs[Random.Range(0, trafficPrefabs.Length)];
            GameObject newCar = Instantiate(randomCar, spawn.position, spawn.rotation);
            activeTraffic.Add(newCar);
        }
    }
    
    private List<Transform> finishedAIs = new List<Transform>(); 
    private float raceTimer = 0f;
    private Rigidbody playerRb; 
    private int cachedPlayerPlace = 1; 
    private bool isViewingSnapshot = false; 

    private void Awake() 
    { 
        if (countdownText == null) Debug.LogWarning("Countdown Text is missing."); 
        Time.timeScale = 1f; 
        
        // Ensure the dashboard AND the needle are hidden when the scene first loads!
        if (dashboardPanel != null) dashboardPanel.SetActive(false);
        if (analogNeedle != null) analogNeedle.gameObject.SetActive(false);
    }
    
    private void OnEnable() { Instance = this; }

    public void ForceSpawnPlayer()
    {
        GameObject[] existingCars = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject car in existingCars) DestroyImmediate(car);

        if (startingLineAnchor == null) return;

        GameObject playerCar = Instantiate(GameManager.Instance.selectedCarPrefab, startingLineAnchor.position, startingLineAnchor.rotation);
        playerCar.tag = "Player"; 
        
        RacerStats stats = playerCar.GetComponent<RacerStats>();
        if (stats == null) stats = playerCar.AddComponent<RacerStats>();
        stats.isPlayer = true;

        foreach (Transform part in playerCar.GetComponentsInChildren<Transform>(true)) part.gameObject.tag = "Player";
        playerRb = playerCar.GetComponent<Rigidbody>();

        if (raceCamera != null)
        {
            raceCamera.target = playerCar.transform; 
            raceCamera.transform.position = playerCar.transform.position + (playerCar.transform.forward * -5f) + (Vector3.up * 2f);
            raceCamera.transform.rotation = playerCar.transform.rotation;
            raceCamera.enabled = false; 
        }

        SetupCinematicStart();
    }

    public void SetupCinematicStart() { StartCoroutine(CinematicIntroRoutine()); }

    private IEnumerator CinematicIntroRoutine()
    {
        raceIsActive = false;
        if (countdownText != null) countdownText.gameObject.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (dashboardPanel != null) dashboardPanel.SetActive(false); 
        if (analogNeedle != null) analogNeedle.gameObject.SetActive(false); 

        if (cinematicCamera != null) cinematicCamera.SetActive(true);
        if (raceCamera != null) { raceCamera.enabled = false; Camera cam = raceCamera.GetComponent<Camera>(); if (cam != null) cam.enabled = false; }

        yield return new WaitForSecondsRealtime(4f);

        if (cinematicCamera != null) cinematicCamera.SetActive(false);
        if (raceCamera != null) { raceCamera.enabled = true; Camera cam = raceCamera.GetComponent<Camera>(); if (cam != null) cam.enabled = true; }

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            countdownText.text = "3"; yield return new WaitForSecondsRealtime(1f);
            countdownText.text = "2"; yield return new WaitForSecondsRealtime(1f);
            countdownText.text = "1"; yield return new WaitForSecondsRealtime(1f);
            countdownText.text = "GO!"; 
            
            // THE FIX: Start the race AND turn on the UI + Needle!
            raceIsActive = true; 
            if (dashboardPanel != null) dashboardPanel.SetActive(true);
            if (analogNeedle != null) analogNeedle.gameObject.SetActive(true);

            yield return new WaitForSecondsRealtime(1f);
            countdownText.gameObject.SetActive(false);
        }
        else 
        { 
            raceIsActive = true; 
            if (dashboardPanel != null) dashboardPanel.SetActive(true); 
            if (analogNeedle != null) analogNeedle.gameObject.SetActive(true);
        }
    }

    private void Update()
    {
        if ((Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton7)) && !isViewingSnapshot)
        {
            TogglePauseMenu();
        }

        if (isPaused) return; 

        if ((Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.JoystickButton8)) && raceIsActive && !isRespawning)
        {
            StartCoroutine(RespawnSequence(0f)); 
        }

        if (isViewingSnapshot && raceCamera != null && playerRb != null)
        {
            Transform car = playerRb.transform;
            raceCamera.transform.position = car.position + (car.right * finishCameraOffset.x) + (car.up * finishCameraOffset.y) + (car.forward * finishCameraOffset.z);
            raceCamera.transform.LookAt(car.position + (car.right * finishCameraLookAtOffset.x) + (car.up * finishCameraLookAtOffset.y) + (car.forward * finishCameraLookAtOffset.z));
        }

        if (raceIsActive)
        {
            raceTimer += Time.deltaTime;
            UpdateDashboardUI();
            UpdateLivePosition();
            CheckForMissedCheckpointInRealtime();
        }
    }

    private void CheckForMissedCheckpointInRealtime()
    {
        if (playerRb == null || isRespawning || nextExpectedCheckpoint >= checkpoints.Length) return;

        float forwardVelocity = Vector3.Dot(playerRb.linearVelocity, playerRb.transform.forward);
        if (forwardVelocity < -1f)
        {
            closestDistanceToNextCP = Mathf.Infinity;
            return;
        }

        float currentDist = Vector3.Distance(playerRb.position, checkpoints[nextExpectedCheckpoint].position);

        if (currentDist < closestDistanceToNextCP)
        {
            closestDistanceToNextCP = currentDist; 
        }
        else if (currentDist > closestDistanceToNextCP + 20f && closestDistanceToNextCP < 30f)
        {
            StartCoroutine(RespawnSequence(4f));
            closestDistanceToNextCP = Mathf.Infinity; 
        }
    }

    private void UpdateLivePosition()
    {
        if (livePositionText == null || playerRb == null) return;
        
        GameObject[] aiCars = GameObject.FindGameObjectsWithTag("AI");
        int currentPlace = 1;

        if (nextExpectedCheckpoint < checkpoints.Length)
        {
            float myDistance = Vector3.Distance(playerRb.position, checkpoints[nextExpectedCheckpoint].position);

            foreach (GameObject ai in aiCars)
            {
                if (ai.transform != ai.transform.root) continue;

                float aiDistance = Vector3.Distance(ai.transform.position, checkpoints[nextExpectedCheckpoint].position);
                if (aiDistance < myDistance)
                {
                    currentPlace++;
                }
            }
        }

        livePositionText.text = currentPlace.ToString() + (currentPlace == 1 ? "ST" : currentPlace == 2 ? "ND" : currentPlace == 3 ? "RD" : "TH");
    }

    private void UpdateDashboardUI()
    {
        if (timerText != null)
        {
            int m = Mathf.FloorToInt(raceTimer / 60F); 
            int s = Mathf.FloorToInt(raceTimer % 60F); 
            int ms = Mathf.FloorToInt((raceTimer * 100F) % 100F);
            timerText.text = m.ToString("00") + ":" + s.ToString("00") + ":" + ms.ToString("00");
        }

        if (playerRb != null)
        {
            float rawSpeedKmH = playerRb.linearVelocity.magnitude * 3.6f;
            float clampedSpeed = Mathf.Clamp(rawSpeedKmH, 0f, 250f); 
            
            if (speedText != null) speedText.text = Mathf.RoundToInt(clampedSpeed).ToString() + " KM/H";

            float forwardVelocity = Vector3.Dot(playerRb.linearVelocity, playerRb.transform.forward);
            bool isRewindingOrReversing = forwardVelocity < -1f;

            int calculatedGear = previousGear;
            float[] gearSpeeds = { 0f, 40f, 80f, 130f, 180f, 250f }; 

            if (!isRewindingOrReversing && !isRespawning)
            {
                if (calculatedGear < 5 && clampedSpeed >= gearSpeeds[calculatedGear]) 
                {
                    calculatedGear++;
                }
                else if (calculatedGear > 1 && clampedSpeed < (gearSpeeds[calculatedGear - 1] - 5f)) 
                {
                    calculatedGear--;
                }

                if (calculatedGear > previousGear && !isShifting)
                {
                    StartCoroutine(SimulateGearShift(true)); 
                    previousGear = calculatedGear;
                }
                else if (calculatedGear < previousGear && !isShifting) 
                {
                    StartCoroutine(SimulateGearShift(false)); 
                    previousGear = calculatedGear;
                }
            }
            else
            {
                for (int i = 1; i <= 5; i++) 
                {
                    if (clampedSpeed >= gearSpeeds[i - 1]) calculatedGear = i;
                }
                previousGear = calculatedGear; 
            }

            if (gearText != null) gearText.text = "GEAR: " + calculatedGear;

            float minSpeedForGear = gearSpeeds[calculatedGear - 1];
            float maxSpeedForGear = gearSpeeds[calculatedGear];
            float gearProgress = Mathf.InverseLerp(minSpeedForGear, maxSpeedForGear, clampedSpeed);
            
            float baseRpm = Mathf.Lerp(3000f, 7000f, gearProgress);
            
            float finalRpm = baseRpm - shiftRpmDrop + Random.Range(-50f, 50f);
            if (rpmText != null) rpmText.text = "RPM: " + Mathf.Clamp(Mathf.RoundToInt(finalRpm), 1000, 8000);

            if (analogNeedle != null)
            {
                float needleProgress = clampedSpeed / 250f; 
                float currentAngle = Mathf.Lerp(minNeedleAngle, maxNeedleAngle, needleProgress);
                analogNeedle.localRotation = Quaternion.Euler(0, 0, currentAngle); 
            }
        }
    }

    private IEnumerator SimulateGearShift(bool isUpshift)
    {
        isShifting = true;
        float shiftDuration = 0.1f; 
        float timer = 0f;
        
        MonoBehaviour carScript = null;
        WheelCollider[] wheels = new WheelCollider[0];

        if (playerRb != null)
        {
            carScript = playerRb.GetComponent("CarController") as MonoBehaviour;
            wheels = playerRb.GetComponentsInChildren<WheelCollider>();
        }

        if (isUpshift)
        {
            if (gearShiftSound != null) gearShiftSound.Play();
            
            if (carScript != null) carScript.enabled = false;

            foreach (WheelCollider wc in wheels)
            {
                wc.motorTorque = 0f;
                wc.brakeTorque = 0f;
            }

            while (timer < shiftDuration)
            {
                float progress = timer / shiftDuration;
                shiftRpmDrop = Mathf.Lerp(2500f, 0f, Mathf.SmoothStep(0f, 1f, progress));
                timer += Time.deltaTime;
                yield return null;
            }

            if (carScript != null) carScript.enabled = true;
        }
        else 
        {
            if (downshiftSound != null) downshiftSound.Play();
            else if (gearShiftSound != null) gearShiftSound.Play();

            while (timer < shiftDuration)
            {
                float progress = timer / shiftDuration;
                shiftRpmDrop = Mathf.Lerp(-2000f, 0f, Mathf.SmoothStep(0f, 1f, progress)); 
                timer += Time.deltaTime;
                yield return null;
            }
        }

        shiftRpmDrop = 0f;
        isShifting = false;
    }

    public void PlayerHitCheckpoint(int index, Transform hitTransform)
    {
        int trueIndex = -1;
        for (int i = 0; i < checkpoints.Length; i++)
        {
            if (hitTransform.IsChildOf(checkpoints[i])) 
            {
                trueIndex = i;
                break;
            }
        }

        if (trueIndex == -1) trueIndex = index;

        if (trueIndex == nextExpectedCheckpoint)
        {
            if (checkpointSound != null) checkpointSound.Play(); 
            nextExpectedCheckpoint++;
            closestDistanceToNextCP = Mathf.Infinity; 
            
            if (nextExpectedCheckpoint >= checkpoints.Length)
            {
                if (isCircuit && currentLap < totalLaps) 
                { 
                    currentLap++; 
                    nextExpectedCheckpoint = 0; 
                }
                else TriggerWinCondition();
            }
        }
        else if (trueIndex > nextExpectedCheckpoint && !isRespawning)
        {
            StartCoroutine(RespawnSequence(4f)); 
        }
    }

    private IEnumerator RespawnSequence(float delay)
    {
        isRespawning = true;
        
        if (delay > 0 && missedCheckpointWarning != null) 
        {
            float timer = 0f;
            bool isTextVisible = true;
            
            while (timer < delay)
            {
                missedCheckpointWarning.SetActive(isTextVisible);
                isTextVisible = !isTextVisible; 
                yield return new WaitForSecondsRealtime(0.5f);
                timer += 0.5f;
            }
        }
        else if (delay > 0) 
        { 
            yield return new WaitForSecondsRealtime(delay); 
        }

        if (missedCheckpointWarning != null) missedCheckpointWarning.SetActive(false);

        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
            
            int spawnIndex = Mathf.Clamp(nextExpectedCheckpoint, 0, checkpoints.Length - 1);
            Transform targetSpawn = checkpoints[spawnIndex];
            
            playerRb.transform.position = targetSpawn.position - (targetSpawn.forward * 15f) + (Vector3.up * 1.5f); 
            playerRb.transform.rotation = Quaternion.LookRotation(-targetSpawn.forward);

            previousGear = 1; 
        }

        closestDistanceToNextCP = Mathf.Infinity; 
        isRespawning = false;
    }

    // --- NEW: Helper method to safely pass focus to UI buttons ---
    private void HighlightButton(GameObject target)
    {
        if (gameObject.activeInHierarchy) StartCoroutine(HighlightButtonRoutine(target));
    }

    private IEnumerator HighlightButtonRoutine(GameObject targetButton)
    {
        yield return null; 
        if (EventSystem.current != null && targetButton != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(targetButton);
        }
    }

    public void TogglePauseMenu()
    {
        isPaused = !isPaused;
        if (isPaused)
        {
            Time.timeScale = 0f;
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
            if (cameraShutterAudio != null) cameraShutterAudio.Pause();

            // FIX 1: Send focus to the Pause Menu button immediately
            HighlightButton(pauseMenuFirstButton);
        }
        else
        {
            Time.timeScale = 1f;
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (cameraShutterAudio != null) cameraShutterAudio.UnPause();
        }
    }

    public void UI_ResumeGame() { TogglePauseMenu(); }
    
    public void UI_RestartRace() { Time.timeScale = 1f; StartCoroutine(LoadScene(SceneManager.GetActiveScene().buildIndex)); }
    
    public void UI_LoadMainToGarage() { Time.timeScale = 1f; StartCoroutine(LoadScene(0)); }
    
    public void UI_OpenSettings() { if (settingsPanel != null) settingsPanel.SetActive(true); }
    
    public void UI_CloseSettings() { if (settingsPanel != null) settingsPanel.SetActive(false); }

    public void UI_QuitGame() { Application.Quit(); }

    private IEnumerator LoadScene(int index)
    {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (loadingScreenPanel != null) loadingScreenPanel.SetActive(true);
        yield return new WaitForSecondsRealtime(0.5f);
        SceneManager.LoadScene(index);
    }

    public void RegisterAIFinish(Transform aiCar) { if (!finishedAIs.Contains(aiCar)) finishedAIs.Add(aiCar); }

    private void TriggerWinCondition()
    {
        raceIsActive = false; 
        int playerPlace = finishedAIs.Count + 1; 
        int payout = playerPlace == 1 ? 6500000 : playerPlace == 2 ? 25000 : playerPlace == 3 ? 5000 : 0;
        string titleString = playerPlace == 1 ? "WINNER!" : "RACE FINISHED";
        string placementString = playerPlace + (playerPlace == 1 ? "ST" : playerPlace == 2 ? "ND" : playerPlace == 3 ? "RD" : "TH") + " PLACE";

        if (playerPlace == 1) { GameManager.Instance.currentPlayerProfile.totalMoney += payout; GameManager.Instance.currentPlayerProfile.hasCompletedSprint = true; GameManager.Instance.SaveCurrentProfile(); }

        StartCoroutine(CinematicFinishSequence(titleString, placementString, payout, playerPlace));
    }

    private IEnumerator CinematicFinishSequence(string title, string placement, int payout, int playerPlace)
    {
        // THE FIX: Hide the dashboard AND the needle instantly when the race finishes
        if (dashboardPanel != null) dashboardPanel.SetActive(false);
        if (analogNeedle != null) analogNeedle.gameObject.SetActive(false);

        Time.timeScale = 0.15f; yield return new WaitForSecondsRealtime(1.5f); Time.timeScale = 0f; 

        if (raceCamera != null) raceCamera.enabled = false; 
        isViewingSnapshot = true; 
        if (cameraShutterAudio != null) cameraShutterAudio.Play();

        if (cameraFlashImage != null) { cameraFlashImage.gameObject.SetActive(true); cameraFlashImage.color = new Color(1f, 1f, 1f, 1f); }

        if (finishScreenPanel != null)
        {
            finishScreenPanel.SetActive(true);
            if (titleText != null) titleText.text = title; if (placementText != null) placementText.text = placement;
            if (payoutText != null) payoutText.text = payout > 0 ? "REWARD: $" + payout.ToString("N0") : "NO REWARD";
            
            if (finalTimeText != null && timerText != null) finalTimeText.text = "TIME: " + timerText.text; 

            HighlightButton(finishScreenFirstButton);
        }

        float fadeSpeed = 3f;
        while (cameraFlashImage != null && cameraFlashImage.color.a > 0)
        {
            Color c = cameraFlashImage.color; c.a -= Time.unscaledDeltaTime * fadeSpeed; cameraFlashImage.color = c; yield return null;
        }

        cachedPlayerPlace = playerPlace;
    }

    public void OnNextButtonPressed() { isViewingSnapshot = false; if (finishScreenPanel != null) finishScreenPanel.SetActive(false); ShowLeaderboard(cachedPlayerPlace); }

    private void ShowLeaderboard(int playerPlace)
    {
        if (leaderboardPanel != null) leaderboardPanel.SetActive(true);

        // FIX 3: Send focus to the Leaderboard "Continue" button
        HighlightButton(leaderboardFirstButton);

        string positionsStr = "";
        string namesStr = "";
        string timesStr = "";

        int aiIndex = 1;
        
        // --- NEW: DYNAMIC NAME LOOKUP ---
        string pName = "PLAYER";
        if (GameManager.Instance != null && GameManager.Instance.currentPlayerProfile != null)
        {
            // Note: If your profile name variable is called something else (like 'playerName'), just change it here!
            pName = GameManager.Instance.currentPlayerProfile.saveProfileName;
        }

        for (int i = 1; i <= 4; i++)
        {
            positionsStr += i.ToString() + "\n";
            
            if (i == playerPlace)
            {
                namesStr += "<color=#FACC15>" + pName.ToUpper() + " (YOU)</color>\n";
                timesStr += (timerText != null ? timerText.text : "--:--:--") + "\n";
            }
            else
            {
                namesStr += "RIVAL " + aiIndex + "\n";
                timesStr += "FINISHED\n"; 
                aiIndex++;
            }
        }

        if (lbPositionsText != null) lbPositionsText.text = positionsStr;
        if (lbNamesText != null) lbNamesText.text = namesStr;
        if (lbTimesText != null) lbTimesText.text = timesStr;
    }
}