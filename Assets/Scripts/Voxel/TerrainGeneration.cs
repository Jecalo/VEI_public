using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;

namespace VoxelTerrain
{
    public static class TerrainGeneration
    {
        public static void SimpleNoise(VoxelGrid grid, float scale)
        {
            List<JobForWrapper<Perlin3Dfbm.Job>> jobs = new List<JobForWrapper<Perlin3Dfbm.Job>>();
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

            sw.Start();
            foreach (var c in grid.chunks.Values)
            {
                var job = Perlin3Dfbm.GenerateJob(c.data, grid.chunkRes,
                    new int3(c.index.x * grid.chunkRes2, c.index.y * grid.chunkRes2, c.index.z * grid.chunkRes2),
                    NoiseHelper.BuildTRS(float3.zero, float3.zero, new float3(scale, scale, scale)),
                    23, 4.0f, 2, 2.0f);

                jobs.Add(job);
                job.Schedule();
            }

            foreach (var job in jobs)
            {
                job.Handle.Complete();
            }
            sw.Stop();
            Debug.Log("Noise generation: " + sw.Elapsed.TotalMilliseconds + "ms");
        }

        public static void MovingNoise(VoxelGrid grid, float scale, float time)
        {
            List<JobForWrapper<Perlin3Dfbm.Job>> jobs = new List<JobForWrapper<Perlin3Dfbm.Job>>();
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

            sw.Start();

            VoxelGrid.ForceChunkData(grid.chunks.Values);

            foreach (var c in grid.chunks.Values)
            {
                

                var job = Perlin3Dfbm.GenerateJob(c.data, grid.chunkRes,
                    new int3(c.index.x * grid.chunkRes2, c.index.y * grid.chunkRes2, c.index.z * grid.chunkRes2),
                    NoiseHelper.BuildTRS(new float3(time, 0f, 0f), float3.zero, new float3(scale, scale, scale)),
                    23, 4.0f, 2, 2.0f);

                jobs.Add(job);
                job.Schedule();
            }

            foreach (var job in jobs)
            {
                job.Handle.Complete();
            }

            FillMat(grid, 1);

            VoxelGrid.TryRemoveChunkData(grid.chunks.Values);

            grid.SetDirtyAll();

            sw.Stop();
            Debug.Log("Moving noise: " + sw.Elapsed.TotalMilliseconds + "ms");
        }

        public static void EmptyFill(VoxelGrid grid)
        {
            foreach (var c in grid.chunks)
            {
                for (int i = 0; i < grid.chunkDataSize; i++)
                {
                    c.Value.data[i] = grid.maxClamp;
                }
            }
        }

