using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Rendering;
using Unity.Collections.LowLevel.Unsafe;


public static class MeshGenM
{
    public static List<Mesh> GenerateBatch(List<Mesh> meshes, List<NativeList<float3>> verts,
        List<NativeList<float3>> normals, List<NativeList<int4>> quads, List<NativeList<NaiveSurfaceNetsM.MatBlock>> mats)
    {
        if (meshes == null || verts == null || normals == null || quads == null || mats == null)
        { Debug.LogError("Null list"); return null; }

        if ((verts.Count != normals.Count) || (verts.Count != quads.Count) || (verts.Count != meshes.Count) || (verts.Count != mats.Count))
        { Debug.LogError("Wrong mesh input"); return null; }


        int count = verts.Count;
        Job job = new Job();

        job.verts = new NativeArray<UnsafeList<float3>>(count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        job.normals = new NativeArray<UnsafeList<float3>>(count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        job.quads = new NativeArray<UnsafeList<int4>>(count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        job.mats = new NativeArray<UnsafeList<NaiveSurfaceNetsM.MatBlock>>(count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        job.vertexAttributes = MemoryManager.GetVertexAttributes();
        job.meshDataArray = Mesh.AllocateWritableMeshData(count);

        unsafe
        {
            for (int i = 0; i < count; i++)
            {
                job.verts[i] = new UnsafeList<float3>((float3*)verts[i].GetUnsafePtr(), verts[i].Length);
                job.normals[i] = new UnsafeList<float3>((float3*)normals[i].GetUnsafePtr(), normals[i].Length);
                job.quads[i] = new UnsafeList<int4>((int4*)quads[i].GetUnsafePtr(), quads[i].Length);
                job.mats[i] = new UnsafeList<NaiveSurfaceNetsM.MatBlock>((NaiveSurfaceNetsM.MatBlock*)mats[i].GetUnsafePtr(), mats[i].Length);
            }
        }

        job.ScheduleParallel(count, 1, default).Complete();

        for (int i = 0; i < verts.Count; i++)
        {
            Bounds bounds = new Bounds();
            int subMeshCount = job.meshDataArray[i].subMeshCount;

            bounds = job.meshDataArray[i].GetSubMesh(0).bounds;
            for (int j = 1; j < subMeshCount; j++)
            {
                bounds.Encapsulate(job.meshDataArray[i].GetSubMesh(j).bounds);
            }

            //meshes[i].bounds = job.meshDataArray[i].GetSubMesh(0).bounds;
            meshes[i].bounds = bounds;
        }

        MeshUpdateFlags flags = MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontResetBoneBounds |
                MeshUpdateFlags.DontValidateIndices /*| MeshUpdateFlags.DontRecalculateBounds*/;
        Mesh.ApplyAndDisposeWritableMeshData(job.meshDataArray, meshes, flags);

        job.verts.Dispose();
        job.normals.Dispose();
        job.quads.Dispose();
        job.mats.Dispose();

        return meshes;
    }

    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    private struct Job : IJobFor
    {
        [ReadOnly]
        public NativeArray<UnsafeList<float3>> verts;
        [ReadOnly]
        public NativeArray<UnsafeList<float3>> normals;
        [ReadOnly]
        public NativeArray<UnsafeList<int4>> quads;
        [ReadOnly]
        public NativeArray<UnsafeList<NaiveSurfaceNetsM.MatBlock>> mats;

        [ReadOnly]
        public NativeArray<VertexAttributeDescriptor> vertexAttributes;

        public Mesh.MeshDataArray meshDataArray;

        public void Execute(int index)
        {
            Mesh.MeshData meshData = meshDataArray[index];
            MeshUpdateFlags flags = MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontResetBoneBounds |
                MeshUpdateFlags.DontValidateIndices;

            if (quads[index].Length != 0)
            {
                meshData.SetVertexBufferParams(verts[index].Length, vertexAttributes);

                NativeArray<float3> positions = meshData.GetVertexData<float3>(0);
                unsafe
                {
                    UnsafeUtility.MemCpy(positions.GetUnsafePtr(), verts[index].Ptr, verts[index].Length * 3 * 4);
                }

                NativeArray<float3> norm = meshData.GetVertexData<float3>(1);
                unsafe
                {
                    UnsafeUtility.MemCpy(norm.GetUnsafePtr(), normals[index].Ptr, normals[index].Length * 3 * 4);
                }

                meshData.SetIndexBufferParams(quads[index].Length * 4, IndexFormat.UInt32);
                NativeArray<uint> faces = meshData.GetIndexData<uint>();
                unsafe
                {
                    UnsafeUtility.MemCpy(faces.GetUnsafePtr(), quads[index].Ptr, quads[index].Length * 4 * 4);
                }

                meshData.subMeshCount = mats[index].Length;
                for (int i = 0; i < meshData.subMeshCount; i++)
                {
                    meshData.SetSubMesh(i, new SubMeshDescriptor(
                        mats[index][i].indexStart * 4,
                        (mats[index][i].indexEnd - mats[index][i].indexStart) * 4,
                        MeshTopology.Quads), flags);
                }
            }
            else
            {
                meshData.SetVertexBufferParams(0, vertexAttributes);
                meshData.SetIndexBufferParams(0, IndexFormat.UInt32);
                meshData.subMeshCount = 1;
                meshData.SetSubMesh(0, new SubMeshDescriptor(0, 0, MeshTopology.Quads) { vertexCount = 0 }, flags);
            }
        }
    }
}
