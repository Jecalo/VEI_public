using System;
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



//Based on the paper: Generating Signed Distance Fields From Triangle Meshes [Andreas Bærentzen, Henrik Aanæs, 2002]

namespace VoxelTerrain
{
    public static class MeshSDF
    {
        //Full data structure necessary for generating an sdf.
        //Contains a BVH tree, and weighted normals.
        public struct MeshSDFData
        {
            public NativeArray<float3> verts;
            public NativeArray<int3> tris;

            public NativeArray<Node> tree;
            public NativeArray<float3> vertWeightedNormal;
            public NativeHashMap<ulong, float3> edgeWeightedNormal;

            public void Dispose()
            {
                verts.Dispose();
                tris.Dispose();
                tree.Dispose();
                vertWeightedNormal.Dispose();
                edgeWeightedNormal.Dispose();
            }
        }

        public static MeshSDFData BakeMeshSDFData(Mesh mesh, Allocator allocator)
        {
            if (allocator != Allocator.TempJob && allocator != Allocator.Persistent) { Debug.LogError("Invalid allocator type"); return new MeshSDFData(); }
            MeshSDFData sdf = new MeshSDFData();

            var dataArray = Mesh.AcquireReadOnlyMeshData(mesh);
            int vertCount = dataArray[0].vertexCount;
            int triCount = dataArray[0].GetSubMesh(0).indexCount / 3;

            if (dataArray[0].GetSubMesh(0).topology != MeshTopology.Triangles)
            {
                Debug.LogError("Mesh has non-triangle topology");
                dataArray.Dispose();
                return new MeshSDFData();
            }

            sdf.verts = new NativeArray<float3>(vertCount, allocator, NativeArrayOptions.UninitializedMemory);
            sdf.tris = new NativeArray<int3>(triCount, allocator, NativeArrayOptions.UninitializedMemory);

            dataArray[0].GetVertices(sdf.verts.Reinterpret<Vector3>());
            dataArray[0].GetIndices(sdf.tris.Reinterpret<int>(12), 0);

            sdf.tree = new NativeArray<Node>(triCount * 2, allocator, NativeArrayOptions.UninitializedMemory);
            sdf.vertWeightedNormal = new NativeArray<float3>(vertCount, allocator, NativeArrayOptions.ClearMemory);
            sdf.edgeWeightedNormal = new NativeHashMap<ulong, float3>(2 * triCount, allocator);

            Job_GenWeighterNormals job_n = new Job_GenWeighterNormals()
            {
                verts = sdf.verts,
                tris = sdf.tris,
                vertWeightedNormal = sdf.vertWeightedNormal,
                edgeWeightedNormal = sdf.edgeWeightedNormal
            };
            job_n.Schedule().Complete();

            ConstructTree(sdf.tree, sdf.verts, sdf.tris);

            dataArray.Dispose();
            return sdf;
        }

        public static void GenSDF(MeshSDFData meshData, NativeArray<float> data, float4x4 trs, int3 indexStart, float voxelSize, int3 size, float min, float max)
        {
            if (data.Length != (size.x * size.y * size.z)) { Debug.LogError("Wrong data size"); return; }

            Job_GenSDF job = new Job_GenSDF();
            job.meshData = meshData;
            job.data = data;
            job.voxelSize = new float3(voxelSize, voxelSize, voxelSize);
            job.sizeZ = size.z;
            job.stepYZ = size.y * size.z;
            job.min = min;
            job.max = max;
            job.indexStart = indexStart;
            job.trs = trs;
            job.invTrs = math.inverse(job.trs);
            //job.inflate = math.sqrt(3.0f * 0.25f * voxelSize * voxelSize);
            job.inflate = 0.5f * voxelSize;

            job.ScheduleParallel(data.Length, 8, default).Complete();
        }

