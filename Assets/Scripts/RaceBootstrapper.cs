using UnityEngine;

public class RaceBootstrapper : MonoBehaviour
{
    [Header("Track Objects")]
    public GameObject sprintTrackObjects; // Drag your Sprint Track/Spawners here
    public GameObject circuitTrackObjects; // Drag your Circuit Track/Spawners here

    void Awake()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("No GameManager found! Defaulting to Sprint.");
            SetupTrack("Sprint");
            return;
        }

        string mode = GameManager.Instance.selectedRaceMode;
        Debug.Log($"<color=magenta>BOOTSTRAPPER: Loading setup for {mode}</color>");
        SetupTrack(mode);
    }

    void SetupTrack(string mode)
    {
        // Turn off everything first
        if (sprintTrackObjects) sprintTrackObjects.SetActive(false);
        if (circuitTrackObjects) circuitTrackObjects.SetActive(false);

        // Turn on the correct one
        if (mode.Equals("Circuit", System.StringComparison.OrdinalIgnoreCase))
        {
            if (circuitTrackObjects) circuitTrackObjects.SetActive(true);
        }
        else
        {
            // Default to Sprint
            if (sprintTrackObjects) sprintTrackObjects.SetActive(true);
        }
    }
}