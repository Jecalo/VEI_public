using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using System;
using UnityEditor.Rendering;
using System.Security.Cryptography;

namespace VoxelTerrain
{
    public static class TreeKernel
    {
        public class Branch
        {
            public List<float3> mainPoints = new();
            public List<Tuple<float3, float3>> childBranches = new();

            public int deepestChildIndex = -1; //Index of the main point on this branch that has the deepest child (closest to the edge)

            public void AttachChild(Branch branch, int parentNode, int childNode)
            {
                if (parentNode >= mainPoints.Count) { throw new Exception("Parent node is out of bounds"); }
                if (childNode >= branch.mainPoints.Count) { throw new Exception("Child node is out of bounds"); }

                if (parentNode > deepestChildIndex) { deepestChildIndex = parentNode; }

                foreach (var child in branch.childBranches)
                {
                    childBranches.Add(child);
                }

                for (int i = 1; i <= childNode; i++)
                {
                    childBranches.Add(new Tuple<float3, float3>(branch.mainPoints[i-1], branch.mainPoints[i]));
                }
                childBranches.Add(new Tuple<float3, float3>(branch.mainPoints[childNode], mainPoints[parentNode]));
            }

            public void ClosestPoint(float3 p, ref int node, ref float distsqr)
            {
                node = -1;
                distsqr = float.MaxValue;

                for (int i = 0; i < mainPoints.Count; i++)
                {
                    float d = math.distancesq(p, mainPoints[i]);
                    if (d < distsqr) { node = i; distsqr = d; }
                }
            }
        }

        public static void TestGen(ref Unity.Mathematics.Random rng, float3 offset)
        {
            offset.y += 1.0f;
            List<Branch> branches = new();
            int splits = 4;
            int count = 30;

            for (int i = 0; i < count; i++)
            {
                float3 p = rng.NextFloat3Direction();
                //if (p.y < (rng.NextFloat() * -0.75f)) { continue; }

                branches.Add(new Branch());

                for (int j = 0; j < splits; j++)
                {
                    branches[branches.Count - 1].mainPoints.Add((float)j / splits * p);
                }
                branches[branches.Count - 1].mainPoints.Add(p);
            }

            for (int i = branches.Count - 1; i > 0; i--)
            {
                Branch branch = branches[i];
                branches.RemoveAt(i);

                int childIndex = (branch.deepestChildIndex == -1) ? 0 : branch.deepestChildIndex;

                float dist = float.MaxValue;
                int node = -1;
                int index = -1;
                float3 p = branch.mainPoints[childIndex];

                for (int j = 0; j < branches.Count; j++)
                {
                    float d = float.MaxValue;
                    int n = -1;
                    branches[j].ClosestPoint(p, ref n, ref d);

                    if (d < dist) { dist = d; node = n; index = j; }
                }

                branches[index].AttachChild(branch, node, childIndex + 1);
            }


            foreach (var branch in branches)
            {
                for(int i = 1; i < branch.mainPoints.Count; i++)
                {
                    DebugHelper.AddMarkerLine(branch.mainPoints[i - 1] + offset, branch.mainPoints[i] + offset);
                }
                foreach (var l in branch.childBranches)
                {
                    DebugHelper.AddMarkerLine(l.Item1 + offset, l.Item2 + offset);
                }
            }
        }

        private delegate void AddBranchMethod(ref Unity.Mathematics.Random rng, List<Tuple<float3, float3, float2>> lines);

        private struct LineSegment
        {
            public float3 p0;
            public float3 p1;
            public float r0;
            public float r1;

            public LineSegment(float3 p0, float3 p1, float r0, float r1)
            {
                this.p0 = p0;
                this.p1 = p1;
                this.r0 = r0;
                this.r1 = r1;
            }
        }

        private  static void TreeGen_v0(ref Unity.Mathematics.Random rng, List<LineSegment> lines,
            float heightMult, float voxelSize)
        {
            lines.Clear();

            float height = 16f * heightMult * rng.NextFloat(0.65f, 1.5f);
            float thickness = height / 16f + rng.NextFloat(0.2f);
            float thickness2 = thickness / 32f + 0.25f + rng.NextFloat(0.2f);

            int mainSplits = 4;

            AddMultiline(ref rng, lines, float3.zero, new float3(0f, 1f, 0f), height, thickness, thickness2, 35f, mainSplits, false);

            PropagateBranch(ref rng, lines, 0, mainSplits, 1f, 60f, voxelSize * 0.65f);

            //int totalBranches = rng.NextInt(5) + rng.NextInt(5);
            //int[] branches = new int[mainSplits - 1];
            //for (int i = 0; i < totalBranches; i++)
            //{
            //    branches[rng.NextInt(mainSplits - 1)]++;
            //}

            //for (int i = 0; i < (mainSplits - 1); i++)
            //{
            //    int branchCount = branches[i];
            //    for (int j = 0; j < branchCount; j++)
            //    {
            //        float a = rng.NextFloat() * math.PI * 2f;
            //        float3 dir = math.normalize(new float3(math.cos(a), rng.NextFloat(), math.sin(a)));
            //        float length = rng.NextFloat(3.5f, 10f);
            //        float r0 = rng.NextFloat(0.4f, 0.8f);
            //        float r1 = r0 / 2f;
            //        AddMultiline(ref rng, lines, lines[i].p1, dir, length, r0, r1, 35f, 3, true);
            //    }
            //}
        }

