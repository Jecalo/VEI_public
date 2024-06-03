using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Jobs.LowLevel.Unsafe;

public class DebugHelper : MonoBehaviour
{
    [SerializeField]
    public bool log = true;
    [SerializeField]
    public bool logNewLines = true;

    public bool showGizmos = false;
    public bool showMarkers = true;
    public bool drawChunks = true;
    public bool drawVoxels = false;
    public bool alignToMesh = false;
    public bool drawFilledVoxels = false;
    public int voxelDrawRadius = 3;

    public Color markerColor = Color.red;

    public static DebugHelper Instance { get; private set; } = null;
    private bool Initialized = false;

    private PlayerController player;

    private List<string> messages = null;
    private System.Text.StringBuilder str = null;

    private List<Vector3> markers = null;
    private List<Tuple<Vector3, Vector3>> markerLines = null;


    private static readonly Color ChunkColor = new Color(1f, 0.92f, 0.016f, 1.0f);


    private void Initialize()
    {
        if (Initialized) { return; }

        Initialized = true;
        Instance = this;
        messages = new List<string>(10);
        str = new();
        markers = new();
        markerLines = new();
        DontDestroyOnLoad(gameObject);
        //debugMenu = Instantiate(debugMenuPrefab, FindObjectOfType<Canvas>().transform).GetComponent<DebugMenu>();

#if !UNITY_EDITOR
        if (log)
        {
            System.Text.StringBuilder sb = new();
            sb.Append(DateTime.Now);
            sb.Append('\n');
            sb.Append(UnityEngine.SystemInfo.operatingSystem);
            sb.Append('\n');
            sb.Append(UnityEngine.SystemInfo.deviceModel);
            sb.Append('\n');
            sb.AppendFormat("Job worker threads: {0}/{1}", JobsUtility.JobWorkerCount, JobsUtility.JobWorkerMaximumCount);
            sb.Append('\n');
            sb.Append("RAM " + UnityEngine.SystemInfo.systemMemorySize + "MB");
            sb.Append('\n');
            sb.Append(UnityEngine.SystemInfo.processorType);
            sb.Append('\n');
            sb.Append(UnityEngine.SystemInfo.graphicsDeviceName);
            Debug.Log(sb);
        }
#endif
    }

    private void Awake()
    {
        if (Instance != null)
        {
#if !UNITY_EDITOR
            Debug.LogWarning("Debug Helper already instanced.");
#endif
            Destroy(this);
        }
        else { Initialize(); }
    }

    private void Start()
    {
        SceneChanged();
    }