        [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
        private struct Job_GenSDF : IJobFor
        {
            [ReadOnly]
            public MeshSDFData meshData;

            [ReadOnly]
            public int3 indexStart;
            [ReadOnly]
            public float3 voxelSize;
            [ReadOnly]
            public int sizeZ, stepYZ;
            [ReadOnly]
            public float min, max;
            [ReadOnly]
            public float4x4 trs;
            [ReadOnly]
            public float4x4 invTrs;
            [ReadOnly]
            public float inflate;

            [WriteOnly]
            public NativeArray<float> data;

            public void Execute(int index)
            {
                int3 i;
                i.x = index / stepYZ;
                i.y = (index % stepYZ) / sizeZ;
                i.z = index % sizeZ;
                i += indexStart;

                float3 point = i * voxelSize;
                float3 point_t = math.transform(invTrs, point);
                Result result = NearestNeighbour_NR(point_t, meshData.tree, meshData.verts, meshData.tris);
                bool inside = IsInside(point_t, result, meshData);
                float3 closestPoint = math.transform(trs, result.pos);
                float f = inside ? -math.distance(point, closestPoint) : math.distance(point, closestPoint);
                data[index] = math.clamp(f - inflate, min, max);
            }
        }

        public static SignedDistance GetSignedDistance(MeshSDFData mesh, float3 t, quaternion r, float3 s, float3 point)
        {
            Job_NNS job = new Job_NNS();
            job.point = point;
            job.trs = float4x4.TRS(t, r, s);
            job.invTrs = math.inverse(job.trs);
            job.meshData = mesh;
            job.distance = new NativeArray<SignedDistance>(1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            job.Schedule().Complete();

            SignedDistance distance = job.distance[0];
            job.distance.Dispose();
            return distance;
        }

        [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
        private struct Job_GenWeighterNormals : IJob
        {
            [ReadOnly]
            public NativeArray<float3> verts;
            [ReadOnly]
            public NativeArray<int3> tris;

            public NativeArray<float3> vertWeightedNormal;
            public NativeHashMap<ulong, float3> edgeWeightedNormal;

            public void Execute()
            {
                int triCount = tris.Length;
                for (int i = 0; i < triCount; i++)
                {
                    ulong key;
                    int3 tri_i = tris[i];
                    float3 a = verts[tri_i.x];
                    float3 b = verts[tri_i.y];
                    float3 c = verts[tri_i.z];

                    float3 n = math.normalize(math.cross((b - a), (c - a)));

                    float wa = math.acos(math.dot(math.normalize(b - a), math.normalize(c - a)));
                    float wb = math.acos(math.dot(math.normalize(a - b), math.normalize(c - b)));
                    float wc = math.acos(math.dot(math.normalize(b - c), math.normalize(a - c)));

                    vertWeightedNormal[tri_i.x] += n * wa;
                    vertWeightedNormal[tri_i.y] += n * wb;
                    vertWeightedNormal[tri_i.z] += n * wc;


                    key = GetEdgeKey(tri_i.x, tri_i.y);
                    if (edgeWeightedNormal.ContainsKey(key)) { edgeWeightedNormal[key] += n * 0.5f; }
                    else { edgeWeightedNormal.Add(key, n * 0.5f); }

                    key = GetEdgeKey(tri_i.y, tri_i.z);
                    if (edgeWeightedNormal.ContainsKey(key)) { edgeWeightedNormal[key] += n * 0.5f; }
                    else { edgeWeightedNormal.Add(key, n * 0.5f); }

                    key = GetEdgeKey(tri_i.x, tri_i.z);
                    if (edgeWeightedNormal.ContainsKey(key)) { edgeWeightedNormal[key] += n * 0.5f; }
                    else { edgeWeightedNormal.Add(key, n * 0.5f); }
                }
            }
        }

        private static void ConstructTree(NativeArray<Node> tree, NativeArray<float3> verts, NativeArray<int3> tris)
        {
            NativeArray<Tri> tris_ordered = new NativeArray<Tri>(tris.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            //Fill the triangle temporary data.
            Job_TriPreComp job_tri = new Job_TriPreComp()
            {
                verts = verts,
                tris = tris,
                tris_ordered = tris_ordered
            };

            job_tri.ScheduleParallel(tris.Length, 16, default).Complete();

            Job_TreeBuild job_tree = new Job_TreeBuild()
            {
                tris_ordered = tris_ordered,
                tree = tree
            };
            job_tree.Schedule().Complete();

            tris_ordered.Dispose();
        }

        private static ulong GetEdgeKey(int a, int b)
        {
            if (a > b) { return ((uint)b) | (((ulong)a) << 32); }
            else { return ((uint)a) | (((ulong)b) << 32); }
        }


        //Given a result from a nearest neighbour search, it checks if the point is within the mesh by comparing against the weighted normals.
        private static bool IsInside(float3 p, Result result, MeshSDFData meshData)
        {
            float3 n;
            int3 tri_i = meshData.tris[result.index];

            switch (result.type)
            {
                case 0:
                    float3 a = meshData.verts[tri_i.x];
                    float3 b = meshData.verts[tri_i.y];
                    float3 c = meshData.verts[tri_i.z];
                    n = math.normalize(math.cross((b - a), (c - a)));
                    break;
                case 1:
                    n = meshData.edgeWeightedNormal[GetEdgeKey(tri_i.y, tri_i.z)];
                    break;
                case 2:
                    n = meshData.edgeWeightedNormal[GetEdgeKey(tri_i.x, tri_i.z)];
                    break;
                case 3:
                    n = meshData.vertWeightedNormal[tri_i.z];
                    break;
                case 4:
                    n = meshData.edgeWeightedNormal[GetEdgeKey(tri_i.x, tri_i.y)];
                    break;
                case 5:
                    n = meshData.vertWeightedNormal[tri_i.y];
                    break;
                case 6:
                    n = meshData.vertWeightedNormal[tri_i.x];
                    break;
                default:
                    n = new float3(0.0f, 0.0f, 0.0f);
                    break;
            }

            return (math.dot(n, result.pos - p) > 0.0f);
        }

        [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
        private struct Job_TriPreComp : IJobFor
        {
            [ReadOnly]
            public NativeArray<float3> verts;
            [ReadOnly]
            public NativeArray<int3> tris;

            [WriteOnly]
            public NativeArray<Tri> tris_ordered;

            public void Execute(int index)
            {
                int3 tri_i = tris[index];
                float3 a = verts[tri_i.x];
                float3 b = verts[tri_i.y];
                float3 c = verts[tri_i.z];
                Sphere sphere = TriBoundingSphere(a, b, c);
                tris_ordered[index] = new Tri() { a = a, b = b, c = c, center = sphere.center, radius = sphere.radius, index = index };
            }
        }

        //Minimum sphere that cointains a triangle.
        private static Sphere TriBoundingSphere(float3 a, float3 b, float3 c)
        {
            float ab = math.lengthsq(b - a);
            float bc = math.lengthsq(c - b);
            float ac = math.lengthsq(c - a);

            Sphere sphere = new Sphere();

            //Swap so that largest side is ab.
            if (bc < ac) { float tmp = bc; bc = ac; ac = tmp; float3 tmpv = a; a = b; b = tmpv; }
            if (ab < bc) { float tmp = ab; ab = bc; bc = tmp; float3 tmpv = c; c = a; a = tmpv; }

            if (ab > (bc + ac))
            {
                //If the angle is obtuse, sphere is has the longest side as diameter.
                sphere.radius = math.sqrt(ab) * 0.5f;
                sphere.center = (a + b) * 0.5f;
            }
            else
            {
                //Otherwise, sphere is given by 3D circumscribed circle.
                float3 A = a - c;
                float3 B = b - c;
                float cos = (bc + ac - ab) / (math.sqrt(bc) * math.sqrt(ac) * 2.0f);
                float3 AxB = math.cross(A, B);

                sphere.radius = math.sqrt(ab) / (math.sqrt(1.0f - cos * cos) * 2.0f);
                sphere.center = math.cross((math.dot(A, A) * B - math.dot(B, B) * A), AxB) / (math.dot(AxB, AxB) * 2.0f) + c;
            }

            return sphere;
        }

        //KD tree node with bounding spheres as volume.
        //Children division is not consistent, as position queries are never needed.
        //Left and right are the indexes of the children.
        //If left is -1, then this is a leaf, and right stores the index of the final triangle.
        [Serializable]
        public struct Node
        {
            public float3 pos;
            public float radius;
            public int left, right;
        }

        //Temporary data for the non recursive generation of the tree.
        private struct NodeParam
        {
            public int start, end;
            public int index_parent;
            public bool left;
        }

        //Temporary data for a triangle. Use in the tree generation. Includes its bounding sphere.
        private struct Tri
        {
            public float3 a, b, c;
            public int index;
            public float3 center;
            public float radius;
        }

        private struct Sphere
        {
            public float3 center;
            public float radius;
        }

        //Builds a kd tree BVH non recursively.
        //Uses Ritter's "Efficient bounding sphere" algorithm for nodes with multiple tris.
        [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
        private struct Job_TreeBuild : IJob
        {
            public NativeArray<Tri> tris_ordered;
            public NativeArray<Node> tree;

            public void Execute()
            {
                TreeNR(tris_ordered, tree);
            }
        }

        private static void TreeNR(NativeArray<Tri> tris_ordered, NativeArray<Node> tree)
        {
            NativeArray<NodeParam> stack = new NativeArray<NodeParam>(math.ceillog2(tris_ordered.Length) + 4, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            int index = 0;
            int stackIndex = 0;
            stack[0] = new NodeParam() { left = false, start = 0, end = tris_ordered.Length };


            while (stackIndex > -1)
            {
                NodeParam ndata = stack[stackIndex--];
                int slice = ndata.end - ndata.start;

                if (slice == 1)
                {
                    //If only 1 tri remains, make this into a leaf node.
                    Node node = new Node();

                    node.left = -1;
                    node.right = tris_ordered[ndata.start].index;

                    node.pos = tris_ordered[ndata.start].center;
                    node.radius = tris_ordered[ndata.start].radius;

                    tree[index] = node;

                    //Fill this node's index in its parent.
                    node = tree[ndata.index_parent];
                    if (ndata.left) { node.left = index; }
                    else { node.right = index; }
                    tree[ndata.index_parent] = node;

                    index++;
                }
                else
                {
                    //More than 1 tri remains.
                    Node node = new Node();

                    //Get the AABB for splitting, and the min/max points for Ritt's algorithm.
                    float3 center;
                    float radius;
                    float3 min = new float3(float.MaxValue, float.MaxValue, float.MaxValue);
                    float3 max = new float3(float.MinValue, float.MinValue, float.MinValue);
                    float3 bx = min, bX = max, by = min, bY = max, bz = min, bZ = max;
                    for (int i = ndata.start; i < ndata.end; i++)
                    {
                        float3 p = tris_ordered[i].a;
                        if (p.x < min.x) { min.x = p.x; bx = p; }
                        if (p.y < min.y) { min.y = p.y; by = p; }
                        if (p.z < min.z) { min.z = p.z; bz = p; }
                        if (p.x > max.x) { max.x = p.x; bX = p; }
                        if (p.y > max.y) { max.y = p.y; bY = p; }
                        if (p.z > max.z) { max.z = p.z; bZ = p; }

                        p = tris_ordered[i].b;
                        if (p.x < min.x) { min.x = p.x; bx = p; }
                        if (p.y < min.y) { min.y = p.y; by = p; }
                        if (p.z < min.z) { min.z = p.z; bz = p; }
                        if (p.x > max.x) { max.x = p.x; bX = p; }
                        if (p.y > max.y) { max.y = p.y; bY = p; }
                        if (p.z > max.z) { max.z = p.z; bZ = p; }

                        p = tris_ordered[i].c;
                        if (p.x < min.x) { min.x = p.x; bx = p; }
                        if (p.y < min.y) { min.y = p.y; by = p; }
                        if (p.z < min.z) { min.z = p.z; bz = p; }
                        if (p.x > max.x) { max.x = p.x; bX = p; }
                        if (p.y > max.y) { max.y = p.y; bY = p; }
                        if (p.z > max.z) { max.z = p.z; bZ = p; }
                    }

                    float dx = math.distance(bx, bX);
                    float dy = math.distance(by, bY);
                    float dz = math.distance(bz, bZ);

                    //Make the initial guess for the bounding sphere.
                    if (dx > dy)
                    {
                        if (dx > dz) { center = (bx + bX) * 0.5f; radius = dx * 0.5f; }
                        else { center = (bz + bZ) * 0.5f; radius = dz * 0.5f; }
                    }
                    else
                    {
                        if (dy > dz) { center = (by + bY) * 0.5f; radius = dy * 0.5f; }
                        else { center = (bz + bZ) * 0.5f; radius = dz * 0.5f; }
                    }

                    //For each point, if it is not contained, increase the sphere. From Ritt's algorithm.
                    float radius_sq = radius * radius;
                    for (int i = ndata.start; i < ndata.end; i++)
                    {
                        float3 p = tris_ordered[i].a;

                        float dist = math.lengthsq(p - center);
                        if (dist > radius_sq)
                        {
                            dist = math.sqrt(dist);
                            radius = (radius + dist) / 2.0f;
                            radius_sq = radius * radius;
                            float diff = dist - radius;
                            center = (center * radius + p * diff) / dist;
                        }

                        p = tris_ordered[i].b;

                        dist = math.lengthsq(p - center);
                        if (dist > radius_sq)
                        {
                            dist = math.sqrt(dist);
                            radius = (radius + dist) / 2.0f;
                            radius_sq = radius * radius;
                            float diff = dist - radius;
                            center = (center * radius + p * diff) / dist;
                        }

                        p = tris_ordered[i].c;

                        dist = math.lengthsq(p - center);
                        if (dist > radius_sq)
                        {
                            dist = math.sqrt(dist);
                            radius = (radius + dist) / 2.0f;
                            radius_sq = radius * radius;
                            float diff = dist - radius;
                            center = (center * radius + p * diff) / dist;
                        }
                    }

                    //Choose the splitting direction, and order the tris.
                    float3 diagonal = max - min;
                    if (diagonal.x > diagonal.y)
                    {
                        if (diagonal.x > diagonal.z) { Sort_x(tris_ordered, ndata.start, ndata.end - 1); }
                        else { Sort_z(tris_ordered, ndata.start, ndata.end - 1); }
                    }
                    else
                    {
                        if (diagonal.y > diagonal.z) { Sort_y(tris_ordered, ndata.start, ndata.end - 1); }
                        else { Sort_z(tris_ordered, ndata.start, ndata.end - 1); }
                    }

                    node.pos = center;
                    node.radius = radius;

                    tree[index] = node;

                    //Fill this node's index in its parent.
                    node = tree[ndata.index_parent];
                    if (ndata.left) { node.left = index; }
                    else { node.right = index; }
                    tree[ndata.index_parent] = node;

                    //Add both children to the stack, so that the left one is calculated first.
                    int mid = (ndata.start + ndata.end) / 2;
                    stack[++stackIndex] = new NodeParam() { left = false, index_parent = index, start = mid, end = ndata.end };
                    stack[++stackIndex] = new NodeParam() { left = true, index_parent = index, start = ndata.start, end = mid };

                    index++;
                }
            }
            stack.Dispose();
        }

        //Internal result for a nearest neighbour search.
        private struct Result
        {
            public float distance;
            public float distance_sq;
            public float3 pos;
            public int trisChecked;
            public int type;
            public int index;
        }

        //Temporary data for the stack in the non recursive nearest neighbour search.
        private struct NNS_NR
        {
            public int index;
            public float dist;

            public NNS_NR(int index, float dist)
            {
                this.index = index;
                this.dist = dist;
            }
        }

        //Distance is not actually signed (it is always positive).
        public struct SignedDistance
        {
            public float3 position;
            public float distance;
            public bool sign;

            public SignedDistance(float3 position, float distance, bool sign)
            {
                this.position = position;
                this.distance = distance;
                this.sign = sign;
            }
        }

        [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
        private struct Job_NNS : IJob
        {
            [ReadOnly]
            public MeshSDFData meshData;

            [ReadOnly]
            public float3 point;
            [ReadOnly]
            public float4x4 trs;
            [ReadOnly]
            public float4x4 invTrs;

            [WriteOnly]
            public NativeArray<SignedDistance> distance;

            public void Execute()
            {
                float3 point_t = math.transform(invTrs, point);
                Result result = NearestNeighbour_NR(point_t, meshData.tree, meshData.verts, meshData.tris);
                bool inside = IsInside(point_t, result, meshData);
                float3 closestPoint = math.transform(trs, result.pos);
                distance[0] = new SignedDistance(closestPoint, math.distance(point, closestPoint), inside);
            }
        }

        //Nearest neighbour search, non recursive.
        private static Result NearestNeighbour_NR(float3 point, NativeArray<Node> tree, NativeArray<float3> verts, NativeArray<int3> tris)
        {
            Result result = new Result() { distance = float.MaxValue, distance_sq = float.MaxValue, trisChecked = 0 };

            NativeArray<NNS_NR> stack = new NativeArray<NNS_NR>(math.ceillog2(tree.Length) + 4, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            int stackIndex = 0;
            stack[0] = new NNS_NR(0, 0.0f);

            while (stackIndex > -1)
            {
                NNS_NR data = stack[stackIndex--];

                int left = tree[data.index].left;
                int right = tree[data.index].right;

                if (data.dist > result.distance) { continue; }
                else if (left == -1)
                {
                    result.trisChecked++;
                    int3 tri = tris[right];
                    int type = -1;
                    float3 pr = ClosestPointInTri(verts[tri.x], verts[tri.y], verts[tri.z], point, ref type);
                    float distance_sq = math.distancesq(point, pr);
                    if (distance_sq < result.distance_sq)
                    {
                        result.distance_sq = distance_sq;
                        result.distance = math.sqrt(distance_sq);
                        result.pos = pr;
                        result.type = type;
                        result.index = right;
                    }
                }
                else
                {
                    float distance_l = math.length(point - tree[left].pos) - tree[left].radius;
                    float distance_r = math.length(point - tree[right].pos) - tree[right].radius;

                    if (distance_l < distance_r)
                    {
                        if (distance_r < result.distance) { stack[++stackIndex] = new NNS_NR(right, distance_r); }
                        if (distance_l < result.distance) { stack[++stackIndex] = new NNS_NR(left, distance_l); }
                    }
                    else
                    {
                        if (distance_l < result.distance) { stack[++stackIndex] = new NNS_NR(left, distance_l); }
                        if (distance_r < result.distance) { stack[++stackIndex] = new NNS_NR(right, distance_r); }
                    }
                }
            }

            stack.Dispose();
            return result;
        }

        //Calculates the closest point within the triangle.
        //Also return the type of geometry that point belongs to (face, edge, vertex).
        private static float3 ClosestPointInTri(float3 a, float3 b, float3 c, float3 p, ref int type)
        {
            float3 v0 = b - a;
            float3 v1 = c - a;
            float3 v2 = p - a;

            float d00 = math.dot(v0, v0);
            float d01 = math.dot(v0, v1);
            float d11 = math.dot(v1, v1);
            float d20 = math.dot(v2, v0);
            float d21 = math.dot(v2, v1);

            float invDenom = 1.0f / (d00 * d11 - d01 * d01);

            float3 n = math.normalize(math.cross(v0, v1));
            float3 pr = p - (math.dot(n, p - a)) * n;
            float3 br;

            br.y = (d11 * d20 - d01 * d21) * invDenom;
            br.z = (d00 * d21 - d01 * d20) * invDenom;
            br.x = 1.0f - br.y - br.z;

            pr = BaricentricToClosest(a, b, c, pr, br, ref type);

            return pr;
        }

        //Determines the closest point given the baricentric coordinates.
        private static float3 BaricentricToClosest(float3 a, float3 b, float3 c, float3 p, float3 br, ref int type)
        {
            int m = 0;
            float3 r;
            float u;
            if (br.x < 0.0f) { m |= 1; }
            if (br.y < 0.0f) { m |= 2; }
            if (br.z < 0.0f) { m |= 4; }

            switch (m)
            {
                case 0:
                    r = p;
                    type = 0;
                    break;
                case 1:
                    u = ((p.x - b.x) * (c.x - b.x)) + ((p.y - b.y) * (c.y - b.y)) + ((p.z - b.z) * (c.z - b.z));
                    u /= math.lengthsq(c - b);
                    if (u < 0.0f) { r = b; type = 5; }
                    else if (u > 1.0f) { r = c; type = 3; }
                    else { r = b + u * (c - b); type = 1; }
                    break;
                case 2:
                    u = ((p.x - a.x) * (c.x - a.x)) + ((p.y - a.y) * (c.y - a.y)) + ((p.z - a.z) * (c.z - a.z));
                    u /= math.lengthsq(c - a);
                    if (u < 0.0f) { r = a; type = 6; }
                    else if (u > 1.0f) { r = c; type = 3; }
                    else { r = a + u * (c - a); type = 2; }
                    break;
                case 3:
                    r = c;
                    type = 3;
                    break;
                case 4:
                    u = ((p.x - a.x) * (b.x - a.x)) + ((p.y - a.y) * (b.y - a.y)) + ((p.z - a.z) * (b.z - a.z));
                    u /= math.lengthsq(b - a);
                    if (u < 0.0f) { r = a; type = 6; }
                    else if (u > 1.0f) { r = b; type = 5; }
                    else { r = a + u * (b - a); type = 4; }
                    break;
                case 5:
                    r = b;
                    type = 5;
                    break;
                case 6:
                    r = a;
                    type = 6;
                    break;
                default:
                    r = new float3(0.0f, 0.0f, 0.0f);
                    type = -1;
                    break;
            }

            return r;
        }

        private static void Sort_x(NativeArray<Tri> tris, int start, int end)
        {
            int range = end - start;

            if (range <= 0) { return; }
            else if (range == 1)
            {
                if (tris[start].center.x > tris[end].center.x)
                {
                    Tri tmp = tris[start];
                    tris[start] = tris[end];
                    tris[end] = tmp;
                }
            }
            else if (range <= 9)
            {
                for (int i = start + 1; i <= end; i++)
                {
                    for (int j = i; j > start && tris[j - 1].center.x > tris[j].center.x; j--)
                    {
                        Tri tmp = tris[j];
                        tris[j] = tris[j - 1];
                        tris[j - 1] = tmp;
                    }
                }
            }
            else
            {
                float pivot = tris[end].center.x;
                int j = start - 1;

                for (int i = start; i < end; i++)
                {
                    if (pivot > tris[i].center.x)
                    {
                        j++;
                        Tri tmp = tris[i];
                        tris[i] = tris[j];
                        tris[j] = tmp;
                    }
                }
                j++;

                {
                    Tri tmp = tris[end];
                    tris[end] = tris[j];
                    tris[j] = tmp;
                }

                Sort_x(tris, start, j - 1);
                Sort_x(tris, j + 1, end);
            }
        }

        private static void Sort_y(NativeArray<Tri> tris, int start, int end)
        {
            int range = end - start;

            if (range <= 0) { return; }
            else if (range == 1)
            {
                if (tris[start].center.y > tris[end].center.y)
                {
                    Tri tmp = tris[start];
                    tris[start] = tris[end];
                    tris[end] = tmp;
                }
            }
            else if (range <= 9)
            {
                for (int i = start + 1; i <= end; i++)
                {
                    for (int j = i; j > start && tris[j - 1].center.y > tris[j].center.y; j--)
                    {
                        Tri tmp = tris[j];
                        tris[j] = tris[j - 1];
                        tris[j - 1] = tmp;
                    }
                }
            }
            else
            {
                float pivot = tris[end].center.y;
                int j = start - 1;

                for (int i = start; i < end; i++)
                {
                    if (pivot > tris[i].center.y)
                    {
                        j++;
                        Tri tmp = tris[i];
                        tris[i] = tris[j];
                        tris[j] = tmp;
                    }
                }
                j++;

                {
                    Tri tmp = tris[end];
                    tris[end] = tris[j];
                    tris[j] = tmp;
                }

                Sort_y(tris, start, j - 1);
                Sort_y(tris, j + 1, end);
            }
        }

        private static void Sort_z(NativeArray<Tri> tris, int start, int end)
        {
            int range = end - start;

            if (range <= 0) { return; }
            else if (range == 1)
            {
                if (tris[start].center.z > tris[end].center.z)
                {
                    Tri tmp = tris[start];
                    tris[start] = tris[end];
                    tris[end] = tmp;
                }
            }
            else if (range <= 9)
            {
                for (int i = start + 1; i <= end; i++)
                {
                    for (int j = i; j > start && tris[j - 1].center.z > tris[j].center.z; j--)
                    {
                        Tri tmp = tris[j];
                        tris[j] = tris[j - 1];
                        tris[j - 1] = tmp;
                    }
                }
            }
            else
            {
                float pivot = tris[end].center.z;
                int j = start - 1;

                for (int i = start; i < end; i++)
                {
                    if (pivot > tris[i].center.z)
                    {
                        j++;
                        Tri tmp = tris[i];
                        tris[i] = tris[j];
                        tris[j] = tmp;
                    }
                }
                j++;

                {
                    Tri tmp = tris[end];
                    tris[end] = tris[j];
                    tris[j] = tmp;
                }

                Sort_z(tris, start, j - 1);
                Sort_z(tris, j + 1, end);
            }
        }

        //Tri-Box overlap based on "Fast 3D Triangle-Box Overlap Testing" by Tomas Akenine-Möller
        //Simplified for axis aligned and cubic extents.
        //TODO: might not actually work, center position might be applied wrongly
        private static bool AABBTriOverlap(float3 center, float extents, float3 a, float3 b, float3 c)
        {
            float halfExtents = extents * 0.5f;

            //Simple AABB <-> tri bounds test
            float3 box_min = center - halfExtents;
            float3 box_max = center + halfExtents;

            float3 triMin = math.min(math.min(a, b), c);
            float3 triMax = math.max(math.max(a, b), c);

            if (box_max.x < triMin.x || box_max.y < triMin.y || box_max.z < triMin.z ||
            triMax.x < box_min.x || triMax.y < box_min.y || triMax.z < box_min.z) { return false; }


            //SAT triangle normal test
            float3 n = math.normalize(math.cross(b - a, c - b));
            float3 A = a - center;

            float s = math.dot(n, A);
            if (math.abs(s) > (halfExtents * (math.abs(n.x) + math.abs(n.y) + math.abs(n.z)))) { return false; }


            //SAT cross product of edges
            float3 B = b - center;
            float3 C = c - center;
            float3 f0 = B - A;
            float3 f1 = C - B;
            float3 f2 = A - C;
            float3 v;
            float p0, p1, p2;
            float r;

            //ex x f0
            v = new float3(0.0f, -f0.z, f0.y);
            p0 = math.dot(v, A);
            p2 = math.dot(v, C);
            r = halfExtents * (math.abs(v.y) + math.abs(v.z));
            if (math.min(p0, p2) > r || math.max(p0, p2) < -r) { return false; }

            //ex x f1
            v = new float3(0.0f, -f1.z, f1.y);
            p0 = math.dot(v, A);
            p1 = math.dot(v, B);
            r = halfExtents * (math.abs(v.y) + math.abs(v.z));
            if (math.min(p0, p1) > r || math.max(p0, p1) < -r) { return false; }

            //ex x f2
            v = new float3(0.0f, -f2.z, f2.y);
            p0 = math.dot(v, A);
            p1 = math.dot(v, B);
            r = halfExtents * (math.abs(v.y) + math.abs(v.z));
            if (math.min(p0, p1) > r || math.max(p0, p1) < -r) { return false; }

            //ey x f0
            v = new float3(f0.z, 0.0f, -f0.x);
            p0 = math.dot(v, A);
            p2 = math.dot(v, C);
            r = halfExtents * (math.abs(v.x) + math.abs(v.z));
            if (math.min(p0, p2) > r || math.max(p0, p2) < -r) { return false; }

            //ey x f1
            v = new float3(f1.z, 0.0f, -f1.x);
            p0 = math.dot(v, A);
            p1 = math.dot(v, B);
            r = halfExtents * (math.abs(v.x) + math.abs(v.z));
            if (math.min(p0, p1) > r || math.max(p0, p1) < -r) { return false; }

            //ey x f2
            v = new float3(f2.z, 0.0f, -f2.x);
            p0 = math.dot(v, A);
            p1 = math.dot(v, B);
            r = halfExtents * (math.abs(v.x) + math.abs(v.z));
            if (math.min(p0, p1) > r || math.max(p0, p1) < -r) { return false; }

            //ez x f0
            v = new float3(-f0.y, f0.x, 0.0f);
            p0 = math.dot(v, A);
            p2 = math.dot(v, C);
            r = halfExtents * (math.abs(v.x) + math.abs(v.y));
            if (math.min(p0, p2) > r || math.max(p0, p2) < -r) { return false; }

            //ez x f1
            v = new float3(-f1.y, f1.x, 0.0f);
            p0 = math.dot(v, A);
            p1 = math.dot(v, B);
            r = halfExtents * (math.abs(v.x) + math.abs(v.y));
            if (math.min(p0, p1) > r || math.max(p0, p1) < -r) { return false; }

            //ez x f2
            v = new float3(-f2.y, f2.x, 0.0f);
            p0 = math.dot(v, A);
            p1 = math.dot(v, B);
            r = halfExtents * (math.abs(v.x) + math.abs(v.y));
            if (math.min(p0, p1) > r || math.max(p0, p1) < -r) { return false; }

            return true;
        }

        public static Bounds CalculateBounds(MeshSDFData mesh, float3 t, float3 r, float3 s)
        {
            Bounds bounds = new Bounds();

            Job_Bounds job = new Job_Bounds();
            job.verts = mesh.verts;
            job.t = t;
            job.s = s;
            job.r = float3x3.Euler(math.radians(r));
            job.data = new NativeArray<float3>(2, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            job.Schedule().Complete();

            bounds.SetMinMax(job.data[0], job.data[1]);

            job.data.Dispose();

            return bounds;
        }


        [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
        private struct Job_Bounds : IJob
        {
            [ReadOnly]
            public NativeArray<float3> verts;

            [ReadOnly]
            public float3 t;
            [ReadOnly]
            public float3x3 r;
            [ReadOnly]
            public float3 s;

            [WriteOnly]
            public NativeArray<float3> data;


            public void Execute()
            {
                int iterationCount = verts.Length;

                float3 min = new float3(float.MaxValue, float.MaxValue, float.MaxValue);
                float3 max = new float3(float.MinValue, float.MinValue, float.MinValue);
                float3x3 rs = math.mul(r, float3x3.Scale(s));

                for (int i = 0; i < iterationCount; i++)
                {
                    float3 v = math.mul(rs, verts[i]);
                    min = math.min(v, min);
                    max = math.max(v, max);
                }

                data[0] = min + t;
                data[1] = max + t;
            }
        }
    }

}