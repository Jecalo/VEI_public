using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

public static class Perlin3Dfbm_s
{
    private static readonly int primeX = 501125321;
    private static readonly int primeY = 1136930381;
    private static readonly int primeZ = 1720413743;
    //private static readonly int primeW = 1066037191;

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
        job.seed = math.asint(seed);
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

    private static float4 GradientDot(int4 hash, float4 x, float4 y, float4 z)
    {
        int4 hasha13 = hash & 13;

        //if h < 8 then x, else y
        float4 u = math.select(x, y, hasha13 < 8);

        //if h < 4 then y else if h is 12 or 14 then x else z
        float4 v = math.select(x, z, hasha13 == 12);
        v = math.select(y, v, hasha13 < 2);

        //if h1 then -u else u
        //if h2 then -v else v
        float4 h1 = (hash << 31);
        float4 h2 = ((hash & 2) << 30);
        //then add them
        //return (u ^ h1) + (v ^ h2);
        return math.select(-u, u, (hash & (1 << 31)) == 0) + math.select(-v, v, (hash & (1 << 30)) == 0);
    }

    private static int4 HashPrimes(int4 seed, int4 x, int4 y, int4 z)
    {
        int4 hash = seed;
        hash ^= (x ^ y ^ z);
        hash *= 0x27d4eb2d;
        return (hash >> 15) ^ hash;
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
        public int4 seed;
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
                float4 xs = math.floor(pos.c0);
                float4 ys = math.floor(pos.c1);
                float4 zs = math.floor(pos.c2);

                int4 x0 = (int4)xs * primeX;
                int4 y0 = (int4)ys * primeY;
                int4 z0 = (int4)zs * primeZ;
                int4 x1 = x0 + primeX;
                int4 y1 = y0 + primeY;
                int4 z1 = z0 + primeZ;

                float4 xf0 = xs = pos.c0 - xs;
                float4 yf0 = ys = pos.c1 - ys;
                float4 zf0 = zs = pos.c2 - zs;
                float4 xf1 = xf0 - 1f;
                float4 yf1 = yf0 - 1f;
                float4 zf1 = zf0 - 1f;

                xs = NoiseHelper.Smooth4(xs);
                ys = NoiseHelper.Smooth4(ys);
                zs = NoiseHelper.Smooth4(zs);

                float4 p0 = GradientDot(HashPrimes(seed, x0, y0, z0), xf0, yf0, zf0);
                float4 p1 = GradientDot(HashPrimes(seed, x0, y1, z0), xf0, yf1, zf0);
                float4 p2 = GradientDot(HashPrimes(seed, x1, y1, z0), xf1, yf1, zf0);
                float4 p3 = GradientDot(HashPrimes(seed, x1, y0, z0), xf1, yf0, zf0);
                float4 p4 = GradientDot(HashPrimes(seed, x0, y0, z1), xf0, yf0, zf1);
                float4 p5 = GradientDot(HashPrimes(seed, x0, y1, z1), xf0, yf1, zf1);
                float4 p6 = GradientDot(HashPrimes(seed, x1, y1, z1), xf1, yf1, zf1);
                float4 p7 = GradientDot(HashPrimes(seed, x1, y0, z1), xf1, yf0, zf1);

                float4 l0 = math.lerp(p0, p3, xs);
                float4 l1 = math.lerp(p1, p2, xs);
                float4 l2 = math.lerp(p4, p7, xs);
                float4 l3 = math.lerp(p5, p6, xs);

                r += math.lerp(math.lerp(l0, l1, ys), math.lerp(l2, l3, ys), zs) * amplitude * 0.964921414852142333984375f;

                amplitudeTotal += amplitude;
                pos *= lacunarity;
                amplitude *= (1.0f / lacunarity);
            }

            r = r / amplitudeTotal;
            noise[i] = r;
        }
    }
}
