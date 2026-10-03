using UnityEngine;

public class MinimapFollow : MonoBehaviour
{
    [Header("Minimap Settings")]
    public float cameraHeight = 100f; // How high in the sky the camera sits
    public bool rotateWithCar = false; // True = map spins, False = North is always up

    private Transform playerTarget;

    void LateUpdate()
    {
        // 1. Radar: Find the car if we don't have it yet
        if (playerTarget == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTarget = playerObj.transform;
                Debug.Log("<color=green>Minimap locked onto Player!</color>");
            }
            return;
        }

        // 2. Follow the car's X and Z coordinates, but stay locked at the cameraHeight
        transform.position = new Vector3(playerTarget.position.x, playerTarget.position.y + cameraHeight, playerTarget.position.z);

        // 3. Optional Rotation
        if (rotateWithCar)
        {
            transform.rotation = Quaternion.Euler(90f, playerTarget.eulerAngles.y, 0f);
        }
        else
        {
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }
}