        private static void PropagateBranch(ref Unity.Mathematics.Random rng, List<LineSegment> lines, int start, int end,
            float splitMult, float maxAngle, float minRadius)
        {
            int splits = end - start - 1;

            if (splits <= 0) { return; }

            float parentLength = 0.0f;
            float halfAngle = math.radians(maxAngle) * 0.5f;


            for (int i = start; i < end; i++) { parentLength += math.distance(lines[i].p0, lines[i].p1); }

            for (int i = 0; i < splits; i++)
            {
                int branchCount = 0;

                if (rng.NextFloat() < 0.5f) { branchCount++; }
                if (rng.NextFloat() < 0.5f) { branchCount++; }

                Debug.Log(branchCount);

                float3 parentSplitDir = math.normalize(lines[i].p1 - lines[i].p0);

                for (int j = 0; j < branchCount; j++)
                {
                    float3 dir = BurstHelper.RandomAngle(ref rng, parentSplitDir, math.radians(30f), halfAngle);

                    float length = rng.NextFloat(0.5f, 1f) * (parentLength / (splits + 1) * (i + 1));
                    float r0 = rng.NextFloat(0.7f, 0.9f) * lines[i].r1;
                    float r1 = r0 / 2f;

                    r0 = math.max(minRadius, r0);
                    r1 = math.max(minRadius, r1);

                    AddMultiline(ref rng, lines, lines[i].p1, dir, length, r0, r1, maxAngle, splits, true);
                }
            }
        }


        private static void AddBranches(ref Unity.Mathematics.Random rng, List<LineSegment> lines,
            int startRange, int endRange, float chance, float r1, float r2, float length, int splits, float angle, bool twist)
        {
            for (int i = startRange; i <= endRange; i++)
            {
                while (rng.NextFloat() <= chance)
                {
                    float a = rng.NextFloat() * math.PI * 2f;
                    float3 dir = math.normalize(new float3(math.cos(a), rng.NextFloat(), math.sin(a)));
                    AddMultiline(ref rng, lines, lines[i].p1, dir, length, r1, r2, angle, splits, twist);
                    if (rng.NextFloat() < 0.5f)
                    {
                        int j = lines.Count - rng.NextInt(1, splits);
                        AddMultiline(ref rng, lines, lines[j].p1, math.normalize(lines[j].p1 - lines[j].p0), length * 2.0f / splits,
                            lines[j].r0, lines[j].r1, angle * 0.5f, 2, true);
                    }
                }
            }
        }

        private static void AddMultiline(ref Unity.Mathematics.Random rng, List<LineSegment> lines,
            float3 start, float3 dir, float length, float r1, float r2, float angle, int splits, bool twist)
        {
            float lineLength = length / splits;
            float radiusStep = (r2 - r1) / splits;
            float3 p = start;
            float r = r1;
            angle = math.radians(angle) * 0.5f;

            for (int i = 0; i < splits; i++)
            {
                float3 d = BurstHelper.RandomAngle(ref rng, dir, angle);
                float3 p2 = p + d * lineLength;
                if (twist) { dir = d; }
                lines.Add(new LineSegment(p, p2, r, r + radiusStep));
                r += radiusStep;
                p = p2;
            }
        }

