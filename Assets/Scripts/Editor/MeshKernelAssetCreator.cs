using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using VoxelTerrain;


public class MeshKernelAssetCreator : EditorWindow
{
    private Mesh mesh = null;

    [MenuItem("Tools/Create MeshKernel")]
    public static void ShowWindow()
    {
        EditorWindow window = EditorWindow.GetWindow(typeof(MeshKernelAssetCreator));
        window.Show();
    }

    private void OnEnable()
    {
        Rect rect = this.position;
        rect.size = new Vector2(256, 64);
        this.position = rect;
    }

    private void OnGUI()
    {
        mesh = (Mesh)EditorGUILayout.ObjectField("Mesh", mesh, typeof(Mesh), false);

        if (mesh == null) { GUI.enabled = false; }
        if (GUILayout.Button("Generate"))
        {
            if (Generate()) { Close(); }
        }
        GUI.enabled = true;
    }

    private bool Generate()
    {
        if (mesh == null) { Debug.LogWarning("No mesh selected."); return false; }
        if (mesh.subMeshCount != 1) { Debug.LogWarning("Mesh has multiple submeshes. Only the first one will be computed."); }

        string path = string.Format("Assets/{0}.asset", mesh.name);
        MeshKernelAsset meshKernel = ScriptableObject.CreateInstance<MeshKernelAsset>();
        
        var data = MeshSDF.BakeMeshSDFData(mesh, Allocator.TempJob);

        if (!data.verts.IsCreated) { return false; }

        meshKernel.vertCount = data.verts.Length;
        meshKernel.triCount = data.tris.Length;

        meshKernel.mesh = mesh;
        meshKernel.tree = data.tree.ToArray();
        meshKernel.vertWeightedNormal = data.vertWeightedNormal.Reinterpret<Vector3>().ToArray();

        int i = 0;
        meshKernel.edgeWeightedNormal_keys = new ulong[data.edgeWeightedNormal.Count()];
        meshKernel.edgeWeightedNormal_values = new Vector3[meshKernel.edgeWeightedNormal_keys.Length];

        foreach (var edge in data.edgeWeightedNormal)
        {
            meshKernel.edgeWeightedNormal_keys[i] = edge.Key;
            meshKernel.edgeWeightedNormal_values[i++] = edge.Value;
        }

        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(meshKernel, path);

        Debug.LogFormat("Successfully generated SDF data ({0}): {1} vertices, {2} triangles, {3} edges, {4} tree size",
            mesh.name, data.verts.Length, data.tris.Length, data.edgeWeightedNormal.Count(), data.tree.Length);

        data.Dispose();

        return true;
    }
}
