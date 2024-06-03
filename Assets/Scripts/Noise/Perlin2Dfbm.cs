using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using System.Runtime.CompilerServices;

public static class Perlin2Dfbm
{
    public static JobForWrapper<Job> GenerateJob(NativeArray<float> data, int resolution, int3 offset, float4x4 trs, uint seed, float frequency, int octaves, float lacunarity)
    {
        if (data.Length != (resolution * resolution)) { Debug.LogError("Data array for noise generation has wrong length."); return null; }
        if (resolution <= 0) { Debug.LogError("Too low resolution"); return null; }
        if ((resolution % 4) != 0) { Debug.LogError("Wrong resolution"); return null; }
        int vectorResolution = resolution / 4;

        if (octaves < 1) { octaves = 1; Debug.LogWarning("Too low octaves"); }
        else if (octaves > 8) { octaves = 8; Debug.LogWarning("Too high octaves"); }


        Job job = new Job();
        job.noise = data.Reinterpret<float4>(4);
        job.seed = seed;
        job.resolution = resolution;
        job.invResolution = 1.0f / resolution;
        job.trs = math.float3x4(trs.c0.xyz, trs.c1.xyz, trs.c2.xyz, trs.c3.xyz);
        job.offset = offset;
        job.frequency = frequency;
        job.octaves = octaves;
        job.lacunarity = lacunarity;

        return new JobForWrapper<Job>(job, job.noise.Length, vectorResolution);
    }

    public static void GenerateNoise(NativeArray<float> data, int resolution, float4x4 trs, uint seed, float frequency, int octaves, float lacunarity)
    {
        if (resolution <= 0) { resolution = 4; }
        else if ((resolution % 4) != 0) { resolution += (resolution % 4); }
        int vectorResolution = resolution / 4;

        if (octaves < 1) { octaves = 1; }
        else if (octaves > 8) { octaves = 8; }


        Job job = new Job();
        job.noise = data.Reinterpret<float4>(4);
        job.seed = seed;
        job.resolution = resolution;
        job.invResolution = 1.0f / resolution;
        job.trs = math.float3x4(trs.c0.xyz, trs.c1.xyz, trs.c2.xyz, trs.c3.xyz);
        job.frequency = frequency;
        job.octaves = octaves;
        job.lacunarity = lacunarity;

        job.ScheduleParallel(job.noise.Length, vectorResolution, default).Complete();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float4 CalculateNoise(SXXHash4 hash, float4 tx, float4 ty)
    {
        hash.Hash();
        float4 gx = hash.UnitFloatA * 2.0f - 1.0f;
        float4 gy = 0.5f - math.abs(gx);
        gx -= math.floor(gx + 0.5f);
        return (gx * tx + gy * ty) * (2.0f / 0.53528f);
    }

    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct Job : IJobFor
    {

        [WriteOnly]
        public NativeArray<float4> noise;

        [ReadOnly]
        public int resolution;
        [ReadOnly]
        public float invResolution;
        [ReadOnly]
        public uint seed;
        [ReadOnly]
        public float3x4 trs;
        [ReadOnly]
        public int3 offset;
        [ReadOnly]
        public float frequency;
        [ReadOnly]
        public int octaves;
        [ReadOnly]
        public float lacunarity;

        public void Execute(int i)
        {
            SXXHash4 h;
            int4 u, v, w;
            float4x3 pos;
            float4 r = new float4(0.0f, 0.0f, 0.0f, 0.0f);
            float amplitude = 1.0f;
            float amplitudeTotal = 0.0f;
            SXXHash4 seededHash;

            int4 iv = i * 4 + new int4(0, 1, 2, 3);
            pos.c0 = (float4)(iv / resolution + offset.x) * invResolution;
            pos.c1 = (float4)((iv % resolution) + offset.y) * invResolution;
            pos.c2 = new float4(0.0f, 0.0f, 0.0f, 0.0f);

            pos = NoiseHelper.TRS_vector(trs, pos * frequency);

            for(int oct = 0; oct < octaves; oct++)
            {
                seededHash = new SXXHash4(seed + (uint)oct);
                float4 tx, ty;
                float4 a, b, c, d;


                u = (int4)math.floor(pos.c0);
                v = (int4)math.floor(pos.c1);
                w = (int4)math.floor(pos.c2);

                tx = pos.c0 - math.floor(pos.c0);
                ty = pos.c1 - math.floor(pos.c1);


                h = seededHash;
                h.ConsumeCoords(u, v, w);
                a = CalculateNoise(h, tx, ty);

                h = seededHash;
                h.ConsumeCoords(u + 1, v, w);
                d = CalculateNoise(h, tx - 1.0f, ty);

                h = seededHash;
                h.ConsumeCoords(u, v + 1, w);
                b = CalculateNoise(h, tx, ty - 1.0f);

                h = seededHash;
                h.ConsumeCoords(u + 1, v + 1, w);
                c = CalculateNoise(h, tx - 1.0f, ty - 1.0f);


                tx = NoiseHelper.Smooth4(tx);
                ty = NoiseHelper.Smooth4(ty);

                float4 l1 = math.lerp(a, d, tx);
                float4 l2 = math.lerp(b, c, tx);

                r += math.lerp(l1, l2, ty) * amplitude;

                amplitudeTotal += amplitude;
                pos *= lacunarity;
                amplitude *= (1.0f / lacunarity);   //Should persistence be fixed to the inverse lacunarity?
            }

            r = r / amplitudeTotal;
            noise[i] = r;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float CalculateNoise_slow(SXXHash hash, float2 t)
    {
        hash.Hash();
        float gx = hash.UnitFloatA * 2.0f - 1.0f;
        float gy = 0.5f - math.abs(gx);
        gx -= math.floor(gx + 0.5f);
        return (gx * t.x + gy * t.y) * (2.0f / 0.53528f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Generate(uint seed, float2 p, float frequency, int octaves, float lacunarity)
    {
        float r = 0.0f;
        float amplitude = 1.0f;
        float amplitudeTotal = 0.0f;

        p *= frequency;

        for (int oct = 0; oct < octaves; oct++)
        {
            SXXHash hash;
            int2 u = (int2)math.floor(p);
            float2 t = p - math.floor(p);

            hash = new SXXHash(seed + (uint)oct);
            hash.Consume(u.x);
            hash.Consume(u.y);
            float a = CalculateNoise_slow(hash, t);

            hash = new SXXHash(seed + (uint)oct);
            hash.Consume(u.x + 1);
            hash.Consume(u.y);
            float d = CalculateNoise_slow(hash, t - new float2(1f, 0f));

            hash = new SXXHash(seed + (uint)oct);
            hash.Consume(u.x);
            hash.Consume(u.y + 1);
            float b = CalculateNoise_slow(hash, t - new float2(0f, 1f));

            hash = new SXXHash(seed + (uint)oct);
            hash.Consume(u.x + 1);
            hash.Consume(u.y + 1);
            float c = CalculateNoise_slow(hash, t - new float2(1f, 1f));

            t = NoiseHelper.Smooth2(t);

            float l1 = math.lerp(a, d, t.x);
            float l2 = math.lerp(b, c, t.x);

            r += math.lerp(l1, l2, t.y) * amplitude;

            amplitudeTotal += amplitude;
            p *= lacunarity;
            amplitude *= (1.0f / lacunarity);
        }

        r = r / amplitudeTotal;

        return r;
    }
}
