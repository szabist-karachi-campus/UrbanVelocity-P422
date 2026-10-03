using UnityEngine;
using System.Collections.Generic;

public class TrafficRoute : MonoBehaviour
{
    public Color lineColor = Color.yellow;
    private List<Transform> waypoints = new List<Transform>();

    // Automatically find all child objects to act as waypoints
    void OnDrawGizmos()
    {
        Gizmos.color = lineColor;
        waypoints.Clear();
        foreach (Transform t in transform)
        {
            if (t != transform) waypoints.Add(t);
        }

        for (int i = 0; i < waypoints.Count; i++)
        {
            Vector3 current = waypoints[i].position;
            Vector3 next = waypoints[(i + 1) % waypoints.Count].position; // Loop back to start
            Gizmos.DrawLine(current, next);
            Gizmos.DrawSphere(current, 0.5f);
        }
    }

    public Transform GetWaypoint(int index)
    {
        // Gather children at runtime to be safe
        if (waypoints.Count == 0)
        {
            foreach (Transform t in transform)
                if (t != transform) waypoints.Add(t);
        }
        
        if (index >= waypoints.Count) return waypoints[0]; // Loop
        return waypoints[index];
    }
}