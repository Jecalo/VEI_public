using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Constants
{
    public const int ChunkResolution = 32;
    public const int ResolutionStep = ChunkResolution * ChunkResolution;
    public const int ChunkIndexSize = ChunkResolution * ChunkResolution * ChunkResolution;
    public const int ChunkResolution1 = ChunkResolution - 1;
    public const int ChunkResolution2 = ChunkResolution - 2;

    public const string ProcMeshName = "procMesh";

    public const float DefaultTerrainChangeSize = 2.0f;
    public const byte DefaultTerrainChangeMaterial = 2;

#if UNITY_EDITOR
    public static readonly string TempSaves = UnityEngine.Windows.Directory.temporaryFolder;
#else
    public static readonly string TempSaves = Application.temporaryCachePath;
#endif
}
