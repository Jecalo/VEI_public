using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using TMPro;
using Unity.Collections.LowLevel.Unsafe;
using System.Linq;
using Unity.VisualScripting;

public class AstarTester : MonoBehaviour
{
    public enum Algorithm { Simple, Simple2, JPS }

    [SerializeField]
    Algorithm algorithm = Algorithm.Simple;
    [SerializeField]
    private bool show = true;
    [SerializeField]
    private bool showSaved = true;

    public bool solveOnce = false;
    public bool autoSolve = false;
    public bool savePath = false;

    [SerializeField]
    private GameObject target = null;

    private int3[] result;
    private List<Vector3> path, savedPath;


    private void Awake()
    {

    }

    private void Start()
    {
        path = new List<Vector3>();
        result = new int3[0];
        savedPath = new List<Vector3>();

        GridSaver.LoadPath(savedPath, Constants.TempSaves + "astarPath.bin");
    }
    private void OnDestroy()
    {

    }

    private void Update()
    {
        if (savePath)
        {
            if (path.Count != 0)
            {
                GridSaver.SavePath(path, Constants.TempSaves + "astarPath.bin");
                savedPath = new List<Vector3>(path);
            }
            savePath = false;
        }

        if (autoSolve || solveOnce)
        {
            VoxelTerrain.VoxelGrid grid = VoxelManager.GetFirstGrid();
            int3 start = grid.WorldToVoxelIndex(transform.position);
            int3 end = grid.WorldToVoxelIndex(target.transform.position);

            switch (algorithm)
            {
                case Algorithm.Simple:
                    result = SimplePathfinder.GetPath(grid.chunks[0].data, grid.chunkRes, start, end);
                    break;
                case Algorithm.Simple2:
                    result = SimplePathfinder2.GetPath(grid.chunks[0].data, grid.chunkRes, start, end);
                    break;
                default:
                    Debug.LogError("Unknown algorithm.");
                    break;
            }
            

            WorldPosPath(grid, result, path);
            solveOnce = false;
        }

        if (show) { Draw(path, Color.red); }
        if (showSaved) { Draw(savedPath, Color.blue); }
    }

    private void Draw(List<Vector3> path, Color color, bool points = false)
    {
        if (path.Count == 0) { return; }
        Gizmo.DrawPolyline(color, path);
        if (points)
        {
            foreach (Vector3 point in path) { Gizmo.DrawSphere(point, 0.1f, color); }
        }
    }

    private void WorldPosPath(VoxelTerrain.VoxelGrid grid, int3[] indexes, List<Vector3> path)
    {
        path.Clear();
        foreach (int3 p in indexes)
        {
            path.Add(grid.IndexToWorld(p));
        }
    }
}
