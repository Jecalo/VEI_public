using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using System.Runtime.CompilerServices;

public static class Perlin3Dfbm_org
{
    public static readonly byte[] permutation = {
            151, 160, 137, 91, 90, 15, 131, 13, 201, 95, 96, 53, 194, 233, 7, 225, 140, 36,
            103, 30, 69, 142, 8, 99, 37, 240, 21, 10, 23, 190, 6, 148, 247, 120, 234, 75, 0,
            26, 197, 62, 94, 252, 219, 203, 117, 35, 11, 32, 57, 177, 33, 88, 237, 149, 56,
            87, 174, 20, 125, 136, 171, 168, 68, 175, 74, 165, 71, 134, 139, 48, 27, 166,
            77, 146, 158, 231, 83, 111, 229, 122, 60, 211, 133, 230, 220, 105, 92, 41, 55,
            46, 245, 40, 244, 102, 143, 54, 65, 25, 63, 161, 1, 216, 80, 73, 209, 76, 132,
            187, 208, 89, 18, 169, 200, 196, 135, 130, 116, 188, 159, 86, 164, 100, 109,
            198, 173, 186, 3, 64, 52, 217, 226, 250, 124, 123, 5, 202, 38, 147, 118, 126,
            255, 82, 85, 212, 207, 206, 59, 227, 47, 16, 58, 17, 182, 189, 28, 42, 223, 183,
            170, 213, 119, 248, 152, 2, 44, 154, 163, 70, 221, 153, 101, 155, 167, 43,
            172, 9, 129, 22, 39, 253, 19, 98, 108, 110, 79, 113, 224, 232, 178, 185, 112,
            104, 218, 246, 97, 228, 251, 34, 242, 193, 238, 210, 144, 12, 191, 179, 162,
            241, 81, 51, 145, 235, 249, 14, 239, 107, 49, 192, 214, 31, 181, 199, 106,
            157, 184, 84, 204, 176, 115, 121, 50, 45, 127, 4, 150, 254, 138, 236, 205,
            93, 222, 114, 67, 29, 24, 72, 243, 141, 128, 195, 78, 66, 215, 61, 156, 180,

            151, 160, 137, 91, 90, 15, 131, 13, 201, 95, 96, 53, 194, 233, 7, 225, 140, 36,
            103, 30, 69, 142, 8, 99, 37, 240, 21, 10, 23, 190, 6, 148, 247, 120, 234, 75, 0,
            26, 197, 62, 94, 252, 219, 203, 117, 35, 11, 32, 57, 177, 33, 88, 237, 149, 56,
            87, 174, 20, 125, 136, 171, 168, 68, 175, 74, 165, 71, 134, 139, 48, 27, 166,
            77, 146, 158, 231, 83, 111, 229, 122, 60, 211, 133, 230, 220, 105, 92, 41, 55,
            46, 245, 40, 244, 102, 143, 54, 65, 25, 63, 161, 1, 216, 80, 73, 209, 76, 132,
            187, 208, 89, 18, 169, 200, 196, 135, 130, 116, 188, 159, 86, 164, 100, 109,
            198, 173, 186, 3, 64, 52, 217, 226, 250, 124, 123, 5, 202, 38, 147, 118, 126,
            255, 82, 85, 212, 207, 206, 59, 227, 47, 16, 58, 17, 182, 189, 28, 42, 223, 183,
            170, 213, 119, 248, 152, 2, 44, 154, 163, 70, 221, 153, 101, 155, 167, 43,
            172, 9, 129, 22, 39, 253, 19, 98, 108, 110, 79, 113, 224, 232, 178, 185, 112,
            104, 218, 246, 97, 228, 251, 34, 242, 193, 238, 210, 144, 12, 191, 179, 162,
            241, 81, 51, 145, 235, 249, 14, 239, 107, 49, 192, 214, 31, 181, 199, 106,
            157, 184, 84, 204, 176, 115, 121, 50, 45, 127, 4, 150, 254, 138, 236, 205,
            93, 222, 114, 67, 29, 24, 72, 243, 141, 128, 195, 78, 66, 215, 61, 156, 180
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Calculate(int hash, float x, float y, float z)
    {
        int h = hash & 15;
        float u = (h < 8) ? x : y;
        float v = (h < 4) ? y : ((h == 12) || (h == 14)) ? x : z;
        return (((h & 1) == 0) ? u : -u) + (((h & 2) == 0) ? v : -v);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float4 Calculate(int4 hash, float4 x, float4 y, float4 z)
    {
        int4 h = hash & 15;
        float4 u = math.select(x, y, (h < 8));
        float4 v = math.select(y, math.select(x, z, (h == 12) | (h == 14)), (h < 8));
        return math.select(u, -u, (h & 1) == 0) + math.select(v, -v, (h & 2) == 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int4 Sample(int4 i)
    {
        return new int4(permutation[i.x], permutation[i.y], permutation[i.z], permutation[i.w]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Smooth(float t)
    {
        return t * t * t * (t * (t * 6.0f - 15.0f) + 10.0f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Generate(float3 pos, float frequency = 1.0f, int octaves = 1)
    {
        float amplitude = 1.0f;
        float amplitudeTotal = 0.0f;
        float value = 0.0f;
        pos *= frequency;

        for(int i = 0; i < octaves; i++)
        {
            int u = ((int)math.floor(pos.x)) & 255;
            int v = ((int)math.floor(pos.y)) & 255;
            int w = ((int)math.floor(pos.z)) & 255;

            float tx = pos.x - math.floor(pos.x);
            float ty = pos.x - math.floor(pos.x);
            float tz = pos.x - math.floor(pos.x);

            int A = permutation[u] + v;
            int AA = permutation[A] + w;
            int AB = permutation[A + 1] + w;
            int B = permutation[u + 1] + v;
            int BA = permutation[B] + w;
            int BB = permutation[B + 1] + w;

            float p0 = Calculate(permutation[AA], tx, ty, tz);
            float p1 = Calculate(permutation[BA], tx - 1.0f, ty, tz);
            float p2 = Calculate(permutation[AB], tx, ty - 1.0f, tz);
            float p3 = Calculate(permutation[BB], tx - 1.0f, ty - 1.0f, tz);
            float p4 = Calculate(permutation[AA + 1], tx, ty, tz - 1.0f);
            float p5 = Calculate(permutation[BA + 1], tx - 1.0f, ty, tz - 1.0f);
            float p6 = Calculate(permutation[AB + 1], tx, ty - 1.0f, tz - 1.0f);
            float p7 = Calculate(permutation[BB + 1], tx - 1.0f, ty - 1.0f, tz - 1.0f);

            tx = Smooth(tx);
            ty = Smooth(ty);
            tz = Smooth(tz);

            float l1 = math.lerp(p0, p1, tx);
            float l2 = math.lerp(p2, p3, tx);
            float l3 = math.lerp(p4, p5, tx);
            float l4 = math.lerp(p6, p7, tx);

            value += math.lerp(math.lerp(l1, l2, ty), math.lerp(l3, l4, ty), tz) * amplitude;

            amplitudeTotal += amplitude;
            pos *= 2.0f;
            amplitude *= 0.5f;
        }
        value /= amplitudeTotal;
        return value;
    }

    public static JobForWrapper<Job> GenerateJob(NativeArray<float> data, int resolution, int3 offset, float4x4 trs, uint seed, float frequency, int octaves, float lacunarity, int tiling = -1)
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
        job.tiling = tiling;

        return new JobForWrapper<Job>(job, job.noise.Length, vectorResolution);
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
        [ReadOnly]
        public int4 tiling;

        public void Execute(int i)
        {
            float4x3 pos;
            float4 r = new float4(0.0f, 0.0f, 0.0f, 0.0f);
            float amplitude = 1.0f;
            float amplitudeTotal = 0.0f;

            int4 iv = i * 4 + new int4(0, 1, 2, 3);
            pos.c0 = (float4)((iv / step) + offset.x) * invResolution;
            pos.c1 = (float4)(((iv % step) / resolution) + offset.y) * invResolution;
            pos.c2 = (float4)((iv % resolution) + offset.z) * invResolution;

            pos = NoiseHelper.TRS_vector(trs, pos * frequency);

            for (int oct = 0; oct < octaves; oct++)
            {
                int4 u = (int4)math.floor(pos.c0) & 255;
                int4 v = (int4)math.floor(pos.c1) & 255;
                int4 w = (int4)math.floor(pos.c2) & 255;

                float4 tx = pos.c0 - math.floor(pos.c0);
                float4 ty = pos.c1 - math.floor(pos.c1);
                float4 tz = pos.c2 - math.floor(pos.c2);

                int4 A  = Sample(u) + v;
                int4 AA = Sample(A) + w;
                int4 AB = Sample(A + 1) + w;
                int4 B  = Sample(u + 1) + v;
                int4 BA = Sample(B) + w;
                int4 BB = Sample(B + 1) + w;

                float4 p0 = Calculate(Sample(AA), tx, ty, tz);
                float4 p1 = Calculate(Sample(BA), tx - 1.0f, ty, tz);
                float4 p2 = Calculate(Sample(AB), tx, ty - 1.0f, tz);
                float4 p3 = Calculate(Sample(BB), tx - 1.0f, ty - 1.0f, tz);
                float4 p4 = Calculate(Sample(AA + 1), tx, ty, tz - 1.0f);
                float4 p5 = Calculate(Sample(BA + 1), tx - 1.0f, ty, tz - 1.0f);
                float4 p6 = Calculate(Sample(AB + 1), tx, ty - 1.0f, tz - 1.0f);
                float4 p7 = Calculate(Sample(BB + 1), tx - 1.0f, ty - 1.0f, tz - 1.0f);


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
}
