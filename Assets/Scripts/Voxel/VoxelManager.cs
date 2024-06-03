using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Rendering;
using Unity.Jobs.LowLevel.Unsafe;


//Contains a reference to all grids, so that they are automatically updated each frame.
//It also allows applying destructive or constructive effects to all grids at the same time.
//This class must be run after the default time in the script execution order
//It does not create or destroy grids on its own
public class VoxelManager : MonoBehaviour
{
    [SerializeField]
    private Material[] voxelMaterials = null;

    public static VoxelManager Instance { get; private set; } = null;

    private HashSet<VoxelTerrain.VoxelGrid> grids = null;

    private bool resetGrids = false;

    private void Awake()
    {
        if (Instance != null)
        {
#if !UNITY_EDITOR
            Debug.LogWarning("Voxel Manager already instanced.");
#endif
            Destroy(this);
        }
        else { Initialize(); }
    }

    private void Initialize()
    {
        Instance = this;
        grids = new();
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (resetGrids) { Instance.grids.Clear(); resetGrids = false; }

        foreach (var grid in grids)
        {
            grid.RegenerateDirtyNow();
        }
    }

    public static Material GetMaterial(byte materialIndex)
    {
        if (Instance == null) { Debug.LogError("No voxel manager instanced."); return null; }
        if (materialIndex == 0) { Debug.LogWarning("Requested air material (index 0)"); }
        if (materialIndex >= Instance.voxelMaterials.Length) { Debug.LogWarning("Requested material does not exist"); }
        return Instance.voxelMaterials[materialIndex];
    }

    public static void AddGrid(VoxelTerrain.VoxelGrid grid)
    {
        if (Instance == null) { Debug.LogError("No voxel manager instanced."); return; }
        bool r = Instance.grids.Add(grid);
        if (!r) { Debug.LogWarning("Voxel grid was added twice."); }
    }

    public static void RemoveGrid(VoxelTerrain.VoxelGrid grid)
    {
        if (Instance == null) { Debug.LogError("No voxel manager instanced."); return; }
        bool r = Instance.grids.Remove(grid);
        if (!r) { Debug.LogWarning("Voxel grid was not present."); }
    }

    public static HashSet<VoxelTerrain.VoxelGrid>.Enumerator GetGrids()
    {
        if (Instance == null) { Debug.LogError("No voxel manager instanced."); return new HashSet<VoxelTerrain.VoxelGrid>.Enumerator(); }
        return Instance.grids.GetEnumerator();
    }

    public static VoxelTerrain.VoxelGrid GetFirstGrid()
    {
        if (Instance == null) { Debug.LogError("No voxel manager instanced."); return null; }
        if (Instance.grids.Count == 0) { return null; }
        var e = Instance.grids.GetEnumerator();
        e.MoveNext();
        return e.Current;
    }

    public static void RegenerateAllGrids()
    {
        if (Instance == null) { Debug.LogError("No voxel manager instanced."); return; }
        foreach (var grid in Instance.grids)
        {
            grid.SetDirtyAll();
        }
    }


    public static void ChangeTerrain(Vector3 pos, bool erase, float size, byte material = 0)
    {
        if (Instance == null) { Debug.LogError("No voxel manager instanced."); return; }
        foreach (var grid in Instance.grids)
        {
            VoxelTerrain.Kernel kernel = VoxelTerrain.KernelBuilder.Sphere(grid, pos, size,
                new VoxelTerrain.KernelConfig(erase ? VoxelTerrain.KernelMode.Remove : VoxelTerrain.KernelMode.Add), erase ? (byte)0 : material);
            VoxelTerrain.KernelHelper.ApplyKernel(grid, kernel);
            kernel.Dispose();
        }
    }

    public static void SpeckTerrain(Vector3 pos, bool erase, byte material = 0)
    {
        throw new NotImplementedException();

        //if (Instance == null) { Debug.LogError("No voxel manager instanced."); return; }
        //foreach (var grid in Instance.grids)
        //{
        //    VoxelTerrain.Kernel kernel = VoxelTerrain.KernelBuilder.Speck(grid, pos,
        //        new VoxelTerrain.KernelConfig(erase ? VoxelTerrain.KernelMode.Remove : VoxelTerrain.KernelMode.Add), erase ? (byte)0 : material);
        //    VoxelTerrain.KernelHelper.ApplyKernel(grid, kernel);
        //    kernel.Dispose();
        //}
    }

    public static void SceneChanged()
    {
        if (Instance == null) { Debug.LogError("No voxel manager instanced."); return; }
        Instance.resetGrids = true;
    }
}
