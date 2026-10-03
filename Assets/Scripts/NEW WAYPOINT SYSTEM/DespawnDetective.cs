using UnityEngine;

public class DespawnDetective : MonoBehaviour
{
    void OnDestroy()
    {
        // This will print a red error message in your console the exact frame the car is deleted
        Debug.LogError($"TRAFFIC DESPAWNED: {gameObject.name} at Position {transform.position}");
    }
}