        public static void TestFill(VoxelChunk chunk)
        {
            System.Diagnostics.Stopwatch sw = new();
            sw.Start();

            TestFillJob job = new TestFillJob();
            job.chunkData = chunk.data;
            job.chunkMatData = chunk.material;
            job.noise2d = new NativeArray<float>(chunk.grid.chunkRes * chunk.grid.chunkRes, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            job.voxelSize = chunk.grid.voxelSize;
            job.chunkRes = chunk.grid.chunkRes;
            job.chunkOffset = (float3)chunk.index * chunk.grid.chunkDisplacement;
            job.min = chunk.grid.minClamp;
            job.max = chunk.grid.maxClamp;

            var jobNoise3d = Perlin3Dfbm.GenerateJob(job.chunkData, chunk.grid.chunkRes,
                    new int3(chunk.index.x * chunk.grid.chunkRes2, chunk.index.y * chunk.grid.chunkRes2, chunk.index.z * chunk.grid.chunkRes2),
                    NoiseHelper.BuildTRS(float3.zero, float3.zero, new float3(1.0f, 1.0f, 1.0f)),
                    0, 2f, 1, 2.0f);
            jobNoise3d.Schedule().Complete();

            var jobNoise2d = Perlin2Dfbm.GenerateJob(job.noise2d, chunk.grid.chunkRes,
                    new int3(chunk.index.x * chunk.grid.chunkRes2, chunk.index.z * chunk.grid.chunkRes2, chunk.index.z * chunk.grid.chunkRes2),
                    NoiseHelper.BuildTRS(float3.zero, float3.zero, new float3(1.0f, 1.0f, 1.0f)),
                    5, 2f, 2, 0.5f);
            jobNoise2d.Schedule().Complete();

            job.Schedule().Complete();
            job.noise2d.Dispose();

            sw.Stop();
            Debug.Log("Noise gen: " + sw.Elapsed.TotalMilliseconds + "ms");
        }


        [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
        private struct TestFillJob : IJob
        {
            //[WriteOnly]
            public NativeArray<float> chunkData;
            [WriteOnly]
            public NativeArray<byte> chunkMatData;

            [ReadOnly]
            public NativeArray<float> noise2d;

            [ReadOnly]
            public float voxelSize;
            [ReadOnly]
            public int chunkRes;
            [ReadOnly]
            public float3 chunkOffset;
            [ReadOnly]
            public float min, max;

            public void Execute()
            {
                float3 p;
                int i = 0;
                for (int x = 0; x < chunkRes; x++)
                {
                    p.x = chunkOffset.x + x * voxelSize;
                    for (int y = 0; y < chunkRes; y++)
                    {
                        p.y = chunkOffset.y + y * voxelSize;
                        for (int z = 0; z < chunkRes; z++)
                        {
                            p.z = chunkOffset.z + z * voxelSize;
                            float n = chunkData[i];
                            float n2 = noise2d[z + x * chunkRes];
                            //n = 1f - n;
                            //n *= n;
                            //n = 1f - n;
                            chunkData[i] = math.clamp(n2 * 10f + n * 5f + p.y - 10f, min, max);

                            int m = 1;
                            if (p.y > 30f) { m = 3; }
                            else if (p.y > 15f) { m = 2; }
                            chunkMatData[i] = (byte)m;

                            i++;
                        }
                    }
                }
            }
        }

        public static void DefaultTerrainGeneration(IEnumerable<VoxelChunk> chunks)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            List<JobWrapper<DefaultTerrainJob>> jobs = new List<JobWrapper<DefaultTerrainJob>>();

            foreach (var c in chunks)
            {
                DefaultTerrainJob job = new DefaultTerrainJob();

                job.chunkData = c.data;
                job.chunkMatData = c.material;
                job.voxelSize = c.grid.voxelSize;
                job.chunkRes = c.grid.chunkRes;
                job.chunkOffset = (float3)c.index * c.grid.chunkDisplacement;
                job.min = c.grid.minClamp;
                job.max = c.grid.maxClamp;
                job.seed = 654621354;

                jobs.Add(new JobWrapper<DefaultTerrainJob>(job));
                jobs[jobs.Count - 1].Schedule();
            }

            foreach (var job in jobs)
            {
                job.Handle.Complete();
                //job.Run();
            }

            sw.Stop();
            DebugHelper.AddMsg(string.Format("Noise: {0} ms", sw.Elapsed.TotalMilliseconds));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void DefaultTerrain_BaseLayer(float3 p, uint seed, out float value, out byte material)
        {
            const float heightFreq = 0.004f;
            const float waterLevel = 3.5f;
            const float sandLevel = 7.0f;
            //const float range3D = 20.0f;
            const float height_base = 25.0f;
            const float height_hills = 25.0f;
            const float height_details = 1.5f;
            const float height_mountains = 250.0f;
            const byte stone = 1;
            const byte sand = 2;
            const byte water = 3;
            float v;
            byte m;


            float mountains = QuickNoise.Perlin2Dx4_(seed, p.xz, heightFreq / 4, 1);
            mountains = mountains * mountains * mountains * mountains;

            v = p.y - mountains * height_mountains - height_base;
            if (math.abs(v) > height_hills)
            {
                
            }
            else
            {
                float hills = QuickNoise.Perlin2Dx4_(seed, p.xz, heightFreq, 2);
                float details = QuickNoise.Perlin2Dx4_(seed, p.xz, heightFreq * 32.0f, 2);
                details *= details;
                v -= details * height_details + hills * height_hills;
            }

            

            //if (math.abs(v) < range3D)
            //{
            //    v += Perlin3Dfbm.Generate(seed, p, 0.025f, 1, 2f) * range3D;
            //}


            m = stone;
            if (p.y < waterLevel && v >= 0.0f)
            {
                m = water;
                v = p.y - waterLevel;
            }
            else if (p.y < sandLevel && v < 0.0f)
            {
                m = sand;
            }


            value = v;
            material = (v < 0.0f) ? m : (byte)0;
        }

        [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
        private struct DefaultTerrainJob : IJob
        {
            [WriteOnly]
            public NativeArray<float> chunkData;
            [WriteOnly]
            public NativeArray<byte> chunkMatData;

            [ReadOnly]
            public float voxelSize;
            [ReadOnly]
            public int chunkRes;
            [ReadOnly]
            public float3 chunkOffset;
            [ReadOnly]
            public float min, max;

            [ReadOnly]
            public uint seed;

            public void Execute()
            {
                float3 p;
                int i = 0;
                for (int x = 0; x < chunkRes; x++)
                {
                    p.x = chunkOffset.x + x * voxelSize;
                    for (int y = 0; y < chunkRes; y++)
                    {
                        p.y = chunkOffset.y + y * voxelSize;
                        for (int z = 0; z < chunkRes; z++)
                        {
                            p.z = chunkOffset.z + z * voxelSize;

                            float v;
                            byte m;
                            DefaultTerrain_BaseLayer(p, seed, out v, out m);

                            chunkData[i] = math.clamp(v, min, max);
                            chunkMatData[i] = m;

                            i++;
                        }
                    }
                }
            }
        }

        public static void FuncFill(VoxelGrid grid, Func<float3, float> func)
        {
            foreach (var c in grid.chunks.Values)
            {
                float3 pos;
                int i = 0;
                for (int x = 0; x < grid.chunkRes; x++)
                {
                    pos.x = c.index.x * grid.chunkDisplacement + x * grid.voxelSize;
                    for (int y = 0; y < grid.chunkRes; y++)
                    {
                        pos.y = c.index.y * grid.chunkDisplacement + y * grid.voxelSize;
                        for (int z = 0; z < grid.chunkRes; z++)
                        {
                            pos.z = c.index.z * grid.chunkDisplacement + z * grid.voxelSize;
                            c.data[i] = math.clamp(func(pos), grid.minClamp, grid.maxClamp);
                            i++;
                        }
                    }
                }
            }
        }

        public static void FillMat(VoxelGrid grid, byte matID)
        {
            foreach (var c in grid.chunks.Values)
            {
                BurstHelper.SetNativeArray<byte>(c.material, matID);
            }
        }

        public static void FuncFillMat(VoxelGrid grid, Func<float3, byte> func)
        {
            foreach (var c in grid.chunks.Values)
            {
                float3 pos;
                int i = 0;
                for (int x = 0; x < grid.chunkRes; x++)
                {
                    pos.x = c.index.x * grid.chunkDisplacement + x * grid.voxelSize;
                    for (int y = 0; y < grid.chunkRes; y++)
                    {
                        pos.y = c.index.y * grid.chunkDisplacement + y * grid.voxelSize;
                        for (int z = 0; z < grid.chunkRes; z++)
                        {
                            pos.z = c.index.z * grid.chunkDisplacement + z * grid.voxelSize;
                            c.material[i] = func(pos);
                            i++;
                        }
                    }
                }
            }
        }
    }
}
