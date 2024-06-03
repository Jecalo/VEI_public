using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using System.Runtime.CompilerServices;

public static class NoiseHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Smooth(float t)
    {
        return t * t * t * (t * (t * 6.0f - 15.0f) + 10.0f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float2 Smooth2(float2 t)
    {
        return t * t * t * (t * (t * 6.0f - 15.0f) + 10.0f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float3 Smooth3(float3 t)
    {
        return t * t * t * (t * (t * 6.0f - 15.0f) + 10.0f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float4 Smooth4(float4 t)
    {
        return t * t * t * (t * (t * 6.0f - 15.0f) + 10.0f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float4x3 TRS_vector(float3x4 trs, float4x3 v)
    {
        return new float4x3(
            trs.c0.x * v.c0 + trs.c1.x * v.c1 + trs.c2.x * v.c2 + trs.c3.x,
            trs.c0.y * v.c0 + trs.c1.y * v.c1 + trs.c2.y * v.c2 + trs.c3.y,
            trs.c0.z * v.c0 + trs.c1.z * v.c1 + trs.c2.z * v.c2 + trs.c3.z
            );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float4x4 BuildTRS(float3 t, float3 r, float3 s)
    {
        return float4x4.TRS(t, quaternion.EulerZXY(math.radians(r)), s);
    }
}
