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
using Unity.Collections.LowLevel.Unsafe;
using System.IO;

public class VoxelTerrainTest : MonoBehaviour
{
    [SerializeField]
    private float chunkSize = 16.0f;
    [SerializeField]
    private Vector3Int gridSize;
    [SerializeField]
    private GameObject ChunkPrefab = null;
    [SerializeField]
    private bool resetTransformAtStart = false;
    [SerializeField]
    private bool loadMapAtStart = false;
    [SerializeField]
    private bool saveMapOnExit = false;

    [HideInInspector]
    public VoxelTerrain.VoxelGrid grid = null;


    private void Start()
    {
        if(resetTransformAtStart)
        {
            gameObject.transform.position = Vector3.zero;
            gameObject.transform.rotation = Quaternion.identity;
            gameObject.transform.localScale = Vector3.one;
        }

        grid = new VoxelTerrain.VoxelGrid();

        grid.chunkRes = Constants.ChunkResolution;
        grid.chunkSize = chunkSize;
        grid.ChunkPrefab = ChunkPrefab;
        grid.GridParent = gameObject;

        grid.t = 0.0f;
        grid.r = 0.0f;
        grid.s = 1.0f;

        grid.Initialize();
        VoxelManager.AddGrid(grid);

        if (loadMapAtStart)
        {
            bool r = GridSaver.Load(grid, Path.Combine(Constants.TempSaves, "grid.bin"));
            if (r) { return; }
        }

        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                for (int z = 0; z < gridSize.z; z++)
                {
                    grid.AddChunk(new int3(x, y, z));
                }
            }
        }

        BaseGeneration();
    }

    private void Update()
    {
        grid.SetTRS(transform);
    }

    private void OnDestroy()
    {
        if (saveMapOnExit) { GridSaver.Save(grid, Path.Combine(Constants.TempSaves, "grid.bin")); }
        grid.Dispose();
    }

    public void BaseGeneration()
    {
        VoxelTerrain.TerrainGeneration.FuncFill(grid, (float3 p) => { return (p.y - 10.0f) * 1f; });
        VoxelTerrain.TerrainGeneration.FillMat(grid, 1);
        //VoxelTerrain.TerrainGeneration.FuncFillMat(grid, (float3 p) => { int i = (int)((p.x) / 4.0f) % 2 + 1; return (byte)i; });

        //VoxelTerrain.TerrainGeneration.SimpleNoise(grid, 0.25f);
        //VoxelTerrain.TerrainGeneration.FillMat(grid, 1);

        //foreach (var c in grid.chunks.Values) { VoxelTerrain.TerrainGeneration.TestFill(c); }

        //VoxelTerrain.TerrainGeneration.DefaultTerrainGeneration(grid.chunks.Values);

        VoxelTerrain.VoxelGrid.TryRemoveChunkData(grid.chunks.Values);

        grid.SetDirtyAll();
    }
}
