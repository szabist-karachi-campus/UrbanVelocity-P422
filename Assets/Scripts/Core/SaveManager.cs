using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System; // Needed for the safety net

public static class SaveManager
{
    public static void SaveGame(PlayerData data, int slotID = 1)
    {
        string path = Application.persistentDataPath + "/save_slot_" + slotID + ".dat";
        BinaryFormatter formatter = new BinaryFormatter();

        // The "using" block guarantees the file unlocks immediately, preventing Sharing Violations!
        using (FileStream stream = new FileStream(path, FileMode.Create))
        {
            formatter.Serialize(stream, data);
        }
    }

    public static PlayerData LoadGame(int slotID = 1)
    {
        string path = Application.persistentDataPath + "/save_slot_" + slotID + ".dat";
        
        if (File.Exists(path))
        {
            try
            {
                BinaryFormatter formatter = new BinaryFormatter();
                using (FileStream stream = new FileStream(path, FileMode.Open))
                {
                    // Check if it's a 0-byte ghost file
                    if (stream.Length == 0) return null; 

                    PlayerData data = formatter.Deserialize(stream) as PlayerData;
                    return data;
                }
            }
            catch (Exception e)
            {
                // If the file is corrupted, delete it automatically so the game doesn't crash
                Debug.LogWarning("Corrupted save file found in Slot " + slotID + ". Auto-cleaning... Error: " + e.Message);
                File.Delete(path);
                return null;
            }
        }
        
        return null;
    }

    public static bool DoesSaveExist(int slotID = 1)
    {
        string path = Application.persistentDataPath + "/save_slot_" + slotID + ".dat";
        if (File.Exists(path))
        {
            // Extra safety: Only say the save exists if it actually has data inside it
            FileInfo info = new FileInfo(path);
            if (info.Length > 0) return true;
        }
        return false;
    }

    public static void DeleteGame(int slotID = 1)
    {
        string path = Application.persistentDataPath + "/save_slot_" + slotID + ".dat";
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log("Deleted Save Slot " + slotID);
        }
    }
}