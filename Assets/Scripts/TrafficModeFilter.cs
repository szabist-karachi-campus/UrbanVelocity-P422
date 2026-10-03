using UnityEngine;
using System;

public class TrafficModeFilter : MonoBehaviour
{
    [Tooltip("Type 'Sprint' or 'Circuit'")]
    public string requiredRaceMode;

    private void Start()
    {
        // Add a tiny delay to ensure GameManager is fully awake
        Invoke("CheckMode", 0.1f);
    }

    private void CheckMode()
    {
        if (GameManager.Instance == null)
        {
            Debug.Log($"<color=yellow>TrafficSpawner {gameObject.name} running in Test Mode (No GameManager found).</color>");
            return;
        }

        // Compare strings ignoring case (e.g., 'sprint' matches 'Sprint')
        bool isMatch = string.Equals(GameManager.Instance.selectedRaceMode, requiredRaceMode, StringComparison.OrdinalIgnoreCase);

        if (!isMatch)
        {
            Debug.Log($"<color=red>TrafficSpawner {gameObject.name} disabled. Expected '{requiredRaceMode}', but GameManager says '{GameManager.Instance.selectedRaceMode}'.</color>");
            gameObject.SetActive(false);
        }
        else
        {
            Debug.Log($"<color=green>TrafficSpawner {gameObject.name} active! Mode match found: {requiredRaceMode}.</color>");
        }
    }
}