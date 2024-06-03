using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Rendering;
using Unity.Collections.LowLevel.Unsafe;


public static class MeshGen
{
    public static Mesh QuadMesh(NativeList<float3> verts, NativeList<float3> normals, NativeList<int4> quads)
    {
        if (verts.Length != normals.Length) { Debug.LogError("Vertices don't match normals: " + verts.Length + ", " + normals.Length); return null; }
        if (verts.Length == 0) { return null; }
        if (quads.Length == 0) { return null; }

        Mesh mesh = new Mesh { name = Constants.ProcMeshName };

        Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
        Mesh.MeshData meshData = meshDataArray[0];

        NativeArray<VertexAttributeDescriptor> vertexAttributes = new NativeArray<VertexAttributeDescriptor>(2, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        vertexAttributes[0] = new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0);
        vertexAttributes[1] = new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3, 1);


        meshData.SetVertexBufferParams(verts.Length, vertexAttributes);
        vertexAttributes.Dispose();

        NativeArray<float3> positions = meshData.GetVertexData<float3>(0);
        positions.CopyFrom(verts);

        NativeArray<float3> norm = meshData.GetVertexData<float3>(1);
        norm.CopyFrom(normals);

        meshData.SetIndexBufferParams(quads.Length * 4, IndexFormat.UInt32);
        NativeArray<uint> faces = meshData.GetIndexData<uint>();
        faces.Reinterpret<int4>(4).CopyFrom(quads);

        MeshUpdateFlags flags = MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontResetBoneBounds | MeshUpdateFlags.DontValidateIndices;
        meshData.subMeshCount = 1;
        meshData.SetSubMesh(0, new SubMeshDescriptor(0, quads.Length * 4, MeshTopology.Quads) { vertexCount = verts.Length }, flags);

