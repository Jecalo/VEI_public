using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using VoxelTerrain;
using Unity.VisualScripting;

public static class ConsoleCommands
{
    public static void FillVars()
    {
        UConsole.UConsole console = ConsoleController.GetConsole();

        object o;

        o = GameObject.FindObjectOfType<PlayerController>();
        if (o != null) { console.StoredVars["player"] = o; }

        o = Camera.main;
        if (o != null) { console.StoredVars["camera"] = o; }
    }

    public static void Panic()
    {
        PlayerController player = DebugHelper.GetPlayer();
        player.transform.position = Vector3.zero;
        player.GetComponentInChildren<Rigidbody>().velocity = Vector3.zero;
        GameObject.CreatePrimitive(PrimitiveType.Plane).transform.position = Vector3.zero;
    }

    public static void AddFreeCameraController()
    {
        Camera cam = Camera.main;
        if (cam != null && cam.GetComponent<CameraControllerFree>() == null)
        {
            cam.AddComponent<CameraControllerFree>();
        }
    }

    public static void RestartScene()
    {
        Director.Reload();
    }

    public static PlayerController Player()
    {
        return DebugHelper.GetPlayer();
    }

    public static Vector3 PlayerPos()
    {
        return DebugHelper.GetPlayer().transform.position;
    }

    public static void SetPlayerPos(Vector3 pos)
    {
        DebugHelper.GetPlayer().transform.position = pos;
    }

    public static void TerrainRegen()
    {
        VoxelManager.RegenerateAllGrids();
    }

    public static void TerrainSave()
    {
        GridSaver.Save(VoxelManager.GetFirstGrid(), Constants.TempSaves + "grid.bin");
    }

    public static void TerrainSave(string path)
    {
        GridSaver.Save(VoxelManager.GetFirstGrid(), path);
    }

    public static void TerrainLoad()
    {
        GridSaver.Load(VoxelManager.GetFirstGrid(), Constants.TempSaves + "grid.bin");
    }

    public static void TerrainLoad(string path)
    {
        GridSaver.Load(VoxelManager.GetFirstGrid(), path);
    }

    public static void FillChunk(Vector3Int chunk, int mat)
    {
        if (mat < 0) { return; }

        var grid = VoxelManager.GetFirstGrid();
        int3 i = new int3(chunk.x, chunk.y, chunk.z);
        Kernel kernel = KernelBuilder.Fill(grid, i * grid.chunkRes2, (i + 1) * grid.chunkRes2, new KernelConfig(KernelMode.Overwrite), (byte)mat);
        KernelHelper.ApplyKernel(grid, kernel);
        kernel.Dispose();
    }

    public static void Fill(Vector3 start, Vector3 end, int mat)
    {
        if (mat < 0) { return; }

        var grid = VoxelManager.GetFirstGrid();
        float3 extents = math.abs(end - start);
        float3 center = (start + end) / 2f;
        Kernel kernel = KernelBuilder.Box(grid, center, 0f, extents, new KernelConfig(KernelMode.Overwrite), (byte)mat);
        KernelHelper.ApplyKernel(grid, kernel);
        kernel.Dispose();
    }

    public static void DefaultNormals()
    {
        foreach (var c in VoxelManager.GetFirstGrid().chunks.Values)
        {
            if (c.hasData) { c.mesh.RecalculateNormals(); }
        }
    }
}