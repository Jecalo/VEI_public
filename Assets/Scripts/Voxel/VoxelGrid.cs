using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Mathematics;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Burst;
using Unity.Jobs;

namespace VoxelTerrain
{
    public enum ChunkFillState : uint
    {
        Mixed,                  //Chunk has a mix of air and one or more materials
        Empty,                  //Chunk is fully filled with air
        SolidSingleMaterial,    //Chunk is fully filled with a single material
        Solid                   //Chunk is fully filled with different materials
    }

    //Cubic chunk, containing its source data and references to the object and its components
    public class VoxelChunk
    {
        public int3 index;                          //Index of the chunk
        public NativeArray<float> data;             //Chunk data
        public NativeArray<byte> material;          //Chunk material data

        public ChunkFillState fillState;            //Whether this chunk is fully filled
        public byte fillMaterial;                   //Material for a filled chunk
        public bool hasData = false;

        public VoxelGrid grid;                      //Parent grid

        //Fields with references to the gameobject that holds this chunk
        public GameObject gameObject = null;
        public Mesh mesh = null;
        public MeshFilter meshFilter = null;
        public MeshRenderer meshRenderer = null;
        public MeshCollider coll = null;

        public void Allocate()
        {
            if (hasData) { Debug.LogErrorFormat("Chunk is already allocated: {0}", index); return; }

            data = MemoryManager.GetArrayFloat();
            material = MemoryManager.GetArrayByte();
            hasData = true;
        }

        public void Dispose()
        {
            if (!hasData) { return; }

            MemoryManager.Return(data);
            MemoryManager.Return(material);
            data = new NativeArray<float>();
            material = new NativeArray<byte>();
            hasData = false;
        }
    }

    //General class that holds the entire voxel terrain, divided in chunks.
    public class VoxelGrid
    {
        public GameObject ChunkPrefab = null;
        public GameObject GridParent = null;

        public float chunkSize = 16.0f;                 //Size of the chunks
        public int chunkRes = 32;                       //Resolution of the chunks (SDF points per side)

        public int chunkResStep;                        //Resolution squared

        public float voxelSize;                         //Voxel size of the chunks
        public int chunkDataSize;                       //Maximum index in each chunk data
        public float chunkDisplacement;                 //Total distance from the start of a chunk to the next
        public float minClamp, maxClamp;                //Maximum/Minimum value in the data set
        public int chunkRes1, chunkRes2;                //Chunk resolution -1 / -2

        public float3 t = 0f, r = 0f, s = 1f;
        public float4x4 trs, invTrs;

        public Dictionary<int3, VoxelChunk> chunks;

        public HashSet<VoxelChunk> dirtyChunks;


        public void Initialize()
        {
            if (chunkSize < 4f) { chunkSize = 4f; Debug.LogWarning("Chunk size is too small."); }
            if (chunkRes < 8) { chunkRes = 8; Debug.LogWarning("Chunk resolution is too small."); }
            if (ChunkPrefab == null) { Debug.LogError("Null chunk prefab."); }
            if (GridParent == null) { Debug.LogError("Null grid parent."); }

            chunks = new Dictionary<int3, VoxelChunk>();
            dirtyChunks = new (8);
            voxelSize = chunkSize / (chunkRes - 1);
            chunkDataSize = chunkRes * chunkRes * chunkRes;
            chunkDisplacement = chunkSize * ((float)(chunkRes - 2) / (chunkRes - 1));
            minClamp = -2.0f * voxelSize;
            maxClamp = 2.0f * voxelSize;
            chunkRes1 = chunkRes - 1;
            chunkRes2 = chunkRes - 2;
            chunkResStep = chunkRes * chunkRes;

            UpdateTRSMatrix();
        }

