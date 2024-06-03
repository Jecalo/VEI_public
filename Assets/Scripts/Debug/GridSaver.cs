using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using System.IO;
using Unity.VisualScripting;

public static class GridSaver
{
    public static void Save(VoxelTerrain.VoxelGrid grid, string path)
    {
        System.Diagnostics.Stopwatch sw = new();
        sw.Start();

        using FileStream stream = File.Open(path, FileMode.Create);
        using BinaryWriter writer = new BinaryWriter(stream);

        writer.Write(grid.chunkRes);
        writer.Write(grid.chunks.Count);
        foreach (var c in grid.chunks.Values)
        {
            writer.Write(c.index.x);
            writer.Write(c.index.y);
            writer.Write(c.index.z);

            byte[] data = c.data.Reinterpret<byte>(4).ToArray();
            byte[] mats = c.material.ToArray();
            writer.Write(data);
            writer.Write(mats);
        }

        sw.Stop();
        Debug.LogFormat("Grid saved ({0}ms)", sw.Elapsed.TotalMilliseconds);
    }

    public static bool Load(VoxelTerrain.VoxelGrid grid, string path)
    {
        if (!File.Exists(path)) { Debug.LogWarningFormat("Voxel data not found at: {0}", path); return false; }

        System.Diagnostics.Stopwatch sw = new();
        sw.Start();

        using FileStream stream = File.Open(path, FileMode.Open);
        using BinaryReader reader = new BinaryReader(stream);

        grid.RemoveAllChunks();

        int res = reader.ReadInt32();
        int size = res * res * res;
        int chunkCount = reader.ReadInt32();

        if (res != grid.chunkRes)
        {
            reader.Close();
            stream.Close();
            Debug.LogWarningFormat("Grid resolution mismatch: {0} -> {1}", grid.chunkRes, res);
            return false;
        }

        for(int i = 0; i < chunkCount; i++)
        {
            int3 index;
            index.x = reader.ReadInt32();
            index.y = reader.ReadInt32();
            index.z = reader.ReadInt32();
            var c = grid.AddChunk(index);

            var data = reader.ReadBytes(size * 4);
            var mats = reader.ReadBytes(size);

            c.data.Reinterpret<byte>(4).CopyFrom(data);
            c.material.CopyFrom(mats);
        }

        sw.Stop();
        Debug.LogFormat("Grid loaded ({0}ms)", sw.Elapsed.TotalMilliseconds);

        return true;
    }

    public static void SavePath(List<Vector3> a, string path)
    {
        using FileStream stream = File.Open(path, FileMode.Create);
        using BinaryWriter writer = new BinaryWriter(stream);

        writer.Write("path");
        writer.Write(a.Count);
        foreach (var p in a)
        {
            writer.Write(p.x);
            writer.Write(p.y);
            writer.Write(p.z);
        }
    }

    public static bool LoadPath(List<Vector3> a, string path)
    {
        if (!File.Exists(path)) { Debug.LogWarningFormat("Pathfinding data not found at: {0}", path); return false; }

        using FileStream stream = File.Open(path, FileMode.Open);
        using BinaryReader reader = new BinaryReader(stream);

        string header = reader.ReadString();

        if (header != "path") { Debug.LogErrorFormat("Wrong or corrupt pathfinding data file: {0}", path); return false; }

        int size = reader.ReadInt32();
        a.Clear();

        for (int i = 0; i < size; i++)
        {
            Vector3 p;
            p.x = reader.ReadSingle();
            p.y = reader.ReadSingle();
            p.z = reader.ReadSingle();
            a.Add(p);
        }

        return true;
    }
}
