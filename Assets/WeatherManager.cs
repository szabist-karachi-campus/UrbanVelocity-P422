using UnityEngine;

public class WeatherManager : MonoBehaviour
{
    public enum WeatherState { Day, DayRain, Night }
    
    [Header("Current State")]
    public WeatherState currentWeather = WeatherState.Day;

    [Header("Environment References")]
    [Tooltip("Drag your main Directional Light here")]
    public Light sun;
    [Tooltip("Drag your Rain Particle System prefab/object here")]
    public GameObject rainParticleSystem;

    [Header("Cycle Timers (in seconds)")]
    [Tooltip("How long normal daytime lasts")]
    public float dayDuration = 60f;
    [Tooltip("How long the rainstorm lasts")]
    public float rainDuration = 30f;
    [Tooltip("How long the night lasts")]
    public float nightDuration = 45f;
    
    private float stateTimer = 0f;

    [Header("Lighting & Rotation")]
    public float dayIntensity = 1f;
    public float rainIntensity = 0.5f;
    public float nightIntensity = 0.15f;
    [Tooltip("How fast the sun spins across the sky (Try 1.5 to 5)")]
    public float sunRotationSpeed = 2f; 

    [Header("Tire Physics")]
    [Tooltip("Your car's normal dry sideways stiffness")]
    public float normalFriction = 1.5f; 
    [Tooltip("The slippery wet sideways stiffness")]
    public float rainFriction = 1.0f;

    void Start()
    {
        // Start the first timer based on whatever state is selected in the Inspector
        ApplyWeather(currentWeather);
        stateTimer = GetCurrentDuration();
    }

    void Update()
    {
        // 1. Constantly rotate the sun to simulate time passing!
        if (sun != null)
        {
            // Rotates smoothly along the X-axis over time
            sun.transform.Rotate(Vector3.right * sunRotationSpeed * Time.deltaTime);
        }

        // 2. Count down the clock
        stateTimer -= Time.deltaTime;

        // 3. When the timer hits zero, advance to the next phase
        if (stateTimer <= 0f)
        {
            AdvanceCycle();
        }
    }

    private void AdvanceCycle()
    {
        // Cycle Logic: Day -> Rain -> Night -> Loop back to Day
        if (currentWeather == WeatherState.Day) 
            currentWeather = WeatherState.DayRain;
        else if (currentWeather == WeatherState.DayRain) 
            currentWeather = WeatherState.Night;
        else 
            currentWeather = WeatherState.Day;

        // Apply the new visuals and physics
        ApplyWeather(currentWeather);
        
        // Reset the clock for the new phase
        stateTimer = GetCurrentDuration();
    }

    private float GetCurrentDuration()
    {
        if (currentWeather == WeatherState.Day) return dayDuration;
        if (currentWeather == WeatherState.DayRain) return rainDuration;
        return nightDuration;
    }

    private void ApplyWeather(WeatherState state)
    {
        switch (state)
        {
            case WeatherState.Day:
                if (sun != null) sun.intensity = dayIntensity;
                if (rainParticleSystem != null) rainParticleSystem.SetActive(false);
                UpdatePlayerFriction(normalFriction);
                break;

            case WeatherState.DayRain:
                if (sun != null) sun.intensity = rainIntensity;
                if (rainParticleSystem != null) rainParticleSystem.SetActive(true);
                UpdatePlayerFriction(rainFriction); // SLIPPERY!
                break;

            case WeatherState.Night:
                if (sun != null) sun.intensity = nightIntensity;
                if (rainParticleSystem != null) rainParticleSystem.SetActive(false);
                UpdatePlayerFriction(normalFriction); // Dry night
                break;
        }
    }

    private void UpdatePlayerFriction(float targetFriction)
    {
        // Dynamically find the player since your RaceManager spawns them at runtime
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        WheelCollider[] wheels = player.GetComponentsInChildren<WheelCollider>();
        foreach (WheelCollider wc in wheels)
        {
            // Safely modify the struct and re-apply it
            WheelFrictionCurve curve = wc.sidewaysFriction;
            curve.stiffness = targetFriction;
            wc.sidewaysFriction = curve;
        }
        
        Debug.Log("<color=yellow>WEATHER MANAGER: Time changed! Friction set to " + targetFriction + "</color>");
    }
}