        public VoxelChunk AddChunk(int3 id)
        {
            if (chunks.ContainsKey(id)) { Debug.LogWarning("Chunk already exists: " + id); return chunks[id]; }

            VoxelChunk c = new VoxelChunk();

            c.index = id;
            c.fillState = ChunkFillState.Mixed;
            c.fillMaterial = 0;
            c.grid = this;
            c.Allocate();

            GameObject obj = UnityEngine.Object.Instantiate(ChunkPrefab, GridParent.transform);
            obj.name = Constants.ProcMeshName + "_" + id.x + "_" + id.y + "_" + id.z;
            obj.transform.localPosition = (float3)id * chunkDisplacement;
            c.gameObject = obj;
            c.meshFilter = obj.GetComponent<MeshFilter>();
            c.meshRenderer = obj.GetComponent<MeshRenderer>();
            c.coll = obj.GetComponent<MeshCollider>();
            c.mesh = new Mesh() { name = Constants.ProcMeshName };
            c.coll.sharedMesh = c.mesh;
            c.meshFilter.sharedMesh = c.mesh;

            chunks.Add(id, c);
            dirtyChunks.Add(c);

            return c;
        }

        public void RemoveChunk(int3 id)
        {
            if (!chunks.ContainsKey(id)) { Debug.LogWarning("Chunk doesn't exist: " + id); return; }

            VoxelChunk c = chunks[id];

            c.Dispose();
            UnityEngine.Object.Destroy(c.gameObject);

            chunks.Remove(id);
            dirtyChunks.Remove(c);
        }

        public void ChangeChunk(int3 oldID, int3 newId)
        {
            if (!chunks.ContainsKey(oldID)) { Debug.LogWarning("Chunk doesn't exist: " + oldID); return; }
            if (chunks.ContainsKey(newId)) { Debug.LogWarning("Chunk already exists: " + newId); return; }

            VoxelChunk c = chunks[oldID];
            c.gameObject.name = Constants.ProcMeshName + "_" + newId.x + "_" + newId.y + "_" + newId.z;
            c.gameObject.transform.localPosition = (float3)newId * chunkDisplacement;
            c.index = newId;

            chunks.Remove(oldID);
            chunks.Add(newId, c);
        }

        public void RemoveAllChunks()
        {
            foreach (var c in chunks.Values)
            {
                c.Dispose();
                UnityEngine.Object.Destroy(c.gameObject);
            }
            chunks.Clear();
            dirtyChunks.Clear();
        }

        public void SetTRS(Transform transform)
        {
            t = transform.position;
            r = transform.rotation.eulerAngles;
            s = transform.localScale;
            trs = transform.localToWorldMatrix;
            invTrs = transform.worldToLocalMatrix;
        }

        public void SetTRS(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            t = position;
            r = rotation.eulerAngles;
            s = scale;
            UpdateTRSMatrix();
        }

        public void SetTRS(Vector3 position, Vector3 rotation, Vector3 scale)
        {
            t = position;
            r = rotation;
            s = scale;
            UpdateTRSMatrix();
        }

        public void UpdateTRSMatrix()
        {
            trs = float4x4.TRS(t, quaternion.Euler(math.radians(r)), s);
            invTrs = math.inverse(trs);
        }

        public float3 LocalToWorld(float3 p)
        {
            return math.transform(trs, p);
        }

        public float3 WorldToLocal(float3 p)
        {
            return math.transform(invTrs, p);
        }

        public int3 WorldToChunkIndex(float3 p)
        {
            p = WorldToLocal(p);
            return (int3)math.floor(p / chunkDisplacement);
        }

        public float3 ChunkIndexToWorld_Center(int3 i)
        {
            float3 p = (float3)i * chunkDisplacement + 0.5f * chunkDisplacement;
            return LocalToWorld(p);
        }

        public int3 LocalToChunkIndex(float3 p)
        {
            return (int3)math.floor(p / chunkDisplacement);
        }

        public int3 WorldToVoxelIndex(float3 p)
        {
            p = WorldToLocal(p);
            return (int3)math.floor(p / voxelSize);
        }

        public int3 LocalToVoxelIndex(float3 p)
        {
            return (int3)math.floor(p / voxelSize);
        }

        public float3 WorldToChunkPos(float3 p)
        {
            p = WorldToLocal(p);
            int3 i = LocalToChunkIndex(p);
            return p - (float3)i * chunkDisplacement;
        }