        mesh.bounds = meshData.GetSubMesh(0).bounds;

        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);

        return mesh;
    }

    public static List<Mesh> GenerateBatch(List<Mesh> meshes, List<NativeList<float3>> verts, List<NativeList<float3>> normals, List<NativeList<int4>> quads)
    {
        if (meshes == null ||verts == null || normals == null || quads == null) { Debug.LogError("Null list"); return null; }
        if ((verts.Count != normals.Count) || (verts.Count != quads.Count) || (verts.Count != meshes.Count)) { Debug.LogError("Wrong mesh input"); return null; }

        int count = verts.Count;
        Job job = new Job();

        job.verts = new NativeArray<UnsafeList<float3>>(count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        job.normals = new NativeArray<UnsafeList<float3>>(count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        job.quads = new NativeArray<UnsafeList<int4>>(count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        job.vertexAttributes = MemoryManager.GetVertexAttributes();
        job.meshDataArray = Mesh.AllocateWritableMeshData(count);

        unsafe
        {
            for (int i = 0; i < count; i++)
            {
                job.verts[i] = new UnsafeList<float3>((float3*)verts[i].GetUnsafePtr(), verts[i].Length);
                job.normals[i] = new UnsafeList<float3>((float3*)normals[i].GetUnsafePtr(), normals[i].Length);
                job.quads[i] = new UnsafeList<int4>((int4*)quads[i].GetUnsafePtr(), quads[i].Length);
            }
        }

        job.ScheduleParallel(count, 1, default).Complete();

        for (int i = 0; i < verts.Count; i++)
        {
            meshes[i].bounds = job.meshDataArray[i].GetSubMesh(0).bounds;
        }

        MeshUpdateFlags flags = MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontResetBoneBounds |
                MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds;
        Mesh.ApplyAndDisposeWritableMeshData(job.meshDataArray, meshes, flags);

        job.verts.Dispose();
        job.normals.Dispose();
        job.quads.Dispose();

        return meshes;
    }

    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct Job : IJobFor
    {
        [ReadOnly]
        public NativeArray<UnsafeList<float3>> verts;
        [ReadOnly]
        public NativeArray<UnsafeList<float3>> normals;
        [ReadOnly]
        public NativeArray<UnsafeList<int4>> quads;

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

                meshData.subMeshCount = 1;
                meshData.SetSubMesh(0, new SubMeshDescriptor(0, quads[index].Length * 4, MeshTopology.Quads) { vertexCount = verts[index].Length }, flags);
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

    public static Mesh LineMesh(NativeList<float3x2> verts, Mesh mesh = null)
    {
        int count = verts.Length * 2;
        if (count == 0) { return null; }

        if (mesh == null) { mesh = new Mesh { name = Constants.ProcMeshName }; }

        Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
        Mesh.MeshData meshData = meshDataArray[0];

        NativeArray<VertexAttributeDescriptor> vertexAttributes = new NativeArray<VertexAttributeDescriptor>(1, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        vertexAttributes[0] = new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0);


        meshData.SetVertexBufferParams(count, vertexAttributes);
        vertexAttributes.Dispose();

        NativeArray<float3> positions = meshData.GetVertexData<float3>(0);
        positions.CopyFrom(verts.AsArray().Reinterpret<float3>(24));

        meshData.SetIndexBufferParams(count, IndexFormat.UInt32);
        JobLineIndices job = new JobLineIndices() { indices = meshData.GetIndexData<uint>() };
        job.ScheduleParallel(count, 64, default).Complete();

        MeshUpdateFlags flags = MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontResetBoneBounds | MeshUpdateFlags.DontValidateIndices;
        meshData.subMeshCount = 1;
        meshData.SetSubMesh(0, new SubMeshDescriptor(0, count, MeshTopology.Lines) { vertexCount = count }, flags);

        mesh.bounds = meshData.GetSubMesh(0).bounds;

        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);

        return mesh;
    }

    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    private struct JobLineIndices : IJobFor
    {
        [WriteOnly]
        public NativeArray<uint> indices;

        public void Execute(int index)
        {
            indices[index] = (uint)index;
        }
    }

    //Generate a new mesh from the given data. Triangle faces only, normals auto generated.
    private static Mesh Example(NativeArray<float3> vertices, NativeArray<ushort> triangles)
    {
        if ((triangles.Length % 3) != 0) { UnityEngine.Debug.LogError("Invalid amount of triangle indexes."); return null; }

        Mesh mesh = new Mesh { name = "procMesh" };
        int vertexCount = vertices.Length;
        int triangleCount = triangles.Length / 3;

        Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
        Mesh.MeshData meshData = meshDataArray[0];

        NativeArray<VertexAttributeDescriptor> vertexAttributes = new NativeArray<VertexAttributeDescriptor>(1, Allocator.Temp, NativeArrayOptions.ClearMemory);
        vertexAttributes[0] = new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0);


        meshData.SetVertexBufferParams(vertexCount, vertexAttributes);
        vertexAttributes.Dispose();


        NativeArray<float3> positions = meshData.GetVertexData<float3>(0);
        positions.CopyFrom(vertices);


        meshData.SetIndexBufferParams(triangleCount * 3, IndexFormat.UInt16);
        NativeArray<ushort> tris = meshData.GetIndexData<ushort>();
        tris.CopyFrom(triangles);

        MeshUpdateFlags flags = MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds | MeshUpdateFlags.DontValidateIndices;
        meshData.subMeshCount = 1;
        meshData.SetSubMesh(0, new SubMeshDescriptor(0, triangleCount * 3, MeshTopology.Triangles) { vertexCount = vertexCount }, flags);


        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);
        mesh.RecalculateBounds();
        mesh.Optimize();


        return mesh;
    }

    private static Mesh Example2()
    {
        Mesh mesh = new Mesh { name = "procMesh" };

        int vertexAttributeCount = 4;
        int vertexCount = 4;
        int triangleCount = 2;

        Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
        Mesh.MeshData meshData = meshDataArray[0];

        NativeArray<VertexAttributeDescriptor> vertexAttributes = new NativeArray<VertexAttributeDescriptor>(vertexAttributeCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

        vertexAttributes[0] = new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0);
        vertexAttributes[1] = new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3, 1);
        vertexAttributes[2] = new VertexAttributeDescriptor(VertexAttribute.Tangent, VertexAttributeFormat.Float16, 4, 2);
        vertexAttributes[3] = new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float16, 2, 3);

        meshData.SetVertexBufferParams(vertexCount, vertexAttributes);
        vertexAttributes.Dispose();


        NativeArray<float3> positions = meshData.GetVertexData<float3>(0);
        positions[0] = new float3(0.0f, 0.0f, 0.0f);
        positions[1] = new float3(1.0f, 0.0f, 0.0f);
        positions[2] = new float3(0.0f, 1.0f, 0.0f);
        positions[3] = new float3(1.0f, 1.0f, 0.0f);

        NativeArray<float3> normals = meshData.GetVertexData<float3>(1);
        normals[0] = new float3(0.0f, 0.0f, -1.0f);
        normals[1] = new float3(0.0f, 0.0f, -1.0f);
        normals[2] = new float3(0.0f, 0.0f, -1.0f);
        normals[3] = new float3(0.0f, 0.0f, -1.0f);

        NativeArray<half4> tangents = meshData.GetVertexData<half4>(2);
        tangents[0] = new half4((half)1.0f, (half)0.0f, (half)0.0f, (half)(-1.0f));
        tangents[1] = new half4((half)1.0f, (half)0.0f, (half)0.0f, (half)(-1.0f));
        tangents[2] = new half4((half)1.0f, (half)0.0f, (half)0.0f, (half)(-1.0f));
        tangents[3] = new half4((half)1.0f, (half)0.0f, (half)0.0f, (half)(-1.0f));

        NativeArray<half2> texCoords = meshData.GetVertexData<half2>(3);
        texCoords[0] = new half2((half)0.0f, (half)0.0f);
        texCoords[1] = new half2((half)1.0f, (half)0.0f);
        texCoords[2] = new half2((half)0.0f, (half)1.0f);
        texCoords[3] = new half2((half)1.0f, (half)1.0f);


        meshData.SetIndexBufferParams(triangleCount * 3, IndexFormat.UInt16);
        NativeArray<ushort> tris = meshData.GetIndexData<ushort>();
        tris[0] = 0;
        tris[1] = 2;
        tris[2] = 1;
        tris[3] = 1;
        tris[4] = 2;
        tris[5] = 3;

        Bounds bounds = new Bounds(new Vector3(0.5f, 0.5f, 0.0f), new Vector3(1.0f, 1.0f, 0.0f));
        MeshUpdateFlags flags = MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds | MeshUpdateFlags.DontValidateIndices;

        meshData.subMeshCount = 1;
        meshData.SetSubMesh(0, new SubMeshDescriptor(0, triangleCount * 3, MeshTopology.Triangles) { bounds = bounds, vertexCount = vertexCount }, flags);
        mesh.bounds = bounds;

        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);

        return mesh;
    }
}
