using UnityEngine;
using UnityEditor;

public class GetPolyCount : MonoBehaviour
{
    [MenuItem("Tools/Log Mesh Stats")]
    static void LogStats()
    {
        GameObject obj = Selection.activeGameObject;
        if (obj && obj.GetComponent<MeshFilter>())
        {
            Mesh mesh = obj.GetComponent<MeshFilter>().sharedMesh;
            Debug.Log($"{obj.name} Stats: {mesh.vertexCount} Verts, {mesh.triangles.Length / 3} Tris");
        }
    }
}