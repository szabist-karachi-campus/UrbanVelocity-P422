using System.Collections.Generic;

[System.Serializable]
public class PlayerData
{
    public int totalMoney;
    public List<string> unlockedCarIDs;
    public bool hasCompletedSprint;
    public int difficulty; 
    public string saveProfileName = "New Racer";
    
    // Sensitivity Settings
    public float lookSensitivity;
    public float handlingSensitivity;

    // FIX: Moved here outside the constructor block so they are proper profile variables!
    public List<string> savedCarColors = new List<string>();
    public List<string> savedCarFinishes = new List<string>();

    public PlayerData()
    {
        totalMoney = 60000
    ; // 26 Lakhs
        unlockedCarIDs = new List<string>() { "StarterCar_01" };
        hasCompletedSprint = false;
        difficulty = 1; // 1 = Medium
        
        // Default sensitivity multipliers (1.0 = normal speed)
        lookSensitivity = 1.0f;     
        handlingSensitivity = 1.0f; 
    }
}