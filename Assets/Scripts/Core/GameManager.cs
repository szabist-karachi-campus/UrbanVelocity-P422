using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Player Data")]
    public PlayerData currentPlayerProfile;
    public int currentSaveSlot = 1; // THE FIX: This is what Unity was looking for!

    [Header("Race Setup")]
    public GameObject selectedCarPrefab;
    public string selectedRaceMode;
    public bool isSplitScreen = false;

    private void Awake()
    {
        // Standard AAA Singleton Pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keeps the GameManager alive between menus and races
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SaveCurrentProfile()
    {
        if (currentPlayerProfile != null)
        {
            // Now it automatically saves to whatever slot you currently have open!
            SaveManager.SaveGame(currentPlayerProfile, currentSaveSlot);
            Debug.Log("<color=green>Game Saved Successfully to Slot " + currentSaveSlot + "</color>");
        }
    }
}