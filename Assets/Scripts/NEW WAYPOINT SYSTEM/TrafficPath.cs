using System.Collections.Generic;
using UnityEngine;

public class TrafficPath : MonoBehaviour
{
    public Color pathColor = Color.yellow;
    // Using a list of Transforms so you can easily drag points around in Editor
    public List<Transform> waypoints = new List<Transform>();

    // This draws the lines in the Editor so you can see the route clearly
    private void OnDrawGizmos()
    {
        Gizmos.color = pathColor;
        for (int i = 0; i < waypoints.Count; i++)
        {
            Vector3 current = waypoints[i].position;
            // Get next waypoint, or loop back to start if it's the last one
            Vector3 next = waypoints[(i + 1) % waypoints.Count].position;
            
            Gizmos.DrawLine(current, next);
            Gizmos.DrawSphere(current, 0.5f);
        }
    }
}