        public float3 LocalToChunkPos(float3 p)
        {
            int3 i = LocalToChunkIndex(p);
            return p - (float3)i * chunkDisplacement;
        }

        public void IndexToChunkIndex(int3 i, out int3 localIndex, out int3 chunkIndex)
        {
            chunkIndex = i / chunkRes2;
            localIndex = i % chunkRes2;

            if (localIndex.x < 0) { localIndex.x += chunkRes2; }
            if (localIndex.y < 0) { localIndex.y += chunkRes2; }
            if (localIndex.z < 0) { localIndex.z += chunkRes2; }
            if (i.x < 0 && localIndex.x != 0) { chunkIndex.x--; }
            if (i.y < 0 && localIndex.y != 0) { chunkIndex.y--; }
            if (i.z < 0 && localIndex.z != 0) { chunkIndex.z--; }
        }

        public float3 IndexToWorld(int3 i)
        {
            float3 p = (float3)i * voxelSize;
            return LocalToWorld(p);
        }

        public void Dispose()
        {
            foreach (var c in chunks)
            {
                c.Value.Dispose();
            }
        }

        public void SetDirtyAll()
        {
            foreach (var c in chunks)
            {
                dirtyChunks.Add(c.Value);
            }
        }

        public void RegenerateDirtyNow()
        {
            if(dirtyChunks.Count == 0) { return; }

            System.Diagnostics.Stopwatch sw2 = new();
            sw2.Start();
            List<Tuple<JobWrapper<NaiveSurfaceNetsM.Job>, int3>> snJobs = new ();

            System.Diagnostics.Stopwatch sw = new();
            double[] t = new double[4];

            sw.Start();
            foreach (VoxelChunk c in dirtyChunks)
            {
                if (c.fillState == ChunkFillState.Mixed)
                {
                    //Queue up all chunks that must be meshed
                    snJobs.Add(new(NaiveSurfaceNetsM.GenerateJob(c.data, c.material, chunkRes, float3.zero, voxelSize), c.index));
                    snJobs[snJobs.Count - 1].Item1.Schedule();
                }
                else
                {
                    //Otherwise, disable solid chunks
                    c.meshFilter.gameObject.SetActive(false);
                    c.meshFilter.sharedMesh = null;
                    c.meshRenderer.sharedMaterials = new Material[0];
                    c.coll.sharedMesh = null;
                    c.mesh.Clear();
                }
            }

            List<NativeList<float3>> verts = new();
            List<NativeList<float3>> normals = new();
            List<NativeList<int4>> quads = new();
            List<NativeList<NaiveSurfaceNetsM.MatBlock>> mats = new();
            List<int3> indexes = new();
            List<Mesh> meshes = new();

            foreach (var job in snJobs)
            {
                job.Item1.Handle.Complete();

                int3 ic = job.Item2;
                if (job.Item1.JobData.quads.Length != 0)
                {
                    //A non empty mesh was generated. Gather its data to update it
                    verts.Add(job.Item1.JobData.verts);
                    normals.Add(job.Item1.JobData.normals);
                    quads.Add(job.Item1.JobData.quads);
                    mats.Add(job.Item1.JobData.matBlocks);
                    meshes.Add(chunks[ic].mesh);
                    indexes.Add(ic);
                }
                else
                {
                    //An empty mesh was generated because there is voxel data on a boundary that is meshed by its neighbour
                    //Debug.LogWarningFormat("Empty mesh generated: {0}", ic);
                    VoxelChunk c = chunks[ic];
                    c.meshFilter.gameObject.SetActive(false);
                    c.meshFilter.sharedMesh = null;
                    c.meshRenderer.sharedMaterials = new Material[0];
                    c.coll.sharedMesh = null;
                    c.mesh.Clear();
                }
            }
            sw.Stop();
            t[0] = sw.Elapsed.TotalMilliseconds;

            sw.Restart();
            MeshGenM.GenerateBatch(meshes, verts, normals, quads, mats);
            sw.Stop();
            t[1] = sw.Elapsed.TotalMilliseconds;


            sw.Restart();
            PhysicsBake.Bake(meshes);
            sw.Stop();
            t[2] = sw.Elapsed.TotalMilliseconds;


            sw.Restart();
            List<Material> newMats = new();
            for (int i = 0; i < meshes.Count; i++)
            {
                int3 ic = indexes[i];
                VoxelChunk c = chunks[ic];

                if (meshes[i].GetIndexCount(0) != 0)
                {
                    newMats.Clear();
                    foreach (var m in mats[i])
                    {
                        newMats.Add(VoxelManager.GetMaterial(m.material));
                    }
                    c.meshRenderer.sharedMaterials = newMats.ToArray();
                    c.meshFilter.sharedMesh = c.mesh;

                    if (!c.meshFilter.gameObject.activeSelf) { c.meshFilter.gameObject.SetActive(true); }

                    c.coll.sharedMesh = null;
                    c.coll.sharedMesh = c.mesh;
                }
                else
                {
                    Debug.LogErrorFormat("Empty mesh generated from non empty quad list: {0}", ic);
                    c.meshFilter.gameObject.SetActive(false);
                    c.meshFilter.sharedMesh = null;
                    c.meshRenderer.sharedMaterials = new Material[0];
                    c.coll.sharedMesh = null;
                    c.mesh.Clear();
                }
            }
            sw.Stop();
            t[3] = sw.Elapsed.TotalMilliseconds;


            foreach (var job in snJobs)
            {
                MemoryManager.Return(job.Item1.JobData.verts);
                MemoryManager.Return(job.Item1.JobData.normals);
                MemoryManager.Return(job.Item1.JobData.quads);
                MemoryManager.Return(job.Item1.JobData.vertexIndexBuffer);
                job.Item1.JobData.matBlocks.Dispose();
                job.Item1.JobData.matBuffer.Dispose();
            }


            DebugHelper.AddMsg(string.Format("Dirty chunks: {3}/{4} | Mesh gen: {0}ms | Mesh building: {1}ms | Physics bake: {2}ms",
                t[0], t[1], t[2], snJobs.Count, dirtyChunks.Count, t[3]), 0);

            dirtyChunks.Clear();

            sw2.Stop();
            DebugHelper.AddMsg($"Total regen time: {sw2.Elapsed.TotalMilliseconds}ms");
        }

