using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

public static class UtilsAI
{
    public static int HeuristicManhattan(int3 a, int3 b)
    {
        return math.csum(math.abs(b - a));
    }

    public static int HeuristicGridSimple(int3 a, int3 b)
    {
        return (int)(math.distance(a, b) * 10f);
        //int3 delta = math.abs(a - b);

        //if (delta.x < delta.y)
        //{
        //    int tmp = delta.x;
        //    delta.x = delta.y;
        //    delta.y = tmp;
        //}
        //if (delta.x < delta.z)
        //{
        //    int tmp = delta.x;
        //    delta.x = delta.z;
        //    delta.z = tmp;
        //}
        //if (delta.y < delta.z)
        //{
        //    int tmp = delta.y;
        //    delta.y = delta.z;
        //    delta.z = tmp;
        //}

        //return 17 * delta.z + 14 * (delta.y - delta.z) + 10 * (delta.x - delta.y);
    }

    public static int CostGridSimple(int3 a, int3 b)
    {
        int delta = 0;

        if (a.x != b.x) { delta++; }
        if (a.y != b.y) { delta++; }
        if (a.z != b.z) { delta++; }

        if (delta == 1) { return 10; }
        else if (delta == 2) { return 14; }
        else { return 17; }
    }

    public static readonly int3[] OctileOffsets = {
            new int3(1, 0, 0), new int3(-1, 0, 0), new int3(0, 0, 1), new int3(0, 0, -1), new int3(0, 1, 0), new int3(0, -1, 0),
            new int3(1, 0, 1), new int3(1, 0, -1), new int3(-1, 0, 1), new int3(-1, 0, -1),
            new int3(1, 1, 0), new int3(-1, 1, 0), new int3(0, 1, 1), new int3(0, 1, -1), new int3(1, 1, 1), new int3(1, 1, -1), new int3(-1, 1, 1), new int3(-1, 1, -1),
            new int3(1, -1, 0), new int3(-1, -1, 0), new int3(0, -1, 1), new int3(0, -1, -1), new int3(1, -1, 1), new int3(1, -1, -1), new int3(-1, -1, 1), new int3(-1, -1, -1) };

    public static readonly int[] OctileCosts = {
            10, 10, 10, 10, 10, 10,
            14, 14, 14, 14,
            14, 14, 14, 14, 17, 17, 17, 17,
            14, 14, 14, 14, 17, 17, 17, 17};
}
