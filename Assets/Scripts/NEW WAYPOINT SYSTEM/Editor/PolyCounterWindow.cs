using UnityEngine;
using UnityEditor;

public class PolyCounterWindow : EditorWindow
{
    [MenuItem("Tools/Urban Velocity/Advanced Poly Counter")]
    public static void ShowWindow()
    {
        GetWindow<PolyCounterWindow>("Poly Counter");
    }

    void OnGUI()
    {
        // Get the currently selected object (works in Hierarchy and Project folders)
        GameObject selected = Selection.activeGameObject;

        if (selected == null)
        {
            EditorGUILayout.HelpBox("Select a GameObject or Prefab to see its polygon count.", MessageType.Info);
            return;
        }

        GUILayout.Label($"Selected: {selected.name}", EditorStyles.boldLabel);
        GUILayout.Space(5);

        int totalTris = 0;
        int totalVerts = 0;
        int meshCount = 0;

        // Find all MeshFilters (Standard 3D objects) in this object and its children
        MeshFilter[] meshFilters = selected.GetComponentsInChildren<MeshFilter>(true);
        foreach (MeshFilter mf in meshFilters)
        {
            if (mf.sharedMesh != null)
            {
                totalTris += mf.sharedMesh.triangles.Length / 3;
                totalVerts += mf.sharedMesh.vertexCount;
                meshCount++;
            }
        }

        // Find all SkinnedMeshRenderers (Animated objects/characters)
        SkinnedMeshRenderer[] skinnedMeshes = selected.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (SkinnedMeshRenderer smr in skinnedMeshes)
        {
            if (smr.sharedMesh != null)
            {
                totalTris += smr.sharedMesh.triangles.Length / 3;
                totalVerts += smr.sharedMesh.vertexCount;
                meshCount++;
            }
        }

        // Display the results
        if (meshCount > 0)
        {
            EditorGUILayout.LabelField("Meshes Found:", meshCount.ToString());
            EditorGUILayout.LabelField("Total Vertices:", totalVerts.ToString("N0"));
            EditorGUILayout.LabelField("Total Triangles:", totalTris.ToString("N0"));

            GUILayout.Space(10);

            // Warning system for optimization
            if (totalTris > 50000)
            {
                EditorGUILayout.HelpBox("CRITICAL: Over 50k Triangles! If this is a single prop or traffic car, you must decimate it in Blender to prevent lag.", MessageType.Error);
            }
            else if (totalTris > 10000)
            {
                EditorGUILayout.HelpBox("WARNING: High Poly. Safe for a player car, but too heavy for an environment prop you place multiple times.", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox("OPTIMIZED: Great job! This is well within budget.", MessageType.Info);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No 3D meshes found on this object.", MessageType.Warning);
        }
    }

    // Force the window to update instantly when you click a new object
    void OnSelectionChange()
    {
        Repaint();
    }
}