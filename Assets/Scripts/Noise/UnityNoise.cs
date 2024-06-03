using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using System.Runtime.CompilerServices;

public static class UnityNoise
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Perlin2D(uint seed, float2 p, float frequency, int octaves)
    {
        float r = 0.0f;
        float amplitude = 1.0f;
        float amplitudeTotal = 0.0f;

        p = (p) * frequency;

        for (int oct = 0; oct < octaves; oct++)
        {
            r += noise.cnoise(p);
            amplitudeTotal += amplitude;
            p *= 2.0f;
            amplitude *= 0.5f;
        }

        r = r / amplitudeTotal;
        return r;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Perlin3D(uint seed, float3 p, float frequency, int octaves)
    {
        float r = 0.0f;
        float amplitude = 1.0f;
        float amplitudeTotal = 0.0f;

        p = (p) * frequency;

        for (int oct = 0; oct < octaves; oct++)
        {
            r += noise.cnoise(p);
            amplitudeTotal += amplitude;
            p *= 2.0f;
            amplitude *= 0.5f;
        }

        r = r / amplitudeTotal;
        return r;
    }
}
