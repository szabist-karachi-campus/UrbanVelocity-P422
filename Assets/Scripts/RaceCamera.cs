using UnityEngine;

public class RaceCamera : MonoBehaviour
{
    public Transform targetCar;
    public Rigidbody carRb;

    [Header("Position")]
    public float distance = 6.0f;
    public float height = 2.5f;
    public float smoothSpeed = 10f;

    [Header("Stable FOV Settings")]
    public float minFOV = 60f;
    public float maxFOV = 85f; // Reduced slightly to prevent "fish eye"
    public float zoomSpeed = 2f; // LOWERED: Makes the zoom much slower and smoother

    private float smoothedSpeedForFOV = 0f; // Stores the filtered speed

    void LateUpdate()
    {
        if (!targetCar || !carRb) return;

        // --- 1. Position Logic ---
        Vector3 targetPos = targetCar.position - (targetCar.forward * distance) + (Vector3.up * height);
        transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);
        transform.LookAt(targetCar.position + Vector3.up * 0.5f);

        // --- 2. STABILIZED FOV LOGIC (The Fix) ---

        // A. Get Velocity but ZERO out the Y (Up/Down) axis
        // We only care how fast we are moving along the ground, not bouncing up.
        Vector3 flatVelocity = new Vector3(carRb.linearVelocity.x, 0f, carRb.linearVelocity.z);
        float realSpeed = flatVelocity.magnitude * 3.6f;

        // B. Apply Strong Smoothing
        // If the speed jumps from 100 to 105 instantly, we only allow it to move 1 unit.
        smoothedSpeedForFOV = Mathf.MoveTowards(smoothedSpeedForFOV, realSpeed, 50f * Time.deltaTime);

        // C. Calculate Target FOV
        float targetFOV = Mathf.Lerp(minFOV, maxFOV, smoothedSpeedForFOV / 200f);

        // D. Apply to Camera (Very soft dampening)
        Camera.main.fieldOfView = Mathf.Lerp(Camera.main.fieldOfView, targetFOV, Time.deltaTime * 2f);
    }
}