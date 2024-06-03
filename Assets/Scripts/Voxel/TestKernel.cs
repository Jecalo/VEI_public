using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;


[Serializable]
public class TestKernel
{
    public struct Line
    {
        public float3 a;
        public float3 b;

        public float ar;
        public float br;

        public Line(float3 a, float3 b, float ar, float br)
        {
            this.a = a;
            this.b = b;
            this.ar = ar;
            this.br = br;
        }
    }

    public struct FrontPoint
    {
        public float3 p;
        public float length;

        public FrontPoint(float3 p, float length)
        {
            this.p = p;
            this.length = length;
        }
    }


    [HideInInspector]
    public List<Line> lines;
    [HideInInspector]
    public List<FrontPoint> front;
    [HideInInspector]
    public Unity.Mathematics.Random rng;

    public bool IsComplete { get { return front != null && front.Count == 0 && lines.Count != 0; } }

    public uint currentSeed;

    public int startPoints = 12;
    public float mergeChance = 0.2f;
    public float3 targetCenter = new float3(0f, -0.5f, 0f);


    public void Initialize()
    {
        uint s = (uint)DateTime.Now.Ticks;
        Debug.Log($"Seed: {s}");
        Initialize(s);
    }

    public void Initialize(uint seed)
    {
        currentSeed = seed;
        rng = new Unity.Mathematics.Random(currentSeed);

        lines = new();
        front = new();

        for (int i = 0; i < startPoints; i++)
        {
            float3 p = rng.NextFloat3Direction();
            if (p.y < 0.0f) { p.y = -p.y; }

            //front.Add(new FrontPoint(p, startThickness));
            front.Add(new FrontPoint(p, 0f));
        }
    }

    static void GetFarthestPoint(List<FrontPoint> front, out float3 point, out int index)
    {
        if (front.Count == 0) { throw new Exception("No points in frontier"); }

        float3 p = front[0].p;
        int idx = 0;

        for (int i = 1; i < front.Count; i++)
        {
            if (math.distancesq(front[i].p, 0f) > math.distancesq(p, 0f)) { p = front[i].p; idx = i; }
        }

        point = p;
        index = idx;
    }

    static void GetClosestPoint(List<FrontPoint> front, int index, out int closestIndex)
    {
        if (front.Count <= 1) { throw new Exception("Not enough points in frontier"); }

        float3 p = 1000000f;
        int idx = -1;

        for (int i = 0; i < front.Count; i++)
        {
            if (i == index) { continue; }
            if (math.distancesq(front[i].p, front[index].p) < math.distancesq(p, front[index].p)) { p = front[i].p; idx = i; }
        }

        closestIndex = idx;
    }

    static void GetClosestForwardPoint(List<FrontPoint> front, float3 target, int index, out int closestIndex)
    {
        if (front.Count <= 1) { throw new Exception("Not enough points in frontier"); }

        float3 p = 1000000f;
        int idx = -1;

        for (int i = 0; i < front.Count; i++)
        {
            if (i == index) { continue; }
            if ((math.distancesq(front[i].p, front[index].p) < math.distancesq(p, front[index].p)) &&
                (math.distancesq(front[i].p, target) < math.distancesq(front[index].p, target)))
            {
                p = front[i].p; idx = i;
            }
        }

        closestIndex = idx;
    }

    public bool Advance()
    {
        if (front.Count == 0) { Debug.LogWarning("No points in frontier"); return false; }

        GetFarthestPoint(front, out float3 point, out int idx);

        if (/*rng.NextFloat() < mergeChance && */front.Count >= 2)
        {
            //Merge branch

            //GetClosestPoint(front, idx, out int closesIndex);
            GetClosestForwardPoint(front, targetCenter, idx, out int closesIndex);

            if (closesIndex == -1) { goto labelExtend; }
            if (math.distance(point, front[closesIndex].p) > 0.35f) { goto labelExtend; }

            lines.Add(new Line(point, front[closesIndex].p, front[idx].length, front[closesIndex].length));
            front.RemoveAt(idx);

            return true;
        }


        //Extend branch
        labelExtend:

        float3 dir = math.normalize(targetCenter - point);
        float3 newPoint = point + dir * rng.NextFloat(0.15f, 0.3f) + rng.NextFloat3Direction() * rng.NextFloat(0.15f);

        if (newPoint.y < targetCenter.y)
        {
            newPoint = targetCenter;
        }

        lines.Add(new Line(point, newPoint, front[idx].length, (math.distance(point, newPoint) + front[idx].length)));

        if (newPoint.y > targetCenter.y)
        {
            FrontPoint frontPoint = front[idx];
            frontPoint.p = newPoint;
            frontPoint.length += math.distance(point, newPoint);
            front[idx] = frontPoint;
        }
        else { front.RemoveAt(idx); }
        return true;
    }

    public void Complete()
    {
        while (Advance()) { }
    }

    public void DrawGizmos(float3 offset)
    {
        foreach (var l in lines)
        {
            Gizmo.DrawLine(l.a + offset, l.b + offset, Color.blue);
        }

        foreach (var p in front)
        {
            Gizmo.DrawSphere(p.p + offset, 0.1f, Color.cyan);
        }


        if (front.Count == 0) { return; }
        float3 point = front[0].p;

        for (int i = 1; i < front.Count; i++)
        {
            if (math.distancesq(front[i].p, 0f) > math.distancesq(point, 0f)) { point = front[i].p; }
        }

        Gizmo.DrawSphere(point + offset, 0.15f, Color.red);
    }
}
