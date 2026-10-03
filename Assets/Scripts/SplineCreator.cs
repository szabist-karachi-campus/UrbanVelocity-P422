using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class SplineCreator : MonoBehaviour
{
    [Header("Settings")]
    public float segmentResolution = 20f; // Higher = smoother "snake"
    public GameObject pointVisualizerPrefab; // Optional: The sphere/shape you were clicking before

    private List<Vector3> controlPoints = new List<Vector3>();
    private LineRenderer lineRenderer;
    private Camera mainCam;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        mainCam = Camera.main;
        
        // Setup LineRenderer visually
        lineRenderer.positionCount = 0;
    }

    void Update()
    {
        // 1. Detect Click
        if (Input.GetMouseButtonDown(0))
        {
            AddPoint();
        }
    }

    void AddPoint()
    {
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            Vector3 newPoint = hit.point;
            
            // Adjust height slightly so it sits on top of the ground
            newPoint.y += 0.1f; 

            // Add to our list of control points
            controlPoints.Add(newPoint);

            // Optional: Instantiate that visual marker you had before
            if (pointVisualizerPrefab != null)
            {
                Instantiate(pointVisualizerPrefab, newPoint, Quaternion.identity);
            }

            // Update the "Snake"
            DrawSpline();
        }
    }

    void DrawSpline()
    {
        // We need at least 2 points to draw a line, 4 to draw a curve
        if (controlPoints.Count < 2) return;

        List<Vector3> curvePoints = new List<Vector3>();

        // Loop through all control points to generate the curve
        // We start at 0 and go to Count - 1 to handle the full path
        for (int i = 0; i < controlPoints.Count - 1; i++)
        {
            // Catmull-Rom requires 4 points: P0 (previous), P1 (start), P2 (end), P3 (next)
            // We clamp indices to handle the start and end of the line
            Vector3 p0 = controlPoints[Mathf.Clamp(i - 1, 0, controlPoints.Count - 1)];
            Vector3 p1 = controlPoints[i];
            Vector3 p2 = controlPoints[Mathf.Clamp(i + 1, 0, controlPoints.Count - 1)];
            Vector3 p3 = controlPoints[Mathf.Clamp(i + 2, 0, controlPoints.Count - 1)];

            // Generate intermediate points between P1 and P2
            // "segmentResolution" decides how many points make up the curve between markers
            int steps = Mathf.FloorToInt(segmentResolution);
            for (int j = 0; j <= steps; j++)
            {
                float t = j / (float)steps;
                Vector3 position = GetCatmullRomPosition(t, p0, p1, p2, p3);
                curvePoints.Add(position);
            }
        }

        // Apply calculated points to the LineRenderer
        lineRenderer.positionCount = curvePoints.Count;
        lineRenderer.SetPositions(curvePoints.ToArray());
    }

    // The Math: Calculates a position between p1 and p2 based on t (0 to 1)
    Vector3 GetCatmullRomPosition(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        // The formula for Catmull-Rom Spline
        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t
        );
    }
}