using UnityEngine;

// This line adds a magic button to your Unity right-click menu!
[CreateAssetMenu(fileName = "New Car Data", menuName = "Urban Velocity/Car Data")]
public class CarData : ScriptableObject
{

    [Header("Garage Settings")]
    [Tooltip("Tweak this up or down (e.g., 0.2 or -0.1) until the wheels touch the floor")]
    public float garageHeightOffset = 0f;

    
    [Header("Basic Info")]
    public string carID;       // e.g., "Mustang_GT"
    public string displayName; // e.g., "The Muscle"
    public int price;
    public bool isUnlockedByDefault;

    [Header("Garage UI Stats")]
    [Range(0, 100)] public float topSpeed;
    [Range(0, 100)] public float handling;
    [Range(0, 100)] public float acceleration;

    [Header("Assets")]
    public GameObject carPrefab; // The actual 3D model that spawns on the track
    public Sprite garageIcon;    // The 2D picture shown in the menu
}