    private void Update()
    {
        if (log)
        {
            if (messages.Count != 0)
            {
                str.Clear();
                foreach (var s in messages)
                {
                    str.Append(s);
                    if(logNewLines) { str.Append('\n'); }
                    else { str.Append(" | "); }
                }
                if (logNewLines) { str.Remove(str.Length - 1, 1); }
                else { str.Remove(str.Length - 3, 3); }
                messages.Clear();
                Debug.Log(str);
            }
        }
        else { messages.Clear(); }


        if (!showGizmos) { return; }
        var e = VoxelManager.GetGrids();
        e.MoveNext();
        VoxelTerrain.VoxelGrid grid = e.Current;
        if (grid == null) { return; }

        if (drawChunks)
        {
            Vector3 extents = new Vector3(grid.chunkDisplacement, grid.chunkDisplacement, grid.chunkDisplacement);
            Vector3 halfSize = grid.GridParent.transform.TransformVector(extents / 2f);
            foreach (var chunk in grid.chunks)
            {
                Gizmo.DrawCube(halfSize + chunk.Value.gameObject.transform.position, extents, chunk.Value.gameObject.transform.rotation, ChunkColor);
            }
        }

        if (drawVoxels)
        {
            Vector3 voxel = new Vector3(grid.voxelSize, grid.voxelSize, grid.voxelSize);
            float3 halfVoxel = 0.5f * voxel;
            Transform cam = Camera.main.transform;
            Vector3 rot = grid.GridParent.transform.rotation.eulerAngles;
            if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit))
            {
                int3 start;
                if (alignToMesh) { start = grid.LocalToVoxelIndex(grid.WorldToLocal(hit.point) + halfVoxel) - voxelDrawRadius / 2; }
                else { start = grid.WorldToVoxelIndex(hit.point) - voxelDrawRadius / 2; }
                int3 end = start + voxelDrawRadius;

                for (int i = start.x; i < end.x; i++)
                {
                    for (int j = start.y; j < end.y; j++)
                    {
                        for (int k = start.z; k < end.z; k++)
                        {
                            int3 index = new int3(i, j, k);
                            if (alignToMesh)
                            {
                                Gizmo.DrawCube(grid.LocalToWorld((float3)index * grid.voxelSize), voxel, rot, Color.green);
                            }
                            else
                            {
                                Gizmo.DrawCube(grid.LocalToWorld((float3)index * grid.voxelSize + halfVoxel), voxel, rot, Color.green);
                            }
                        }
                    }
                }
            }
        }

        if (drawFilledVoxels)
        {
            Vector3 voxel = new Vector3(grid.voxelSize, grid.voxelSize, grid.voxelSize);
            float3 halfVoxel = 0.5f * voxel;
            Transform cam = Camera.main.transform;
            Vector3 rot = grid.GridParent.transform.rotation.eulerAngles;
            if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit))
            {
                int3 start;
                if (alignToMesh) { start = grid.LocalToVoxelIndex(grid.WorldToLocal(hit.point) + halfVoxel) - voxelDrawRadius / 2; }
                else { start = grid.WorldToVoxelIndex(hit.point) - voxelDrawRadius / 2; }
                int3 end = start + voxelDrawRadius;

                for (int i = start.x; i < end.x; i++)
                {
                    for (int j = start.y; j < end.y; j++)
                    {
                        for (int k = start.z; k < end.z; k++)
                        {
                            int3 index = new int3(i, j, k);
                            int3 localIndex, chunkIndex;

                            grid.IndexToChunkIndex(index, out localIndex, out chunkIndex);
                            int dataIndex = localIndex.x * grid.chunkResStep + localIndex.y * grid.chunkRes + localIndex.z;

                            if (!grid.chunks.ContainsKey(chunkIndex) || grid.chunks[chunkIndex].data[dataIndex] > 0f) { continue; }

                            if (alignToMesh)
                            {
                                Gizmo.DrawSolidCube(grid.LocalToWorld((float3)index * grid.voxelSize), voxel, Quaternion.Euler(rot), Color.grey);
                            }
                            else
                            {
                                Gizmo.DrawSolidCube(grid.LocalToWorld((float3)index * grid.voxelSize + halfVoxel), voxel, Quaternion.Euler(rot), Color.grey);
                            }
                        }
                    }
                }
            }
        }

        if (showMarkers)
        {
            foreach (var m in markers)
            {
                Gizmo.DrawSphere(m, 0.25f, markerColor);
            }

            foreach (var l in markerLines)
            {
                Gizmo.DrawLine(l.Item1, l.Item2, markerColor);
            }
        }
    }

    public static void AddMsg(string msg, int index = -1)
    {
        if (Instance == null) { Debug.LogError("No debug helper instanced."); }
        if (index < 0 || index >= Instance.messages.Count) { Instance.messages.Add(msg); }
        else { Instance.messages.Insert(index, msg); }
    }

    public static void SceneChanged()
    {
        if (Instance == null) { Debug.LogError("No debug helper instanced."); }
        Instance.player = FindObjectOfType<PlayerController>();
    }

    public static PlayerController GetPlayer()
    {
        if (Instance == null) { Debug.LogError("No debug helper instanced."); }
        return Instance.player;
    }

    public static void ClearMarkers()
    {
        if (Instance == null) { Debug.LogError("No debug helper instanced."); }
        Instance.markers.Clear();
    }

    public static void AddMarker(Vector3 marker)
    {
        if (Instance == null) { Debug.LogError("No debug helper instanced."); }
        Instance.markers.Add(marker);
    }

    public static void AddMarkerLine(Vector3 a, Vector3 b)
    {
        if (Instance == null) { Debug.LogError("No debug helper instanced."); }
        Instance.markerLines.Add(new Tuple<Vector3, Vector3>(a, b));
    }
}
