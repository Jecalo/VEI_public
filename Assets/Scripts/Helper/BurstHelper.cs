using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Profiling;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

public static class BurstHelper
{
    public static void BoxBounds(in float4x4 trs, in float3 extents, out float3 min, out float3 max)
    {
        float3 _min = float.MaxValue, _max = float.MinValue;
        float3 p;
        float4x4 trs_s = math.mul(trs, float4x4.Scale(extents));

        p = new float3(-0.5f, -0.5f, -0.5f);
        p = math.transform(trs_s, p);
        _min = math.min(_min, p);
        _max = math.max(_max, p);
        p = new float3(0.5f, -0.5f, -0.5f);
        p = math.transform(trs_s, p);
        _min = math.min(_min, p);
        _max = math.max(_max, p);
        p = new float3(-0.5f, 0.5f, -0.5f);
        p = math.transform(trs_s, p);
        _min = math.min(_min, p);
        _max = math.max(_max, p);
        p = new float3(-0.5f, -0.5f, 0.5f);
        p = math.transform(trs_s, p);
        _min = math.min(_min, p);
        _max = math.max(_max, p);
        p = new float3(0.5f, 0.5f, -0.5f);
        p = math.transform(trs_s, p);
        _min = math.min(_min, p);
        _max = math.max(_max, p);
        p = new float3(-0.5f, 0.5f, 0.5f);
        p = math.transform(trs_s, p);
        _min = math.min(_min, p);
        _max = math.max(_max, p);
        p = new float3(0.5f, -0.5f, 0.5f);
        p = math.transform(trs_s, p);
        _min = math.min(_min, p);
        _max = math.max(_max, p);
        p = new float3(0.5f, 0.5f, 0.5f);
        p = math.transform(trs_s, p);
        _min = math.min(_min, p);
        _max = math.max(_max, p);

        min = _min;
        max = _max;
    }

    public static bool IsContained(in int3 x, in int3 min, in int3 max)
    {
        return math.all((min <= x) & (x <= max));
    }

    public static void ClearNativeArray<T>(NativeArray<T> array) where T : unmanaged
    {
        if (!array.IsCreated || array.Length == 0) { return; }
        unsafe
        {
            void* p = array.GetUnsafePtr();
            if (p == null) { return; }
            UnsafeUtility.MemClear(p, array.Length * sizeof(T));
        }
    }

    public static void SetNativeArray<T>(NativeArray<T> array, T value) where T : unmanaged
    {
        if (!array.IsCreated || array.Length == 0) { return; }
        array[0] = value;
        if (array.Length == 1) { return; }
        unsafe
        {
            T* p = (T*)array.GetUnsafePtr();
            if (p == null) { return; }
            UnsafeUtility.MemCpyReplicate(p + 1, p, sizeof(T), array.Length - 1);
        }
    }

    //Random direction, pointing away up to the angle given
    public static float3 RandomAngle(ref Unity.Mathematics.Random rng, float3 dir, float angleRads)
    {
        float3 axis = math.cross(dir, rng.NextFloat3Direction());
        quaternion q = quaternion.AxisAngle(axis, rng.NextFloat(-angleRads, angleRads));
        return math.mul(q, dir);
    }

    //Random direction, pointing away between minAngle and maxAngle
    public static float3 RandomAngle(ref Unity.Mathematics.Random rng, float3 dir, float minAngleRads, float maxAngleRads)
    {
        float3 axis = math.cross(dir, rng.NextFloat3Direction());
        float angle = rng.NextFloat(minAngleRads, maxAngleRads);
        angle = rng.NextBool() ? angle : -angle;
        quaternion q = quaternion.AxisAngle(axis, angle);
        return math.mul(q, dir);
    }

    public static float[] BlueNoiseUnitFloats(ref Unity.Mathematics.Random rng, int count, float minDistance)
    {
        float[] r = new float[count];



        return r;
    }


    [StructLayout(LayoutKind.Explicit)]
    private struct UlongBooleansUnion
    {
        [FieldOffset(0)]
        public ulong ulongValue;

        [FieldOffset(0)]
        public bool booleanValue0;
        [FieldOffset(1)]
        public bool booleanValue1;
        [FieldOffset(2)]
        public bool booleanValue2;
        [FieldOffset(3)]
        public bool booleanValue3;
        [FieldOffset(4)]
        public bool booleanValue4;
        [FieldOffset(5)]
        public bool booleanValue5;
        [FieldOffset(6)]
        public bool booleanValue6;
        [FieldOffset(7)]
        public bool booleanValue7;

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong BooleansToUlong(bool b0, bool b1, bool b2, bool b3, bool b4, bool b5, bool b6, bool b7)
    {
        UlongBooleansUnion union;
        union.ulongValue = 0;

        union.booleanValue0 = b0;
        union.booleanValue1 = b1;
        union.booleanValue2 = b2;
        union.booleanValue3 = b3;
        union.booleanValue4 = b4;
        union.booleanValue5 = b5;
        union.booleanValue6 = b6;
        union.booleanValue7 = b7;

        return union.ulongValue;
    }
}
