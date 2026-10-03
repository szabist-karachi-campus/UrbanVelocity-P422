using UnityEngine;
using System.Collections.Generic;

public class WaypointSystem : MonoBehaviour
{
    
    public Color lineColor = Color.yellow;
    public float laneWidth = 3.5f; // Adjust this to match your road texture
    public List<Transform> nodes = new List<Transform>();

    void OnDrawGizmos()
    {
        Gizmos.color = lineColor;
        nodes.Clear();
        foreach (Transform t in transform)
        {
            if (t != transform) nodes.Add(t);
        }

        for (int i = 0; i < nodes.Count - 1; i++)
        {
            Transform current = nodes[i];
            Transform next = nodes[i + 1];

            // Draw Center Line
            Gizmos.DrawLine(current.position, next.position);

            // Draw Lanes (Visual Guide)
            Vector3 dir = (next.position - current.position).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
            
            Gizmos.color = Color.green; // Right Lane
            Gizmos.DrawLine(current.position + (right * laneWidth), next.position + (right * laneWidth));
            
            Gizmos.color = Color.red; // Left Lane
            Gizmos.DrawLine(current.position - (right * laneWidth), next.position - (right * laneWidth));
        }
    }
}