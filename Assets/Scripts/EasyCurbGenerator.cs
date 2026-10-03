using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways] 
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class AdvancedCurbGenerator : MonoBehaviour
{
    [Header("1. Drag Points Here")]
    public Transform[] points; 

    [Header("2. Curb Block Dimensions")]
    public float curbWidth = 0.3f;   // Thickness of the concrete border
    public float curbHeight = 0.4f;  // Height above the road level

    [Header("3. Central Greenery Bed (SubMesh 1)")]
    public float centralDirtWidth = 3.0f; // Width of the grass/sand section inside

    [Header("4. Path Settings")]
    public float curvature = 10f; 
    public float textureTiling = 5f;

    [Header("5. Tree & Bush Spawner")]
    public GameObject[] propPrefabs; // Drag your tree/bush prefabs here
    public float propSpacing = 8f;   // Distance between trees along the median
    [Range(0f, 1f)] public float spawnChance = 0.9f; 
    [Tooltip("Manually sink or lift props. Negative values sink into dirt, positive values float them up.")]
    public float propHeightOffset = 0f; 

    [Header("6. Tree Collider Settings")]
    [Tooltip("Should each spawned tree automatically receive a simplified box collider zone?")]
    public bool generateColliders = true;
    [Tooltip("The thickness/width of the trunk collider box profile.")]
    public float treeColliderWidth = 0.5f;
    [Tooltip("The vertical height length of the trunk collider box zone.")]
    public float treeColliderHeight = 6.0f;
    
    [Space(10)]
    [Tooltip("Click this checkbox to plant the trees inside the median slot!")]
    public bool clickToSpawnTrees = false; 

    private List<Vector3> cachedPath = new List<Vector3>();

    private void Update()
    {
        if (points == null || points.Length < 2) return;
        GenerateMesh();

        if (clickToSpawnTrees)
        {
            clickToSpawnTrees = false;
            SpawnEnvironmentProps();
        }
    }

    void GenerateMesh()
    {
        List<Vector3> verts = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        
        List<int> curbTris = new List<int>();
        List<int> grassTris = new List<int>();

        // 1. Calculate the Smooth Path via Catmull-Rom
        cachedPath.Clear();
        int segments = (int)curvature;

        for (int i = 0; i < points.Length - 1; i++)
        {
            Vector3 p0 = i == 0 ? points[0].position : points[i - 1].position;
            Vector3 p1 = points[i].position;
            Vector3 p2 = points[i + 1].position;
            Vector3 p3 = i == points.Length - 2 ? points[i + 1].position : points[i + 2].position;

            for (int t = 0; t < segments; t++)
            {
                cachedPath.Add(GetCatmullRomPosition(t / (float)segments, p0, p1, p2, p3));
            }
        }
        cachedPath.Add(points[points.Length - 1].position);

        // 2. Extrude the 6-Vertex Closed Profile along the Path
        float distanceAlongPath = 0f;

        for (int i = 0; i < cachedPath.Count; i++)
        {
            Vector3 forward = Vector3.forward;
            if (i < cachedPath.Count - 1) forward = (cachedPath[i + 1] - cachedPath[i]).normalized;
            else if (i > 0) forward = (cachedPath[i] - cachedPath[i - 1]).normalized;
            
            Vector3 up = Vector3.up;
            Vector3 right = Vector3.Cross(up, forward).normalized;

            Vector3 center = cachedPath[i];
            
            // Generate the 6 profile vertices for a solid structure
            Vector3 v0 = center - (right * (centralDirtWidth / 2f + curbWidth)); // Bottom Left (Road)
            Vector3 v1 = v0 + (up * curbHeight);                                // Top Left Outer Corner
            Vector3 v2 = v1 + (right * curbWidth);                              // Top Left Inner Corner (Dirt start)
            Vector3 v3 = center + (right * (centralDirtWidth / 2f)) + (up * curbHeight); // Top Right Inner Corner (Dirt end)
            Vector3 v4 = v3 + (right * curbWidth);                              // Top Right Outer Corner
            Vector3 v5 = v4 - (up * curbHeight);                                // Bottom Right (Road)

            // Local conversions
            verts.Add(transform.InverseTransformPoint(v0));
            verts.Add(transform.InverseTransformPoint(v1));
            verts.Add(transform.InverseTransformPoint(v2));
            verts.Add(transform.InverseTransformPoint(v3));
            verts.Add(transform.InverseTransformPoint(v4));
            verts.Add(transform.InverseTransformPoint(v5));

            if (i > 0) distanceAlongPath += Vector3.Distance(cachedPath[i], cachedPath[i - 1]);
            float v = distanceAlongPath / textureTiling;

            // --- FIXED UV ROTATION ENGINE ---
            uvs.Add(new Vector2(v, 0.0f));
            uvs.Add(new Vector2(v, 0.2f));
            uvs.Add(new Vector2(v, 0.4f));
            uvs.Add(new Vector2(v, 0.6f));
            uvs.Add(new Vector2(v, 0.8f));
            uvs.Add(new Vector2(v, 1.0f));
        }

        // 3. Connect the segments with Quads
        for (int i = 0; i < cachedPath.Count - 1; i++)
        {
            int r = i * 6;        // Current row root index
            int n = (i + 1) * 6;  // Next row root index
            
            // SUBMESH 0: Concrete Curbs
            AddQuad(curbTris, n + 0, n + 1, r + 1, r + 0); // Left outer wall
            AddQuad(curbTris, r + 1, n + 1, n + 2, r + 2); // Left horizontal curb top step
            AddQuad(curbTris, r + 3, n + 3, n + 4, r + 4); // Right horizontal curb top step
            AddQuad(curbTris, r + 5, r + 4, n + 4, n + 5); // Right outer wall

            // SUBMESH 1: Center Dirt/Grass bed container
            AddQuad(grassTris, r + 2, n + 2, n + 3, r + 3); 
        }

        // 4. Solid Structural End-Caps
        curbTris.Add(0); curbTris.Add(5); curbTris.Add(1);
        curbTris.Add(1); curbTris.Add(5); curbTris.Add(4);
        curbTris.Add(1); curbTris.Add(4); curbTris.Add(2);
        curbTris.Add(2); curbTris.Add(4); curbTris.Add(3);

        int eRoot = (cachedPath.Count - 1) * 6;
        curbTris.Add(eRoot + 0); curbTris.Add(eRoot + 1); curbTris.Add(eRoot + 5);
        curbTris.Add(eRoot + 1); curbTris.Add(eRoot + 4); curbTris.Add(eRoot + 5);
        curbTris.Add(eRoot + 1); curbTris.Add(eRoot + 2); curbTris.Add(eRoot + 4);
        curbTris.Add(eRoot + 2); curbTris.Add(eRoot + 3); curbTris.Add(eRoot + 4);

        // 5. Build and Apply to Mesh
        Mesh mesh = new Mesh();
        mesh.vertices = verts.ToArray();
        mesh.uv = uvs.ToArray();
        
        mesh.subMeshCount = 2; 
        mesh.SetTriangles(curbTris, 0);
        mesh.SetTriangles(grassTris, 1);
        
        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshCollider>().sharedMesh = mesh; 
    }

    void AddQuad(List<int> triList, int bL, int tL, int tR, int bR)
    {
        triList.Add(bL); triList.Add(tL); triList.Add(tR);
        triList.Add(bL); triList.Add(tR); triList.Add(bR);
    }

    public void SpawnEnvironmentProps()
    {
        Transform container = transform.Find("EnvironmentProps");
        if (container != null) DestroyImmediate(container.gameObject);

        if (propPrefabs == null || propPrefabs.Length == 0 || cachedPath.Count < 2) return;

        container = new GameObject("EnvironmentProps").transform;
        container.SetParent(transform);
        container.localPosition = Vector3.zero;

        float distanceTraveled = 0f;

        for (int i = 0; i < cachedPath.Count - 1; i++)
        {
            distanceTraveled += Vector3.Distance(cachedPath[i], cachedPath[i + 1]);

            if (distanceTraveled >= propSpacing)
            {
                distanceTraveled = 0f; 

                if (Random.value <= spawnChance)
                {
                    Vector3 forward = (cachedPath[i + 1] - cachedPath[i]).normalized;
                    Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

                    float centerPlacementJitter = Random.Range(-centralDirtWidth * 0.25f, centralDirtWidth * 0.25f);
                    
                    // Height offset applied calculation zone
                    float finalSpawnHeight = curbHeight + propHeightOffset;
                    Vector3 spawnPos = cachedPath[i] + (right * centerPlacementJitter) + (Vector3.up * finalSpawnHeight);

                    GameObject prefabToSpawn = propPrefabs[Random.Range(0, propPrefabs.Length)];
                    GameObject newProp = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity, container);

                    newProp.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);

                    // Physics auto-generation block
                    if (generateColliders)
                    {
                        BoxCollider trunkCollider = newProp.AddComponent<BoxCollider>();
                        trunkCollider.size = new Vector3(treeColliderWidth, treeColliderHeight, treeColliderWidth);
                        trunkCollider.center = new Vector3(0f, treeColliderHeight / 2f, 0f);
                    }
                }
            }
        }
    }

    Vector3 GetCatmullRomPosition(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        return 0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t + (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t);
    }
}