        //Updates the fill state for all chunks that have data, possibly turning them into dataless chunks
        public static void TryRemoveChunkData(IEnumerable<VoxelChunk> chunks)
        {
            List<JobWrapper<UpdateFillStateJob>> jobs = new List<JobWrapper<UpdateFillStateJob>>();
            List<VoxelChunk> updatedChunks = new List<VoxelChunk>();
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

            sw.Start();
            foreach (var c in chunks)
            {
                if (!c.hasData) { continue; }

                updatedChunks.Add(c);

                var job = new UpdateFillStateJob();
                job.chunkData = c.data;
                job.chunkMatData = c.material;
                job.chunkDataLength = Constants.ChunkIndexSize;
                job.chunkIndex = c.index;
                job.fillState = new NativeArray<ChunkFillState>(1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                job.fillMat = new NativeArray<byte>(1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                jobs.Add(new JobWrapper<UpdateFillStateJob>(job));
                jobs[jobs.Count - 1].Schedule();
            }

            for (int i = 0; i < jobs.Count; i++)
            {
                jobs[i].Handle.Complete();
                VoxelChunk c = updatedChunks[i];

                c.fillState = jobs[i].JobData.fillState[0];
                c.fillMaterial = jobs[i].JobData.fillMat[0];

                if (c.fillState == ChunkFillState.Empty || c.fillState == ChunkFillState.SolidSingleMaterial)
                {
                    c.Dispose();
                    //Debug.LogFormat("Non mixed chunk: {0}", c.fillState.ToString());
                }

                jobs[i].JobData.fillState.Dispose();
                jobs[i].JobData.fillMat.Dispose();
            }

            sw.Stop();
            //Debug.LogFormat("Check fill state: {0} ms", sw.Elapsed.TotalMilliseconds);
        }

        [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
        private struct UpdateFillStateJob : IJob
        {
            [ReadOnly]
            public NativeArray<float> chunkData;
            [ReadOnly]
            public NativeArray<byte> chunkMatData;
            [ReadOnly]
            public int chunkDataLength;
            [ReadOnly]
            public int3 chunkIndex;

            public NativeArray<ChunkFillState> fillState;
            public NativeArray<byte> fillMat;

            //TODO: vectorize this
            public void Execute()
            {
                if (chunkData[0] > 0.0f)
                {
                    //If there is one air voxel, we only need to check if all voxels are air
                    for (int i = 0; i < chunkDataLength; i++)
                    {
                        if (chunkData[i] <= 0.0f)
                        {
                            fillState[0] = ChunkFillState.Mixed;
                            fillMat[0] = 0;
                            return;
                        }
                    }

                    fillState[0] = ChunkFillState.Empty;
                    fillMat[0] = 0;
                }
                else
                {
                    byte m = chunkMatData[0];
                    int i = 0;
                    for (; i < chunkDataLength; i++)
                    {
                        if (chunkData[i] > 0.0f)
                        {
                            //If we found an empty voxel, this must be a mixed chunk
                            fillState[0] = ChunkFillState.Mixed;
                            fillMat[0] = 0;
                            return;
                        }
                        if (chunkMatData[i] != m)
                        {
                            i++;

                            //If we found a voxel of a different material, it could still be a solid or a mixed chunk
                            for (; i < chunkDataLength; i++)
                            {
                                if (chunkData[i] > 0.0f)
                                {
                                    fillState[0] = ChunkFillState.Mixed;
                                    fillMat[0] = 0;
                                    return;
                                }
                            }

                            fillState[0] = ChunkFillState.Solid;
                            fillMat[0] = 0;
                            return;
                        }
                    }

                    fillState[0] = ChunkFillState.SolidSingleMaterial;
                    fillMat[0] = m;
                }
            }
        }

        //Explicitly fills chunk data for chunks that have a dataless fill state
        public static void ForceChunkData(IEnumerable<VoxelChunk> chunks)
        {
            List<VoxelChunk> updatedChunks = new List<VoxelChunk>();
            List<JobWrapper<ForceChunkDataJob>> jobs = new List<JobWrapper<ForceChunkDataJob>>();
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

            sw.Start();
            foreach (VoxelChunk c in chunks)
            {
                //Skip chunks that already have data
                if (c.hasData) { continue; }
                c.Allocate();

                updatedChunks.Add(c);

                var job = new ForceChunkDataJob();
                job.chunkData = c.data;
                job.chunkMatData = c.material;
                job.chunkDataLength = Constants.ChunkIndexSize;
                job.value = (c.fillState == ChunkFillState.Empty) ? c.grid.maxClamp : c.grid.minClamp;
                job.mat = c.fillMaterial;
                jobs.Add(new JobWrapper<ForceChunkDataJob>(job));
                jobs[jobs.Count - 1].Schedule();
            }

            for (int i = 0; i < jobs.Count; i++)
            {
                jobs[i].Handle.Complete();
            }

            sw.Stop();
            //Debug.LogFormat("Fill state data: {0} ms", sw.Elapsed.TotalMilliseconds);
        }

        [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
        private struct ForceChunkDataJob : IJob
        {
            [WriteOnly]
            public NativeArray<float> chunkData;
            [WriteOnly]
            public NativeArray<byte> chunkMatData;

            [ReadOnly]
            public int chunkDataLength;
            [ReadOnly]
            public float value;
            [ReadOnly]
            public byte mat;

            //TODO: vectorize this
            public void Execute()
            {
                for (int i = 0; i < chunkDataLength; i++)
                {
                    chunkData[i] = value;
                    chunkMatData[i] = mat;
                }
            }
        }
    }
}
