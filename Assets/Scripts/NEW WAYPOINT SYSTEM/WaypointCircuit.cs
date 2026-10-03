using UnityEngine;
using System.Collections.Generic;

public class WaypointCircuit : MonoBehaviour {
    public List<Transform> nodes = new List<Transform>();

    void OnDrawGizmos() {
        Gizmos.color = Color.yellow;
        nodes.Clear();
        foreach (Transform child in transform) nodes.Add(child);

        for (int i = 0; i < nodes.Count; i++) {
            Vector3 current = nodes[i].position;
            Vector3 next = nodes[(i + 1) % nodes.Count].position;
            Gizmos.DrawLine(current, next);
            Gizmos.DrawSphere(current, 1f);
        }
    }
}