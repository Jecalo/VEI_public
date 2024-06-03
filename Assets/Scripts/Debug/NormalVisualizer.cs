using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

public class NormalVisualizer : MonoBehaviour
{
    public bool ShowNormals = true;

    private MeshFilter meshFilter;


    private void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
    }

    private void Update()
    {
        if (ShowNormals) { AddNormalsGizmo(meshFilter.sharedMesh); }
    }

    private void AddNormalsGizmo(Mesh mesh)
    {
        if (mesh == null) { return; }
        
        var dataArray = Mesh.AcquireReadOnlyMeshData(mesh);
        if (!dataArray[0].HasVertexAttribute(VertexAttribute.Normal)) { dataArray.Dispose(); return; }
        if (dataArray[0].GetVertexAttributeFormat(VertexAttribute.Normal) != VertexAttributeFormat.Float32) { dataArray.Dispose(); return; }

        int count = dataArray[0].vertexCount;
        NativeArray<float3> verts = new NativeArray<float3>(count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        NativeArray<float3> normals = new NativeArray<float3>(count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        dataArray[0].GetVertices(verts.Reinterpret<Vector3>());
        dataArray[0].GetNormals(normals.Reinterpret<Vector3>());

        int start = GizmoInternal.Lines.Length;
        GizmoInternal.Lines.ResizeUninitialized(start + count);

        Job job = new Job();
        job.verts = verts;
        job.normals = normals;
        job.lines = GizmoInternal.Lines.AsArray();
        job.trs = transform.localToWorldMatrix;
        job.color = Color.cyan;
        job.ScheduleParallel(count, 8, default).Complete();

        verts.Dispose();
        normals.Dispose();
        dataArray.Dispose();
    }

    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    private struct Job : IJobFor
    {
        [ReadOnly]
        public NativeArray<float3> verts;
        [ReadOnly]
        public NativeArray<float3> normals;

        [WriteOnly]
        public NativeArray<GizmoInternal.PropsLine> lines;

        [ReadOnly]
        public float4x4 trs;
        [ReadOnly]
        public Vector4 color;

        public void Execute(int index)
        {
            float3 v = math.transform(trs, verts[index]);
            float3 n = math.normalize(math.rotate(trs, normals[index]));

            lines[index] = new GizmoInternal.PropsLine(v, v + n * 0.2f, color);
        }
    }
}
