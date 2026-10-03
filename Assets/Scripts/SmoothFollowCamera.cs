using UnityEngine;

public class SmoothFollowCamera : MonoBehaviour
{
    [Header("Auto-Assignment (No dragging needed)")]
    public Transform target;
    public Rigidbody targetRb;
    public string playerTag = "Player";

    [Header("Garage/Cinematic Settings")]
    public bool isGarageMode = false;

    [Header("Position Settings")]
    public float distance = 6.0f;
    public float height = 2.0f;
    public float heightDamping = 2.0f;
    public float rotationDamping = 3.0f;

    [Header("Free Look Settings")]
    public float freeLookSensitivity = 3.0f;
    public Vector2 verticalLookLimit = new Vector2(-20f, 60f);
    public float autoCenterDelay = 2.0f;

    [Header("Sense of Speed (FOV)")]
    public float minFOV = 60f;
    public float maxFOV = 85f;
    public float zoomSpeed = 2.0f;

    // Internal
    private Camera cam;
    private float freeLookX, freeLookY;
    private float lastFreeLookTime;
    private float currentRotationAngle, currentHeight;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (isGarageMode) return;

        // --- 1. AUTO-FINDER (Fixes the Ghost/Spawn issue) ---
        if (target == null)
        {
            GameObject playerCar = GameObject.FindGameObjectWithTag(playerTag);
            if (playerCar != null)
            {
                target = playerCar.transform;
                targetRb = target.GetComponent<Rigidbody>();
                freeLookX = target.eulerAngles.y;
                freeLookY = 20f;
            }
            return; 
        }

        if (targetRb == null) targetRb = target.GetComponent<Rigidbody>();

        // --- 2. CAMERA MOVEMENT ---
        float mouseX = GetAxisSafe("Mouse X");
        float mouseY = GetAxisSafe("Mouse Y");
        float joyX = GetAxisSafe("RightStickX");
        float joyY = GetAxisSafe("RightStickY");

        float inputX = (Mathf.Abs(joyX) > Mathf.Abs(mouseX)) ? joyX : mouseX;
        float inputY = (Mathf.Abs(joyY) > Mathf.Abs(mouseY)) ? joyY : mouseY;

        bool isActive = Input.GetMouseButton(1) || Mathf.Abs(joyX) > 0.1f || Mathf.Abs(joyY) > 0.1f;

        if (isActive)
        {
            lastFreeLookTime = Time.time;
            HandleFreeLook(inputX, inputY);
        }
        else
        {
            if (Time.time - lastFreeLookTime > autoCenterDelay)
                HandleStandardFollow();
            else
                HoldFreeLookAngle();
        }

        // --- 3. FOV EFFECT ---
        HandleSenseOfSpeed();
    }

    void HandleSenseOfSpeed()
    {
        if (targetRb == null) return;
        
        // Use magnitude for speed
        float speed = targetRb.linearVelocity.magnitude * 3.6f;
        float targetFOV = Mathf.Lerp(minFOV, maxFOV, speed / 200f);
        
        // Smoothly zoom the camera lens
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * zoomSpeed);
    }

    void HandleFreeLook(float xInput, float yInput)
    {
        freeLookX += xInput * freeLookSensitivity;
        freeLookY -= yInput * freeLookSensitivity;
        freeLookY = Mathf.Clamp(freeLookY, verticalLookLimit.x, verticalLookLimit.y);
        HoldFreeLookAngle();
    }

    void HoldFreeLookAngle()
    {
        Vector3 pivotPoint = target.position + (Vector3.up * (height * 0.5f));
        Quaternion rotation = Quaternion.Euler(freeLookY, freeLookX, 0);
        Vector3 position = pivotPoint - (rotation * Vector3.forward * distance);
        transform.rotation = rotation;
        transform.position = position;
    }

    void HandleStandardFollow()
    {
        float wantedRotationAngle = target.eulerAngles.y;
        float wantedHeight = target.position.y + height;

        currentRotationAngle = transform.eulerAngles.y;
        currentHeight = transform.position.y;

        currentRotationAngle = Mathf.LerpAngle(currentRotationAngle, wantedRotationAngle, rotationDamping * Time.deltaTime);
        currentHeight = Mathf.Lerp(currentHeight, wantedHeight, heightDamping * Time.deltaTime);

        Quaternion currentRotation = Quaternion.Euler(0, currentRotationAngle, 0);
        Vector3 newPosition = target.position - (currentRotation * Vector3.forward * distance);
        newPosition.y = currentHeight;

        transform.position = newPosition;
        transform.LookAt(target.position + Vector3.up * 1.0f);
    }

    private float GetAxisSafe(string axisName)
    {
        try { return Input.GetAxis(axisName); }
        catch { return 0f; }
    }
}