using UnityEngine;

public class RacerStats : MonoBehaviour
{
    [Header("Racer Identity")]
    public string racerName = "Opponent";
    public bool isPlayer = false; 

    [Header("Race Data")]
    public int currentLap = 0;
    public int lastCheckpointIndex = 0;
    public bool hasFinished = false;
    public int finalPosition = 0;

    void Start()
    {
        if (isPlayer)
        {
            if (GameManager.Instance != null && GameManager.Instance.currentPlayerProfile != null)
            {
                racerName = GameManager.Instance.currentPlayerProfile.saveProfileName;
                Debug.Log($"<color=cyan>Player car successfully loaded with profile name: {racerName}</color>");
            }
            else
            {
                racerName = "Shahzaib"; 
                Debug.LogWarning("GameManager not found! Defaulting to testing name.");
            }
        }
    }
}