        public static Kernel Tree(VoxelGrid grid, TestKernel k, float3 pos, byte woodMat, byte leavesMat)
        {
            System.Diagnostics.Stopwatch sw = new();
            sw.Start();

            pos = grid.WorldToLocal(pos);
            float voxelSize = grid.voxelSize;

            //===============================================================================================

            List<LineSegment> lines = new();
            Unity.Mathematics.Random rng = new((uint)DateTime.Now.Ticks);

            //TreeGen_v0(ref rng, lines, 1f, voxelSize);

            //for (int i = 0; i < lines.Count; i++)
            //{
            //    LineSegment l = lines[i];
            //    l.p0 += pos;
            //    l.p1 += pos;
            //    lines[i] = l;
            //    DebugHelper.AddMarkerLine(l.p0, l.p1);
            //}

            //TestGen(ref rng, pos);

            //if (!test2.CurrentKernel.IsComplete)
            //{
            //    Debug.LogWarning("Cannot generate a kernel from an empty tree");
            //    return new Kernel() { emptyKernel = true };
            //}
            k.Initialize();
            k.Complete();

            const float trunkHeight = 8f;
            foreach (var l in k.lines)
            {
                float3 a = l.a;
                float3 b = l.b;

                a = (a - k.targetCenter) * 10f + pos;
                b = (b - k.targetCenter) * 10f + pos;
                a.y += trunkHeight;
                b.y += trunkHeight;

                //float ar = (l.ar + 0.8f) * 0.3f;
                //float br = (l.br + 0.8f) * 0.3f;

                float ar = math.log10(l.ar + 1f) + 0.35f;
                float br = math.log10(l.ar + 1f) + 0.35f;

                lines.Add(new LineSegment(a, b, ar, br));
            }

            AddMultiline(ref rng, lines, new float3(0f, trunkHeight, 0f) + pos, new float3(0f, -1f, 0f), trunkHeight, 0.8f, 1.2f, 45f, 3, false);
            

            //===============================================================================================


            float3 min = lines[0].p0;
            float3 max = lines[0].p0;

            foreach(var l in lines)
            {
                min = math.min(min, l.p1);
                max = math.max(max, l.p1);
            }

            int3 minIndex = (int3)math.floor(min / voxelSize) - 2;
            int3 maxIndex = (int3)math.ceil(max / voxelSize) + 2;
            int3 size = maxIndex - minIndex;
            int iterationCount = size.x * size.y * size.z;

            Kernel kernel = new();
            kernel.config = new KernelConfig(KernelMode.Add);
            kernel.data = new NativeArray<float>(iterationCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            kernel.matData = new NativeArray<byte>(1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            kernel.matData[0] = woodMat;
            kernel.size = size;
            kernel.indexOffset = minIndex;
            kernel.complexMaterial = false;
            kernel.emptyKernel = false;            

            KernelTreeJob job = new();
            job.data = kernel.data;
            job.matData = kernel.matData;
            job.lines = new NativeArray<LineSegment>(lines.Count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            job.voxelSize = voxelSize;
            job.sizeZ = size.z;
            job.stepYZ = size.y * size.z;
            job.offset = minIndex;
            job.min = grid.minClamp;
            job.max = grid.maxClamp;
            job.whiteNoiseRange = 0.1f;
            job.woodMat = woodMat;
            job.rng = rng;

            for(int i = 0; i < lines.Count; i++)
            {
                job.lines[i] = lines[i];
            }

            job.ScheduleParallel(iterationCount, 32, default).Complete();

            job.lines.Dispose();

            sw.Stop();
            DebugHelper.AddMsg(string.Format("Kernel build (tree): {0}ms", sw.Elapsed.TotalMilliseconds));

            return kernel;
        }

        [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
        private struct KernelTreeJob : IJobFor
        {
            [WriteOnly]
            public NativeArray<float> data;
            [WriteOnly]
            public NativeArray<byte> matData;
            [ReadOnly]
            public NativeArray<LineSegment> lines;
            [ReadOnly]
            public float whiteNoiseRange;
            [ReadOnly]
            public byte woodMat;

            [ReadOnly]
            public float voxelSize;
            [ReadOnly]
            public int sizeZ;
            [ReadOnly]
            public int stepYZ;
            
            [ReadOnly]
            public int3 offset;
            [ReadOnly]
            public float min, max;

            public Unity.Mathematics.Random rng;

            public void Execute(int index)
            {
                int3 i;
                i.x = index / stepYZ + offset.x;
                i.y = (index % stepYZ) / sizeZ + offset.y;
                i.z = index % sizeZ + offset.z;

                float3 p = (float3)i * voxelSize;
                float r = float.MaxValue;

                int c = lines.Length;
                for (int j = 0; j < c; j++)
                {
                    float3 a = lines[j].p0;
                    float3 b = lines[j].p1;
                    float r0 = lines[j].r0;
                    float r1 = lines[j].r1;

                    float3 ba = b - a;
                    float l2 = math.dot(ba, ba);
                    float rr = r0 - r1;
                    float a2 = l2 - rr * rr;
                    float il2 = 1.0f / l2;

                    float3 pa = p - a;
                    float y = math.dot(pa, ba);
                    float z = y - l2;
                    float x2 = math.dot(pa * l2 - ba * y, pa * l2 - ba * y);
                    float y2 = y * y * l2;
                    float z2 = z * z * l2;

                    float k = math.sign(rr) * rr * rr * x2;
                    float tmp;
                    if (math.sign(z) * a2 * z2 > k) { tmp = math.sqrt(x2 + z2) * il2 - r1; }
                    else if (math.sign(y) * a2 * y2 < k) { tmp = math.sqrt(x2 + y2) * il2 - r0; }
                    else { tmp = (math.sqrt(x2 * a2 * il2) + y * rr) * il2 - r0; }

                    r = math.min(r, tmp);
                }

                data[index] = math.clamp(r /*+ (rng.NextFloat() * 2f - 1f) * whiteNoiseRange*/, min, max);
                //matData[index] = woodMat;
            }
        }
    }
}
