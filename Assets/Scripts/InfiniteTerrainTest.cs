using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEditor;
using VoxelTerrain;
using UnityEngine.InputSystem;

public class InfiniteTerrainTest : MonoBehaviour
{
    [SerializeField]
    private int chunkBatchesPerFrame = 1;
    [SerializeField]
    private float chunkSize = 16.0f;
    [SerializeField]
    private int chunkRadius = 1;
    [SerializeField]
    private GameObject ChunkPrefab = null;


    public VoxelGrid grid = null;

    private PlayerController player = null;
    private int3 chunkCenter;

    private int chunkBatchSize = 0;


    private void Awake()
    {
        
    }

    private void Start()
    {
        chunkBatchSize = JobsUtility.JobWorkerCount;

        grid = new VoxelGrid();

        grid.chunkRes = Constants.ChunkResolution;
        grid.chunkSize = chunkSize;
        grid.ChunkPrefab = ChunkPrefab;
        grid.GridParent = gameObject;

        grid.SetTRS(transform);

        grid.Initialize();
        VoxelManager.AddGrid(grid);


        player = FindObjectOfType<PlayerController>(true);
        chunkCenter = grid.WorldToChunkIndex(player.transform.position);
        int3 start = chunkCenter - chunkRadius;
        int3 end = chunkCenter + chunkRadius;
        for (int x = start.x; x <= end.x; x++)
        {
            for (int y = start.y; y <= end.y; y++)
            {
                for (int z = start.z; z <= end.z; z++)
                {
                    grid.AddChunk(new int3(x, y, z));
                }
            }
        }

        TerrainGeneration.DefaultTerrainGeneration(grid.chunks.Values);
        VoxelGrid.TryRemoveChunkData(grid.chunks.Values);

        grid.SetDirtyAll();
    }

    private void Update()
    {
        float3 playerPos = player.transform.position;
        int3 ci = grid.WorldToChunkIndex(playerPos);

        System.Diagnostics.Stopwatch sw = new();
        sw.Start();

        List<Tuple<float, VoxelChunk>> outdatedChunks = new ();
        int3 start = ci - chunkRadius;
        int3 end = ci + chunkRadius;

        //Get all chunks that are too far away
        foreach (var c in grid.chunks.Values)
        {
            if (!BurstHelper.IsContained(c.index, start, end))
            {
                float3 chunkCenter = grid.ChunkIndexToWorld_Center(c.index);
                float dist = math.distance(playerPos, chunkCenter);
                outdatedChunks.Add(new(dist, c));
            }
        }

        if (outdatedChunks.Count == 0) { return; }

        //Get all missing chunks for the current position
        List<Tuple<float, int3>> missingChunks = new();
        for (int x = start.x; x <= end.x; x++)
        {
            for (int y = start.y; y <= end.y; y++)
            {
                for (int z = start.z; z <= end.z; z++)
                {
                    int3 key = new int3(x, y, z);

                    if (!grid.chunks.ContainsKey(key))
                    {
                        float3 chunkCenter = grid.ChunkIndexToWorld_Center(key);
                        float dist = math.distance(playerPos, chunkCenter);
                        missingChunks.Add(new(dist, key));
                    }
                }
            }
        }

        //Sort old chunks so we update the farthest away first,
        //and missing chunks so we add the closest one first
        outdatedChunks.Sort((a, b) => a.Item1.CompareTo(b.Item1));
        missingChunks.Sort((a, b) => b.Item1.CompareTo(a.Item1));

        int batchCount = 0;
        List<VoxelChunk> updatedChunks = new();
        while (outdatedChunks.Count > 0)
        {
            if (batchCount >= chunkBatchesPerFrame) { break; }

            updatedChunks.Clear();

            for (int i = 0; i < chunkBatchSize; i++)
            {
                if (outdatedChunks.Count == 0) { break; }
                VoxelChunk c = outdatedChunks[outdatedChunks.Count - 1].Item2;
                int3 newIndex = missingChunks[missingChunks.Count - 1].Item2;
                outdatedChunks.RemoveAt(outdatedChunks.Count - 1);
                missingChunks.RemoveAt(missingChunks.Count - 1);

                grid.ChangeChunk(c.index, newIndex);
                grid.dirtyChunks.Add(c);
                updatedChunks.Add(c);
            }

            VoxelGrid.ForceChunkData(updatedChunks);
            TerrainGeneration.DefaultTerrainGeneration(updatedChunks);
            VoxelGrid.TryRemoveChunkData(updatedChunks);

            batchCount++;
        }

        chunkCenter = ci;

        sw.Stop();
        DebugHelper.AddMsg(string.Format("Terrain regen ({0} batches, {1} remaining chunks): {2} ms",
            batchCount, outdatedChunks.Count, sw.Elapsed.TotalMilliseconds));
    }

    private void OnDestroy()
    {
        grid.Dispose();
    }
}
