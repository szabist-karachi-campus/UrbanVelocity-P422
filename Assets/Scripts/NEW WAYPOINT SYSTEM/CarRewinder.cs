using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarRewinder : MonoBehaviour
{
    public static bool isGlobalRewindPressed = false;

    [Header("Rewind Settings")]
    [Tooltip("How many seconds of history to remember")]
    public float recordTime = 5f;
    public bool isRewinding = false;

    [Header("Visuals & Effects")]
    public Transform[] visualWheels; 
    public GameObject rewindScreenEffect;

    private Rigidbody rb;
    private RigidbodyInterpolation defaultInterpolation;
    
    private MonoBehaviour playerController; 
    private MonoBehaviour aiController;
    private MonoBehaviour trafficController;
    
    private List<PointInTime> pointsInTime;
    private WheelCollider[] allWheelColliders;

    private int momentumRecoveryFrames = 0;
    private Vector3 savedVelocity;
    private Vector3 savedAngularVelocity;

    private float steeringLockTimer = 0f;

    void Start()
    {
        pointsInTime = new List<PointInTime>();
        rb = GetComponent<Rigidbody>();
        defaultInterpolation = rb.interpolation; 
        
        playerController = GetComponent("CarController") as MonoBehaviour;
        aiController = GetComponent("AICarController") as MonoBehaviour;
        trafficController = GetComponent("TrafficCar") as MonoBehaviour;
        
        allWheelColliders = GetComponentsInChildren<WheelCollider>();

        if (playerController != null)
        {
            var pc = GetComponent<CarController>(); 
            visualWheels = new Transform[] { pc.frontLeftMesh, pc.frontRightMesh, pc.rearLeftMesh, pc.rearRightMesh };
        }
        else if (aiController != null)
        {
            var ai = GetComponent<AICarController>();
            visualWheels = new Transform[] { ai.frontLeftWheelMesh, ai.frontRightWheelMesh, ai.rearLeftWheelMesh, ai.rearRightWheelMesh };
        }
        else if (trafficController != null)
        {
            var tc = GetComponent<TrafficCar>();
            visualWheels = new Transform[] { tc.fl_mesh, tc.fr_mesh, tc.rl_mesh, tc.rr_mesh }; 
        }

        if (rewindScreenEffect != null) rewindScreenEffect.SetActive(false);
    }

    void Update()
    {
        // HOLD LOGIC: Rewinds as long as the key or UI button is physically held down
        bool wantToRewind = isGlobalRewindPressed || Input.GetKey(KeyCode.R);

        if (wantToRewind && !isRewinding) StartRewind();
        else if (!wantToRewind && isRewinding) StopRewind();
    }

    void FixedUpdate()
    {
        if (isRewinding)
        {
            Rewind();
        }
        else
        {
            Record();

            if (momentumRecoveryFrames > 0)
            {
                rb.linearVelocity = savedVelocity;
                rb.angularVelocity = savedAngularVelocity;
                momentumRecoveryFrames--;
            }
        }
    }

    void LateUpdate()
    {
        // STEERING LOCKOUT: Brute-forces the steering angle to 0 so the car can't snap left/right right after letting go
        if (steeringLockTimer > 0f)
        {
            steeringLockTimer -= Time.deltaTime;
            
            foreach (WheelCollider wc in allWheelColliders)
            {
                if (wc != null)
                {
                    wc.steerAngle = 0f;
                }
            }
        }
    }

    void Rewind()
    {
        if (pointsInTime.Count > 0)
        {
            int lastIndex = pointsInTime.Count - 1;
            PointInTime pointInTime = pointsInTime[lastIndex];
            
            rb.MovePosition(pointInTime.position);
            rb.MoveRotation(pointInTime.rotation);

            if (visualWheels != null && visualWheels.Length == 4 && allWheelColliders.Length == 4)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (allWheelColliders[i] != null && visualWheels[i] != null)
                    {
                        allWheelColliders[i].GetWorldPose(out Vector3 pos, out Quaternion rot);
                        visualWheels[i].position = pos;
                        visualWheels[i].rotation = rot;
                    }
                }
            }

            if (aiController != null)
            {
                ((AICarController)aiController).currentWaypointIndex = pointInTime.waypointIndex;
                ((AICarController)aiController).currentLane = pointInTime.lane;
            }
            
            pointsInTime.RemoveAt(lastIndex);
        }
        else
        {
            // Auto-stop if we run out of recorded history while holding the button
            StopRewind();
        }
    }

    void Record()
    {
        if (pointsInTime.Count > Mathf.RoundToInt(recordTime / Time.fixedDeltaTime))
        {
            pointsInTime.RemoveAt(0);
        }

        int currentWP = 0;
        int currentLane = 0;

        if (aiController != null)
        {
            currentWP = ((AICarController)aiController).currentWaypointIndex;
            currentLane = ((AICarController)aiController).currentLane;
        }

        pointsInTime.Add(new PointInTime(
            rb.position, rb.rotation, rb.linearVelocity, rb.angularVelocity, currentWP, currentLane
        ));
    }

    public void StartRewind()
    {
        isRewinding = true;
        rb.isKinematic = true; 
        
        if (rewindScreenEffect != null) rewindScreenEffect.SetActive(true);

        if (playerController != null) playerController.enabled = false; 
        if (aiController != null) aiController.enabled = false;
        if (trafficController != null) trafficController.enabled = false;

        foreach (WheelCollider wc in allWheelColliders)
        {
            if (wc != null)
            {
                wc.motorTorque = 0f;
                wc.brakeTorque = 0f;
            }
        }
    }

    public void StopRewind()
    {
        // Prevent StopRewind from firing multiple times if already stopped
        if (!isRewinding) return; 

        isRewinding = false;
        rb.isKinematic = false; 
        
        if (rewindScreenEffect != null) rewindScreenEffect.SetActive(false);

        if (pointsInTime.Count > 0)
        {
            int lastIndex = pointsInTime.Count - 1;
            savedVelocity = pointsInTime[lastIndex].velocity;
            savedAngularVelocity = pointsInTime[lastIndex].angularVelocity;
            
            momentumRecoveryFrames = 2; 
            
            rb.linearVelocity = savedVelocity;
            rb.angularVelocity = savedAngularVelocity;
        }

        foreach (WheelCollider wc in allWheelColliders)
        {
            if (wc != null)
            {
                wc.rotationSpeed = 0f;
            }
        }

        if (playerController != null) 
        {
            playerController.enabled = true;
            steeringLockTimer = 0.3f; // Start the 0.3s steering lockout the moment control is handed back
        }
        
        if (aiController != null) aiController.enabled = true;
        
        if (trafficController != null)
        {
            trafficController.enabled = true;
            System.Reflection.FieldInfo stuckTimerField = trafficController.GetType().GetField("stuckTimer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (stuckTimerField != null) stuckTimerField.SetValue(trafficController, 0f);
        }
    }
}

public struct PointInTime
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 velocity;
    public Vector3 angularVelocity;
    
    public int waypointIndex;
    public int lane;

    public PointInTime(Vector3 _pos, Quaternion _rot, Vector3 _vel, Vector3 _angVel, int _waypointIndex, int _lane)
    {
        position = _pos;
        rotation = _rot;
        velocity = _vel;
        angularVelocity = _angVel;
        waypointIndex = _waypointIndex;
        lane = _lane;
    }
}