using UnityEngine;

public class CameraPan : MonoBehaviour
{
    public float panSpeed = 10f;
    
    void Update()
    {
        // Slowly rotates the camera to the right to create a cinematic sweep
        transform.Rotate(Vector3.up, panSpeed * Time.deltaTime);
    }
}