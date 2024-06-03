using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using VoxelTerrain;


public class MeshKernelAsset : ScriptableObject
{
    public Mesh mesh;

    [HideInInspector]
    public int vertCount, triCount;
    [HideInInspector]
    public MeshSDF.Node[] tree;
    [HideInInspector]
    public Vector3[] vertWeightedNormal;
    [HideInInspector]
    public ulong[] edgeWeightedNormal_keys;
    [HideInInspector]
    public Vector3[] edgeWeightedNormal_values;


    public MeshSDF.MeshSDFData GetData()
    {
        if (mesh == null || tree.Length == 0) { Debug.LogError("Cannot extract data from null MeshKernel"); return new MeshSDF.MeshSDFData(); }
        if (mesh.vertexCount != vertCount || mesh.GetIndexCount(0) != (triCount * 3)) { Debug.LogError("Wrong mesh parameters."); return new MeshSDF.MeshSDFData(); }

        MeshSDF.MeshSDFData data = new MeshSDF.MeshSDFData();
        data.verts = new NativeArray<float3>(vertCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        data.tris = new NativeArray<int3>(triCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        data.tree = new NativeArray<MeshSDF.Node>(tree.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        data.vertWeightedNormal = new NativeArray<float3>(vertWeightedNormal.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        data.edgeWeightedNormal = new NativeHashMap<ulong, float3>(edgeWeightedNormal_keys.Length, Allocator.Persistent);

        var dataArray = Mesh.AcquireReadOnlyMeshData(mesh);
        dataArray[0].GetVertices(data.verts.Reinterpret<Vector3>());
        dataArray[0].GetIndices(data.tris.Reinterpret<int>(12), 0);
        dataArray.Dispose();

        data.tree.CopyFrom(tree);
        data.vertWeightedNormal.Reinterpret<Vector3>().CopyFrom(vertWeightedNormal);

        for (int i = 0; i < edgeWeightedNormal_keys.Length; i++)
        {
            data.edgeWeightedNormal.Add(edgeWeightedNormal_keys[i], edgeWeightedNormal_values[i]);
        }

        return data;
    }
}
