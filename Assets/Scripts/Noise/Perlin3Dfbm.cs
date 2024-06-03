using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using System.Runtime.CompilerServices;

public static class Perlin3Dfbm
{
    public static JobForWrapper<Job> GenerateJob(NativeArray<float> data, int resolution, int3 offset, float4x4 trs, uint seed, float frequency, int octaves, float lacunarity)
    {
        if (data.Length != (resolution * resolution * resolution)) { Debug.LogError("Data array for noise generation has wrong length."); return null; }
        if (resolution <= 0) { Debug.LogError("Too low resolution"); return null; }
        if ((resolution % 4) != 0) { Debug.LogError("Wrong resolution"); return null; }
        int vectorResolution = resolution / 4;

        if (octaves < 1) { octaves = 1; Debug.LogWarning("Too low octaves"); }
        else if (octaves > 8) { octaves = 8; Debug.LogWarning("Too high octaves"); }


        Job job = new Job();
        job.noise = data.Reinterpret<float4>(4);
        job.seed = seed;
        job.resolution = resolution;
        job.step = resolution * resolution;
        job.invResolution = 1.0f / resolution;
        job.trs = math.float3x4(trs.c0.xyz, trs.c1.xyz, trs.c2.xyz, trs.c3.xyz);
        job.offset = offset;
        job.frequency = frequency;
        job.octaves = octaves;
        job.lacunarity = lacunarity;

        return new JobForWrapper<Job>(job, job.noise.Length, vectorResolution);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float4 CalculateNoise(SXXHash4 hash, float4 tx, float4 ty, float4 tz)
    {
        hash.Hash();
        float4 gx = hash.UnitFloatA * 2.0f - 1.0f, gy = hash.UnitFloatD * 2.0f - 1.0f;
        float4 gz = 1f - math.abs(gx) - math.abs(gy);
        float4 offset = math.max(-gz, 0.0f);
        gx += math.select(-offset, offset, gx < 0.0f);
        gy += math.select(-offset, offset, gy < 0.0f);
        return (gx * tx + gy * ty + gz * tz) * (1.0f / 0.56290f);
    }

    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct Job : IJobFor
    {

        [WriteOnly]
        public NativeArray<float4> noise;

        [ReadOnly]
        public int resolution;
        [ReadOnly]
        public int step;
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
            SXXHash4 hash;
            float4x3 pos;
            float4 r = new float4(0.0f, 0.0f, 0.0f, 0.0f);
            float amplitude = 1.0f;
            float amplitudeTotal = 0.0f;
            SXXHash4 seededHash;

            int4 iv = i * 4 + new int4(0, 1, 2, 3);
            pos.c0 = (float4)((iv / step) + offset.x) * invResolution;
            pos.c1 = (float4)(((iv % step) / resolution) + offset.y) * invResolution;
            pos.c2 = (float4)((iv % resolution) + offset.z) * invResolution;

            pos = NoiseHelper.TRS_vector(trs, pos * frequency);

            for (int oct = 0; oct < octaves; oct++)
            {
                seededHash = new SXXHash4(seed + (uint)oct);
                float4 p0, p1, p2, p3, p4, p5, p6, p7;


                int4 u = (int4)math.floor(pos.c0);
                int4 v = (int4)math.floor(pos.c1);
                int4 w = (int4)math.floor(pos.c2);

                float4 tx = pos.c0 - math.floor(pos.c0);
                float4 ty = pos.c1 - math.floor(pos.c1);
                float4 tz = pos.c2 - math.floor(pos.c2);

                hash = seededHash;
                hash.ConsumeCoords(u, v, w);
                p0 = CalculateNoise(hash, tx, ty, tz);

                hash = seededHash;
                hash.ConsumeCoords(u + 1, v, w);
                p3 = CalculateNoise(hash, tx - 1.0f, ty, tz);

                hash = seededHash;
                hash.ConsumeCoords(u, v + 1, w);
                p1 = CalculateNoise(hash, tx, ty - 1.0f, tz);

                hash = seededHash;
                hash.ConsumeCoords(u + 1, v + 1, w);
                p2 = CalculateNoise(hash, tx - 1.0f, ty - 1.0f, tz);

                hash = seededHash;
                hash.ConsumeCoords(u, v, w + 1);
                p4 = CalculateNoise(hash, tx, ty, tz - 1.0f);

                hash = seededHash;
                hash.ConsumeCoords(u + 1, v, w + 1);
                p7 = CalculateNoise(hash, tx - 1.0f, ty, tz - 1.0f);

                hash = seededHash;
                hash.ConsumeCoords(u, v + 1, w + 1);
                p5 = CalculateNoise(hash, tx, ty - 1.0f, tz - 1.0f);

                hash = seededHash;
                hash.ConsumeCoords(u + 1, v + 1, w + 1);
                p6 = CalculateNoise(hash, tx - 1.0f, ty - 1.0f, tz - 1.0f);


                tx = NoiseHelper.Smooth4(tx);
                ty = NoiseHelper.Smooth4(ty);
                tz = NoiseHelper.Smooth4(tz);

                float4 l0 = math.lerp(p0, p3, tx);
                float4 l1 = math.lerp(p1, p2, tx);
                float4 l2 = math.lerp(p4, p7, tx);
                float4 l3 = math.lerp(p5, p6, tx);

                r += math.lerp(math.lerp(l0, l1, ty), math.lerp(l2, l3, ty), tz) * amplitude;

                amplitudeTotal += amplitude;
                pos *= lacunarity;
                amplitude *= (1.0f / lacunarity);
            }

            r = r / amplitudeTotal;
            noise[i] = r;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float CalculateNoise_slow(SXXHash hash, float3 t)
    {
        hash.Hash();
        float gx = hash.UnitFloatA * 2.0f - 1.0f;
        float gy = hash.UnitFloatD * 2.0f - 1.0f;
        float gz = 1f - math.abs(gx) - math.abs(gy);
        float offset = math.max(-gz, 0.0f);
        gx += math.select(-offset, offset, gx < 0.0f);
        gy += math.select(-offset, offset, gy < 0.0f);
        return (gx * t.x + gy * t.y + gz * t.z) * (1.0f / 0.56290f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Generate(uint seed, float3 p, float frequency, int octaves, float lacunarity)
    {
        float r = 0.0f;
        float amplitude = 1.0f;
        float amplitudeTotal = 0.0f;

        p *= frequency;

        for (int oct = 0; oct < octaves; oct++)
        {
            SXXHash hash;
            int3 u = (int3)math.floor(p);
            float3 t = p - math.floor(p);

            hash = new SXXHash(seed + (uint)octaves);
            hash.Consume(u.x);
            hash.Consume(u.y);
            hash.Consume(u.z);
            float p0 = CalculateNoise_slow(hash, t);

            hash = new SXXHash(seed + (uint)octaves);
            hash.Consume(u.x + 1);
            hash.Consume(u.y);
            hash.Consume(u.z);
            float p3 = CalculateNoise_slow(hash, t - new float3(1f, 0f, 0f));

            hash = new SXXHash(seed + (uint)octaves);
            hash.Consume(u.x);
            hash.Consume(u.y + 1);
            hash.Consume(u.z);
            float p1 = CalculateNoise_slow(hash, t - new float3(0f, 1f, 0f));

            hash = new SXXHash(seed + (uint)octaves);
            hash.Consume(u.x + 1);
            hash.Consume(u.y + 1);
            hash.Consume(u.z);
            float p2 = CalculateNoise_slow(hash, t - new float3(1f, 1f, 0f));

            hash = new SXXHash(seed + (uint)octaves);
            hash.Consume(u.x);
            hash.Consume(u.y);
            hash.Consume(u.z + 1);
            float p4 = CalculateNoise_slow(hash, t - new float3(0f, 0f, 1f));

            hash = new SXXHash(seed + (uint)octaves);
            hash.Consume(u.x + 1);
            hash.Consume(u.y);
            hash.Consume(u.z + 1);
            float p7 = CalculateNoise_slow(hash, t - new float3(1f, 0f, 1f));

            hash = new SXXHash(seed + (uint)octaves);
            hash.Consume(u.x);
            hash.Consume(u.y + 1);
            hash.Consume(u.z + 1);
            float p5 = CalculateNoise_slow(hash, t - new float3(0f, 1f, 1f));

            hash = new SXXHash(seed + (uint)octaves);
            hash.Consume(u.x + 1);
            hash.Consume(u.y + 1);
            hash.Consume(u.z + 1);
            float p6 = CalculateNoise_slow(hash, t - new float3(1f, 1f, 1f));

            t = NoiseHelper.Smooth3(t);

            float l0 = math.lerp(p0, p3, t.x);
            float l1 = math.lerp(p1, p2, t.x);
            float l2 = math.lerp(p4, p7, t.x);
            float l3 = math.lerp(p5, p6, t.x);

            r += math.lerp(math.lerp(l0, l1, t.y), math.lerp(l2, l3, t.y), t.z) * amplitude;

            amplitudeTotal += amplitude;
            p *= lacunarity;
            amplitude *= (1.0f / lacunarity);
        }

        r = r / amplitudeTotal;

        return r;
    }
}
