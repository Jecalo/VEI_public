using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.VisualScripting;

public static class MeshFinder
{
    public static Vector3[] FindPath(Mesh mesh, float4x4 trs, int start, int end, float maxSlope)
    {
        var dataArray = Mesh.AcquireReadOnlyMeshData(mesh);
        int vertCount = dataArray[0].vertexCount;
        int triCount = dataArray[0].GetSubMesh(0).indexCount / 3;

        if (dataArray[0].GetSubMesh(0).topology != MeshTopology.Triangles)
        {
            Debug.LogError("Mesh has non-triangle topology");
            dataArray.Dispose();
            return new Vector3[0];
        }
        if (start < 0 || start >= triCount || end < 0 || end >= triCount)
        {
            Debug.LogError("Invalid start/end points.");
            dataArray.Dispose();
            return new Vector3[0];
        }

        NativeArray<float3> verts = new NativeArray<float3>(vertCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        NativeArray<int3> tris = new NativeArray<int3>(triCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        dataArray[0].GetVertices(verts.Reinterpret<Vector3>());
        dataArray[0].GetIndices(tris.Reinterpret<int>(12), 0);
        dataArray.Dispose();


        var edges = new NativeHashMap<ulong, int2>(2 * triCount, Allocator.TempJob);

        for (int i = 0; i < triCount; i++)
        {
            ulong key;
            int3 tri_i = tris[i];

            key = GetEdgeKey(tri_i.x, tri_i.y);
            if (edges.ContainsKey(key)) { edges[key] = new int2(edges[key].x, i); }
            else { edges.Add(key, new int2(i, -1)); }

            key = GetEdgeKey(tri_i.y, tri_i.z);
            if (edges.ContainsKey(key)) { edges[key] = new int2(edges[key].x, i); }
            else { edges.Add(key, new int2(i, -1)); }

            key = GetEdgeKey(tri_i.x, tri_i.z);
            if (edges.ContainsKey(key)) { edges[key] = new int2(edges[key].x, i); }
            else { edges.Add(key, new int2(i, -1)); }
        }

        Job job = new();
        job.tris = tris;
        job.centers = new NativeArray<float3>(triCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        job.edges = edges;
        job.slope = new NativeArray<float>(triCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        job.maxSlope = maxSlope;
        job.start = start;
        job.end = end;
        job.openSet = new PriorityQueue(Allocator.TempJob);
        job.closedSet = new NativeHashMap<int, Node>(32, Allocator.TempJob);
        job.set = new NativeHashMap<int, Node>(32, Allocator.TempJob);
        job.finalPath = new NativeList<int>(Allocator.TempJob);

        for (int i = 0; i < triCount; i++)
        {
            int3 tri = tris[i];
            float3 a = verts[tri.x];
            float3 b = verts[tri.y];
            float3 c = verts[tri.z];

            float3 p = (a + b + c) / 3f;
            float3 n = math.normalize(math.cross(b - a, c - a));

            job.centers[i] = p;
            job.slope[i] = math.degrees(math.acos(math.dot(new float3(0f, 1f, 0f), n)));
        }

        System.Diagnostics.Stopwatch sw = new();
        sw.Start();
        job.Schedule().Complete();
        sw.Stop();

        Vector3[] path = new Vector3[job.finalPath.Length];

        for (int i = 0; i < path.Length; i++)
        {
            int3 tri = tris[job.finalPath[i]];
            float3 a = verts[tri.x];
            float3 b = verts[tri.y];
            float3 c = verts[tri.z];

            float3 p = (a + b + c) / 3f;
            float3 n = math.normalize(math.cross(b - a, c - a));

            path[i] = p + n * 0.15f;
        }

        job.tris.Dispose();
        verts.Dispose();
        job.edges.Dispose();
        job.centers.Dispose();
        job.slope.Dispose();
        job.openSet.Dispose();
        job.closedSet.Dispose();
        job.set.Dispose();
        job.finalPath.Dispose();

        Debug.LogFormat("Mesh A*: {0}ms", sw.Elapsed.TotalMilliseconds);

        return path;
    }

    private static ulong GetEdgeKey(int a, int b)
    {
        if (a > b) { return ((uint)b) | (((ulong)a) << 32); }
        else { return ((uint)a) | (((ulong)b) << 32); }
    }

    private static int Cost(float3 a, float3 b)
    {
        return (int)(math.distance(a, b) * 10f);
    }

    //Estimated cost to reach the target.
    private static int Heuristic(float3 n, float3 target)
    {
        return (int)(math.distance(n, target) * 10f);
    }

    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    private struct Job : IJob
    {
        [ReadOnly]
        public NativeArray<int3> tris;
        [ReadOnly]
        public NativeHashMap<ulong, int2> edges;
        [ReadOnly]
        public NativeArray<float3> centers;

        [ReadOnly]
        public NativeArray<float> slope;
        [ReadOnly]
        public float maxSlope;

        [ReadOnly]
        public int start, end;

        public PriorityQueue openSet;
        public NativeHashMap<int, Node> closedSet;
        public NativeHashMap<int, Node> set;

        [WriteOnly]
        public NativeList<int> finalPath;


        public void Execute()
        {
            if (start == end) { return; }

            float3 target = centers[end];

            set.Add(start, new Node(start, 0, 0));
            openSet.Push(0, start);

            while (openSet.Length != 0)
            {
                Node current = set[openSet.Pop()];

                //If we are at the end node, build the path and return.
                if (current.i == end)
                {
                    do
                    {
                        finalPath.Add(current.i);
                        current = set[current.previous];
                    } while (current.i != start);
                    finalPath.Add(start);
                    return;
                }

                int3 vi = tris[current.i];
                ulong edge = GetEdgeKey(vi.x, vi.y);
                int2 edgeTris = edges[edge];
                int ni = edgeTris.x != current.i ? edgeTris.x : edgeTris.y;
                if (!set.ContainsKey(ni) && slope[ni] <= maxSlope)
                {
                    Node neighbour = new Node(ni);
                    int cost = current.g + Cost(centers[current.i], centers[ni]);

                    if (cost < neighbour.g)
                    {
                        neighbour.previous = current.i;
                        neighbour.g = cost;
                        neighbour.h = Heuristic(centers[ni], target);
                        set[ni] = current;
                        openSet.Push(neighbour.t, ni);
                    }

                    set[ni] = neighbour;
                }

                edge = GetEdgeKey(vi.y, vi.z);
                edgeTris = edges[edge];
                ni = edgeTris.x != current.i ? edgeTris.x : edgeTris.y;
                if (!set.ContainsKey(ni) && slope[ni] <= maxSlope)
                {
                    Node neighbour = new Node(ni);
                    int cost = current.g + Cost(centers[current.i], centers[ni]);

                    if (cost < neighbour.g)
                    {
                        neighbour.previous = current.i;
                        neighbour.g = cost;
                        neighbour.h = Heuristic(centers[ni], target);
                        set[ni] = current;
                        openSet.Push(neighbour.t, ni);
                    }

                    set[ni] = neighbour;
                }

                edge = GetEdgeKey(vi.x, vi.z);
                edgeTris = edges[edge];
                ni = edgeTris.x != current.i ? edgeTris.x : edgeTris.y;
                if (!set.ContainsKey(ni) && slope[ni] <= maxSlope)
                {
                    Node neighbour = new Node(ni);
                    int cost = current.g + Cost(centers[current.i], centers[ni]);

                    if (cost < neighbour.g)
                    {
                        neighbour.previous = current.i;
                        neighbour.g = cost;
                        neighbour.h = Heuristic(centers[ni], target);
                        set[ni] = current;
                        openSet.Push(neighbour.t, ni);
                    }

                    set[ni] = neighbour;
                }
            }
        }
    }

    public struct Node
    {
        public int i;
        public int previous;

        public int g;   //Lowest cost found to reach this node from the start
        public int h;   //Estimated cost to reach the end node from this one
        public int t { get { return g + h; } }

        public Node(int i)
        {
            this.i = i;
            this.g = int.MaxValue / 2;
            this.h = int.MaxValue / 2;
            this.previous = default;
        }

        public Node(int i, int g, int h)
        {
            this.i = i;
            this.g = g;
            this.h = h;
            this.previous = default;
        }
    }
}