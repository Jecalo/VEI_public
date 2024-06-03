using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace VoxelTerrain
{
    public static class KernelBuilder
    {
        public static Kernel Fill(VoxelGrid grid, int3 start, int3 end, KernelConfig mode, byte material = 0)
        {
            Kernel kernel = new();

            int3 minIndex = math.min(start, end);
            int3 maxIndex = math.max(start, end);
            int3 size = maxIndex - minIndex;

            if (math.any(size <= 0)) { kernel.emptyKernel = true; return kernel; }
            else { kernel.emptyKernel = false; }

            int iterationCount = size.x * size.y * size.z;

            kernel.config = mode;
            kernel.data = new NativeArray<float>(iterationCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            kernel.matData = new NativeArray<byte>(1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            kernel.size = size;
            kernel.indexOffset = minIndex;
            kernel.matData[0] = material;
            kernel.complexMaterial = false;

            BurstHelper.SetNativeArray(kernel.data, -grid.voxelSize);

            return kernel;
        }

        public static Kernel Sphere(VoxelGrid grid, float3 pos, float radius, KernelConfig mode, byte material = 0)
        {
            System.Diagnostics.Stopwatch sw = new();
            sw.Start();

            pos = grid.WorldToLocal(pos);
            radius = math.max(radius, math.sqrt(3f * math.pow(grid.voxelSize / 2f, 2f)) + 0.01f);

            Kernel kernel = new();
            float voxelSize = grid.voxelSize;

            float3 min = pos - radius;
            float3 max = pos + radius;

            int3 minIndex = (int3)math.floor(min / voxelSize) - 1;
            int3 maxIndex = (int3)math.ceil(max / voxelSize) + 1;
            int3 size = maxIndex - minIndex;

            if (radius < (voxelSize / 4f)) { kernel.emptyKernel = true; return kernel; }
            else { kernel.emptyKernel = false; }

            int iterationCount = size.x * size.y * size.z;

            kernel.config = mode;
            kernel.data = new NativeArray<float>(iterationCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            kernel.matData = new NativeArray<byte>(1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            kernel.size = size;
            kernel.indexOffset = minIndex;
            kernel.matData[0] = material;
            kernel.complexMaterial = false;

            KernelSphereJob job = new();
            job.data = kernel.data;
            job.voxelSize = voxelSize;
            job.sizeZ = size.z;
            job.stepYZ = size.y * size.z;
            job.radius = radius;
            job.pos = pos;
            job.offset = minIndex;
            job.min = grid.minClamp;
            job.max = grid.maxClamp;

            job.ScheduleParallel(iterationCount, 32, default).Complete();

            sw.Stop();
            DebugHelper.AddMsg(string.Format("Kernel build (sphere): {0}ms", sw.Elapsed.TotalMilliseconds));

            return kernel;
        }

        [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
        private struct KernelSphereJob : IJobFor
        {
            [WriteOnly]
            public NativeArray<float> data;

            [ReadOnly]
            public float voxelSize;
            [ReadOnly]
            public int sizeZ;
            [ReadOnly]
            public int stepYZ;
            [ReadOnly]
            public float radius;
            [ReadOnly]
            public float3 pos;
            [ReadOnly]
            public int3 offset;
            [ReadOnly]
            public float min, max;

            public void Execute(int index)
            {
                int3 i;
                i.x = index / stepYZ + offset.x;
                i.y = (index % stepYZ) / sizeZ + offset.y;
                i.z = index % sizeZ + offset.z;

                float3 v = (float3)i * voxelSize - pos;

                data[index] = math.clamp(math.length(v) - radius, min, max);
            }
        }

        public static Kernel Box(VoxelGrid grid, float3 pos, float3 rot, float3 extents, KernelConfig mode, byte material = 0)
        {
            System.Diagnostics.Stopwatch sw = new();
            sw.Start();

            Kernel kernel = new();
            float voxelSize = grid.voxelSize;

            float4x4 trs = math.mul(grid.invTrs, float4x4.TRS(pos, quaternion.Euler(math.radians(rot)), 1f));
            BurstHelper.BoxBounds(trs, extents, out float3 min, out float3 max);

            int3 minIndex = (int3)math.floor(min / voxelSize) - 1;
            int3 maxIndex = (int3)math.ceil(max / voxelSize) + 1;
            int3 size = maxIndex - minIndex;

            if (math.any(extents < (voxelSize / 4f))) { kernel.emptyKernel = true; return kernel; }
            else { kernel.emptyKernel = false; }

            int iterationCount = size.x * size.y * size.z;

            kernel.config = mode;
            kernel.data = new NativeArray<float>(iterationCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            kernel.matData = new NativeArray<byte>(1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            kernel.size = size;
            kernel.indexOffset = minIndex;
            kernel.matData[0] = material;
            kernel.complexMaterial = false;

            KernelBoxJob job = new();
            job.data = kernel.data;
            job.voxelSize = voxelSize;
            job.sizeZ = size.z;
            job.stepYZ = size.y * size.z;
            job.extents = extents * 0.5f;
            job.trs = math.inverse(trs);
            job.offset = minIndex;
            job.min = grid.minClamp;
            job.max = grid.maxClamp;

            job.ScheduleParallel(iterationCount, 32, default).Complete();

            sw.Stop();
            DebugHelper.AddMsg(string.Format("Kernel build (box): {0}ms", sw.Elapsed.TotalMilliseconds));

            return kernel;
        }

        [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
        private struct KernelBoxJob : IJobFor
        {
            [WriteOnly]
            public NativeArray<float> data;

            [ReadOnly]
            public float voxelSize;
            [ReadOnly]
            public int sizeZ;
            [ReadOnly]
            public int stepYZ;
            [ReadOnly]
            public float3 extents;
            [ReadOnly]
            public float4x4 trs;
            [ReadOnly]
            public int3 offset;
            [ReadOnly]
            public float min, max;

            public void Execute(int index)
            {
                int3 i;
                i.x = index / stepYZ + offset.x;
                i.y = (index % stepYZ) / sizeZ + offset.y;
                i.z = index % sizeZ + offset.z;

                float3 v = math.transform(trs, (float3)i * voxelSize);
                float3 q = math.abs(v) - extents;
                float qm = math.max(math.max(q.x, q.y), q.z);

                data[index] = math.clamp(math.length(math.max(q, 0.0f)) + math.min(qm, 0.0f), min, max);
            }
        }

        public static Kernel Capsule(VoxelGrid grid, float3 a, float3 b, float radius, KernelConfig mode, byte material = 0)
        {
            System.Diagnostics.Stopwatch sw = new();
            sw.Start();

            Kernel kernel = new();
            float voxelSize = grid.voxelSize;

            a = grid.WorldToLocal(a);
            b = grid.WorldToLocal(b);

            float3 min = math.min(a, b) - radius;
            float3 max = math.max(a, b) + radius;

            int3 minIndex = (int3)math.floor(min / voxelSize) - 1;
            int3 maxIndex = (int3)math.ceil(max / voxelSize) + 1;
            int3 size = maxIndex - minIndex;

            if (radius < (voxelSize / 4f)) { kernel.emptyKernel = true; return kernel; }
            else { kernel.emptyKernel = false; }

            int iterationCount = size.x * size.y * size.z;

            kernel.config = mode;
            kernel.data = new NativeArray<float>(iterationCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            kernel.matData = new NativeArray<byte>(1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            kernel.size = size;
            kernel.indexOffset = minIndex;
            kernel.matData[0] = material;
            kernel.complexMaterial = false;

            KernelCapsuleJob job = new();
            job.data = kernel.data;
            job.voxelSize = voxelSize;
            job.sizeZ = size.z;
            job.stepYZ = size.y * size.z;
            job.radius = radius;
            job.a = a;
            job.b = b;
            job.offset = minIndex;
            job.min = grid.minClamp;
            job.max = grid.maxClamp;

            job.ScheduleParallel(iterationCount, 32, default).Complete();

            sw.Stop();
            DebugHelper.AddMsg(string.Format("Kernel build (capsule): {0}ms", sw.Elapsed.TotalMilliseconds));

            return kernel;
        }

        [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
        private struct KernelCapsuleJob : IJobFor
        {
            [WriteOnly]
            public NativeArray<float> data;

            [ReadOnly]
            public float voxelSize;
            [ReadOnly]
            public int sizeZ;
            [ReadOnly]
            public int stepYZ;
            [ReadOnly]
            public float radius;
            [ReadOnly]
            public float3 a, b;
            [ReadOnly]
            public int3 offset;
            [ReadOnly]
            public float min, max;

            public void Execute(int index)
            {
                int3 i;
                i.x = index / stepYZ + offset.x;
                i.y = (index % stepYZ) / sizeZ + offset.y;
                i.z = index % sizeZ + offset.z;

                float3 p = (float3)i * voxelSize;
                float3 pa = p - a;
                float3 ba = b - a;
                float h = math.clamp(math.dot(pa, ba) / math.dot(ba, ba), 0f, 1f);
                data[index] = math.clamp(math.length(pa - ba * h) - radius, min, max);
            }
        }

        public static Kernel Speck(VoxelGrid grid, float3 pos, KernelConfig mode, byte material = 0)
        {
            throw new System.NotImplementedException();
            //float r = grid.voxelSize / 2f;
            //return Sphere(grid, pos, math.sqrt(3f * r * r) + 0.01f, mode, material);
        }

        public static Kernel MeshKernel(VoxelGrid grid, Mesh mesh, float3 pos, float3 rot, float3 scale, KernelConfig mode, byte material = 0)
        {
            Kernel kernel = new Kernel();

            if (mesh == null) { Debug.LogError("Null mesh"); kernel.emptyKernel = true; return kernel; }
            if (!mesh.isReadable) { Debug.LogError("Non readable mesh."); kernel.emptyKernel = true; return kernel; }

            System.Diagnostics.Stopwatch sw = new();
            sw.Start();

            kernel.emptyKernel = false;
            MeshSDF.MeshSDFData meshData = MeshSDF.BakeMeshSDFData(mesh, Allocator.TempJob);

            sw.Stop();
            DebugHelper.AddMsg(string.Format("MeshSDFData gen: {0}ms", sw.Elapsed.TotalMilliseconds));

            kernel = MeshKernel(grid, meshData, pos, rot, scale, mode, material);
            meshData.Dispose();

            return kernel;
        }

        public static Kernel MeshKernel(VoxelGrid grid, MeshSDF.MeshSDFData meshData, float3 pos, float3 rot, float3 scale, KernelConfig mode, byte material = 0)
        {
            Kernel kernel = new();

            if (!meshData.verts.IsCreated) { Debug.LogError("Empty meshData."); kernel.emptyKernel = true; return kernel; }

            System.Diagnostics.Stopwatch sw = new();
            sw.Start();

            float voxelSize = grid.voxelSize;
            kernel.emptyKernel = false;

            Bounds bounds = MeshSDF.CalculateBounds(meshData, grid.WorldToLocal(pos), rot, scale);
            float3 mesh_min = bounds.min;
            float3 mesh_max = bounds.max;


            int3 minIndex = (int3)math.floor(mesh_min / voxelSize) - 1;
            int3 maxIndex = (int3)math.ceil(mesh_max / voxelSize) + 1;

            int3 size = maxIndex - minIndex;

            int iterationCount = size.x * size.y * size.z;

            kernel.config = mode;
            kernel.data = new NativeArray<float>(iterationCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            kernel.matData = new NativeArray<byte>(1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            kernel.size = size;
            kernel.indexOffset = minIndex;
            kernel.matData[0] = material;
            kernel.complexMaterial = false;

            float4x4 trs = math.mul(grid.invTrs, float4x4.TRS(pos, quaternion.Euler(math.radians(rot)), scale));

            MeshSDF.GenSDF(meshData, kernel.data, trs, minIndex, voxelSize, size, grid.minClamp, grid.maxClamp);

            sw.Stop();
            DebugHelper.AddMsg(string.Format("Kernel build (meshKernel): {0}ms", sw.Elapsed.TotalMilliseconds));

            return kernel;
        }
    }
}
