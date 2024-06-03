using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using System.Linq;
using UnityEngine.Assertions;
using Unity.VisualScripting;

public static class SimplePathfinder2
{
    public static int3[] GetPath(NativeArray<float> data, int res, int3 start, int3 end)
    {
        System.Diagnostics.Stopwatch sw = new();

        Job job = new Job();
        job.data = data;
        job.res = res;
        job.start = start;
        job.end = end;
        job.openSet = new PriorityQueue(Allocator.TempJob, 2048, res * res * res);
        job.set = new NativeArray<Node>(res * res * res, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        job.finalPath = new NativeList<int3>(Allocator.TempJob);
        job.log = new NativeArray<int>(4, Allocator.TempJob, NativeArrayOptions.ClearMemory);

        for(int i = 0; i < (res * res * res); i++)
        {
            int resStep = res * res;
            int x = i / resStep;
            int y = (i % resStep) / res;
            int z = i % res;
            job.set[i] = new Node(new int3(x, y, z));
        }

        sw.Start();
        job.Schedule().Complete();
        sw.Stop();

        int3[] path = job.finalPath.ToArray();

        float d = 0f;
        if (path.Length != 0)
        {
            for (int i = 1; i < path.Length; i++)
            {
                d += Vector3.Distance((float3)path[i - 1], (float3)path[i]);
            }
        }

        Debug.LogFormat("Pathfinding: {0}ms -> {1}m ({2} itrs, {3} max heap, {4} ending openset, {5} total nodes visited)",
            sw.Elapsed.TotalMilliseconds, d, job.log[0], job.log[1], job.log[2], job.log[3]);

        var grid = VoxelManager.GetFirstGrid();
        foreach (var i in job.set)
        {
            switch (i.state)
            {
                case Node.State.None:
                    continue;
                case Node.State.Closed:
                    Gizmo.DrawSphere(grid.IndexToWorld(i.i), 0.1f, Color.yellow);
                    break;
                case Node.State.Open:
                    Gizmo.DrawSphere(grid.IndexToWorld(i.i), 0.1f, Color.cyan);
                    break;
            }
        }

        job.openSet.Dispose();
        job.set.Dispose();
        job.finalPath.Dispose();
        job.log.Dispose();

        return path;
    }


    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    private struct Job : IJob
    {
        [ReadOnly]
        public NativeArray<float> data;

        [ReadOnly]
        public int res;
        [ReadOnly]
        public int3 start, end;

        public PriorityQueue openSet;
        public NativeArray<Node> set;

        [WriteOnly]
        public NativeList<int3> finalPath;

        public NativeArray<int> log;

        [ReadOnly]
        private static readonly int3[] offsets = { new int3(1, 0, 0), new int3(-1, 0, 0), new int3(0, 1, 0), new int3(0, -1, 0), new int3(0, 0, 1), new int3(0, 0, -1) };


        public void Execute()
        {
            if (math.all(start == end)) { return; }

            int step = res * res;
            set[start.x * step + start.y * res + start.z] = new Node(start, 0, 0);
            openSet.Push(0, start.x * step + start.y * res + start.z);

            while (openSet.Length != 0)
            {
                log[1] = math.max(log[1], openSet.Length);

                int currentIndex = openSet.Pop();
                Node current = set[currentIndex];

                log[0] = log[0] + 1;
                current.state = Node.State.Closed;
                set[currentIndex] = current;

                if (math.all(current.i == end))
                {
                    do
                    {
                        finalPath.Add(current.i);
                        current = set[current.previous];
                    } while (!math.all(current.i == start));
                    finalPath.Add(start);
                    log[2] = openSet.Length;
                    return;
                }

                for (int j = 0; j < 6; j++)
                {
                    int3 i = current.i + offsets[j];
                    int indexNbr = i.x * step + i.y * res + i.z;

                    if (math.any(i < 0) || math.any(i >= res)) { continue; }    //Don't go outside the bounds
                    if (data[indexNbr] < 0f) { continue; }                      //Skip impassable cells

                    Node neighbour = set[indexNbr];
                    int cost = current.g + 10;

                    if (neighbour.state == Node.State.None)
                    {
                        neighbour.state = Node.State.Open;
                        neighbour.previous = currentIndex;
                        neighbour.g = cost;
                        neighbour.h = UtilsAI.HeuristicGridSimple(i, end);
                        neighbour.previousMove = j;
                        openSet.Push(neighbour.t, indexNbr);

                        set[indexNbr] = neighbour;
                        log[3] = log[3] + 1;
                    }
                    else if (neighbour.state == Node.State.Open && cost < neighbour.g)
                    {
                        neighbour.state = Node.State.Open;
                        neighbour.previous = currentIndex;
                        neighbour.g = cost;
                        neighbour.h = UtilsAI.HeuristicGridSimple(i, end);
                        neighbour.previousMove = j;
                        openSet.Change(neighbour.t, indexNbr);

                        set[indexNbr] = neighbour;
                    }
                    //else if (neighbour.state == Node.State.Closed && cost < neighbour.g)
                    //{
                    //    //Gizmo.DrawSphere((float3)neighbour.i * 1.03225f, 0.2f, Color.black);
                    //    neighbour.state = Node.State.Open;
                    //    neighbour.previous = currentIndex;
                    //    neighbour.g = cost;
                    //    neighbour.h = math.csum(math.abs(i - end));
                    //    openSet.Push(neighbour.t, indexNbr);

                    //    set[indexNbr] = neighbour;
                    //}
                }
            }
        }
    }


    public struct Node
    {
        public enum State : int { None, Open, Closed }

        public int3 i;
        public int g, h;
        public int t { get { return g + h; } }

        public int previous;
        public int previousMove;

        public State state; 

        public Node(int3 i)
        {
            this.i = i;
            this.g = int.MaxValue;
            this.h = int.MaxValue;
            this.previous = int.MaxValue;
            state = State.None;
            previousMove = -1;
        }

        public Node(int3 i, int g, int h)
        {
            this.i = i;
            this.g = g;
            this.h = h;
            this.previous = int.MaxValue;
            this.state = State.None;
            previousMove = -1;
        }
    }
}
