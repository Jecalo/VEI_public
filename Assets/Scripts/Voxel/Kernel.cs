using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace VoxelTerrain
{
    //Data that represents a change to be applied to a given grid.
    //It is not bounded to the volume of the grid.
    public struct Kernel
    {
        public KernelConfig config;             //Configuration
        public int3 indexOffset;                //Offset of the lowest voxel corner
        public int3 size;                       //Size of the kernel cube in voxels

        public NativeArray<float> data;         //Data
        public NativeArray<byte> matData;       //Material data (see complexMaterial member)

        public bool complexMaterial;            //If false, material data has length 1, and all replacements are made using that one material.
        public bool emptyKernel;                //Whether this kernel is empty. Empty kernels are completely skipped, and must not have any allocated data.

        public void Dispose()
        {
            if (emptyKernel) { return; }
            data.Dispose();
            matData.Dispose();
        }
    }

    //Modes for kernel application
    public enum KernelMode
    {
        None = 0,       //Null mode
        Add,            //Union of sdf/voxels
        Remove,         //Substraction of sdf/voxels
        Overwrite,      //Replace all voxels with exactly those of the kernel
        SwapMat,        //Replace the material on negative voxels in the chunk with the material of negative voxels in the kernel
        Intersection,   //Intersection of sdf/voxels
    }

    //Kernel configuration
    public struct KernelConfig
    {
        public KernelMode mode;     //Kernel mode

        public KernelConfig(KernelMode mode)
        {
            this.mode = mode;
        }
    }

    //Kernel operations
    public static class KernelHelper
    {
        //Struct containing information that describes a slice of kernel corresponding to one chunk.
        //It does not contain any data, and must only be used on the kernel and the chunk it was originally made from.
        private struct KernelPiece
        {
            public int3 chunkIndex;                 //Index of the chunk this piece belongs to within the voxel grid.
            public int3 localIndexStart;            //Lowest index of this piece within its chunk.
            public int3 kernelIndexStart;           //Lowest index of this piece within the kernel.
            public int3 size;                       //Size of the data to be copied to the chunk.
        }

        //Apply the kernel to the given grid.
        public static void ApplyKernel(VoxelGrid grid, Kernel kernel)
        {
            if (grid == null || kernel.emptyKernel) { return; }

            System.Diagnostics.Stopwatch sw = new();
            sw.Start();

            List<KernelPiece> pieces = SplitKernel(grid, kernel);

            List<JobWrapper<KernelApplyJob>> jobs = new(pieces.Count);

            int chunkStep = grid.chunkRes * grid.chunkRes;

            for(int i = pieces.Count - 1; i >= 0; i--)
            {
                if (!grid.chunks.ContainsKey(pieces[i].chunkIndex))
                {
                    pieces.RemoveAt(i);
                }
            }

            List<VoxelChunk> updatedChunks = new();
            foreach (KernelPiece piece in pieces)
            {
                updatedChunks.Add(grid.chunks[piece.chunkIndex]);
            }

            //Make sure all affected chunks have data
            VoxelGrid.ForceChunkData(updatedChunks);

            foreach (KernelPiece piece in pieces)
            {
                VoxelChunk c = grid.chunks[piece.chunkIndex];

                KernelApplyJob job = new KernelApplyJob();
                job.chunkData = c.data;
                job.chunkMatData = c.material;
                job.kernelData = kernel.data;
                job.kernelMatData = kernel.matData;
                job.complexMat = kernel.complexMaterial;
                job.piece = piece;
                job.config = kernel.config;
                job.chunkRes = grid.chunkRes;
                job.chunkStep = chunkStep;
                job.kernelResZ = kernel.size.z;
                job.kernelStepYZ = kernel.size.y * kernel.size.z;

                jobs.Add(new JobWrapper<KernelApplyJob>(job));
                jobs[jobs.Count - 1].Schedule();

                grid.dirtyChunks.Add(c);
            }

            foreach (var job in jobs)
            {
                job.Handle.Complete();
            }

            //Update fill state (and remove redundant data)
            VoxelGrid.TryRemoveChunkData(updatedChunks);

            sw.Stop();
            DebugHelper.AddMsg(string.Format("Kernel apply: {0}ms", sw.Elapsed.TotalMilliseconds));
        }


        [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
        private struct KernelApplyJob : IJob
        {
            public NativeArray<float> chunkData;
            [WriteOnly]
            public NativeArray<byte> chunkMatData;

            [ReadOnly]
            public NativeArray<float> kernelData;
            [ReadOnly]
            public NativeArray<byte> kernelMatData;

            [ReadOnly]
            public bool complexMat;

            [ReadOnly]
            public KernelPiece piece;
            [ReadOnly]
            public KernelConfig config;
            [ReadOnly]
            public int chunkRes, chunkStep;
            [ReadOnly]
            public int kernelResZ, kernelStepYZ;

            public void Execute()
            {
                for (int x = 0; x < piece.size.x; x++)
                {
                    for (int y = 0; y < piece.size.y; y++)
                    {
                        for (int z = 0; z < piece.size.z; z++)
                        {
                            int3 a = new (piece.localIndexStart.x + x, piece.localIndexStart.y + y, piece.localIndexStart.z + z);
                            int3 b = new (piece.kernelIndexStart.x + x, piece.kernelIndexStart.y + y, piece.kernelIndexStart.z + z);
                            int i = a.x * chunkStep + a.y * chunkRes + a.z;
                            int j = b.x * kernelStepYZ + b.y * kernelResZ + b.z;

                            float v = kernelData[j];
                            bool s = (v > 0.0f);
                            bool ts = (chunkData[i] > 0.0f);

                            switch (config.mode)
                            {
                                case KernelMode.Add:
                                    if (v < chunkData[i])
                                    {
                                        chunkData[i] = v;
                                        if (ts) { chunkMatData[i] = complexMat ? kernelMatData[j] : kernelMatData[0]; }
                                    }
                                    break;
                                case KernelMode.Remove:
                                    if (-v > chunkData[i])
                                    {
                                        chunkData[i] = -v;
                                    }
                                    break;
                                case KernelMode.Overwrite:
                                    chunkData[i] = v;
                                    chunkMatData[i] = complexMat ? kernelMatData[j] : kernelMatData[0];
                                    break;
                                case KernelMode.SwapMat:
                                    if (!s && !ts)
                                    {
                                        chunkMatData[i] = complexMat ? kernelMatData[j] : kernelMatData[0];
                                    }
                                    break;
                                case KernelMode.Intersection:
                                    if (v > chunkData[i])
                                    {
                                        chunkData[i] = v;
                                    }
                                    break;
                                case KernelMode.None:
                                default:
                                    break;
                            }
                        }
                    }
                }
            }
        }

        //Split the kernel into pieces, each piece being assigned to a chunk.
        //It makes sure the overlap of data between chunk boundaries is properly copied and kept.
        private static List<KernelPiece> SplitKernel(VoxelGrid grid, Kernel kernel)
        {
            List<KernelPiece> pieces = new List<KernelPiece>();

            int chunkRes = grid.chunkRes;
            int chunkRes1 = grid.chunkRes1;
            int chunkRes2 = grid.chunkRes2;

            int3 localIndex = kernel.indexOffset % chunkRes2;
            int3 chunkIndex = kernel.indexOffset / chunkRes2;
            int3 kernelIndex = new(0, 0, 0);
            int3 localEnd = new();
            int3 kernelIndexDelta;
            int3 nextStart;

            //Adjust starting parameters to handle negative indexes.
            if (localIndex.x < 0) { localIndex.x += chunkRes2; }
            if (localIndex.y < 0) { localIndex.y += chunkRes2; }
            if (localIndex.z < 0) { localIndex.z += chunkRes2; }
            if (kernel.indexOffset.x < 0 && localIndex.x != 0) { chunkIndex.x--; }
            if (kernel.indexOffset.y < 0 && localIndex.y != 0) { chunkIndex.y--; }
            if (kernel.indexOffset.z < 0 && localIndex.z != 0) { chunkIndex.z--; }

            //Make sure we start on the lowest index when at a lower boundary.
            if (localIndex.x == 0) { localIndex.x = chunkRes2; chunkIndex.x--; }
            else if (localIndex.x == 1) { localIndex.x = chunkRes1; chunkIndex.x--; }
            if (localIndex.y == 0) { localIndex.y = chunkRes2; chunkIndex.y--; }
            else if (localIndex.y == 1) { localIndex.y = chunkRes1; chunkIndex.y--; }
            if (localIndex.z == 0) { localIndex.z = chunkRes2; chunkIndex.z--; }
            else if (localIndex.z == 1) { localIndex.z = chunkRes1; chunkIndex.z--; }

            //While we have kernel left, keep going through the chunks, assigning a new piece for each chunk.
            while (kernelIndex.x < kernel.size.x)
            {
                localEnd.x = chunkRes;
                if ((localEnd.x - localIndex.x) > (kernel.size.x - kernelIndex.x))
                {
                    localEnd.x = localIndex.x + (kernel.size.x - kernelIndex.x);
                }
                kernelIndexDelta.x = localEnd.x - localIndex.x;
                if (localEnd.x == chunkRes) { kernelIndexDelta.x -= 2; }
                else if (localEnd.x == chunkRes1) { kernelIndexDelta.x -= 1; }
                kernelIndexDelta.x = math.max(kernelIndexDelta.x, 0);
                if (localIndex.x == chunkRes1) { nextStart.x = 1; }
                else { nextStart.x = 0; }

                int ty = kernelIndex.y;
                int cy = chunkIndex.y;
                int ly = localIndex.y;
                while (kernelIndex.y < kernel.size.y)
                {
                    localEnd.y = chunkRes;
                    if ((localEnd.y - localIndex.y) > (kernel.size.y - kernelIndex.y))
                    {
                        localEnd.y = localIndex.y + (kernel.size.y - kernelIndex.y);
                    }
                    kernelIndexDelta.y = localEnd.y - localIndex.y;
                    if (localEnd.y == chunkRes) { kernelIndexDelta.y -= 2; }
                    else if (localEnd.y == chunkRes1) { kernelIndexDelta.y -= 1; }
                    kernelIndexDelta.y = math.max(kernelIndexDelta.y, 0);
                    if (localIndex.y == chunkRes1) { nextStart.y = 1; }
                    else { nextStart.y = 0; }

                    int tz = kernelIndex.z;
                    int cz = chunkIndex.z;
                    int lz = localIndex.z;
                    while (kernelIndex.z < kernel.size.z)
                    {
                        localEnd.z = chunkRes;
                        if ((localEnd.z - localIndex.z) > (kernel.size.z - kernelIndex.z))
                        {
                            localEnd.z = localIndex.z + (kernel.size.z - kernelIndex.z);
                        }
                        kernelIndexDelta.z = localEnd.z - localIndex.z;
                        if (localEnd.z == chunkRes) { kernelIndexDelta.z -= 2; }
                        else if (localEnd.z == chunkRes1) { kernelIndexDelta.z -= 1; }
                        kernelIndexDelta.z = math.max(kernelIndexDelta.z, 0);
                        if (localIndex.z == chunkRes1) { nextStart.z = 1; }
                        else { nextStart.z = 0; }

                        KernelPiece piece = new KernelPiece();

                        piece.size = localEnd - localIndex;
                        piece.chunkIndex = chunkIndex;
                        piece.localIndexStart = localIndex;
                        piece.kernelIndexStart = kernelIndex;

                        pieces.Add(piece);

                        kernelIndex.z += kernelIndexDelta.z;
                        chunkIndex.z++;
                        localIndex.z = nextStart.z;
                    }
                    kernelIndex.z = tz;
                    chunkIndex.z = cz;
                    localIndex.z = lz;

                    kernelIndex.y += kernelIndexDelta.y;
                    chunkIndex.y++;
                    localIndex.y = nextStart.y;
                }
                kernelIndex.y = ty;
                chunkIndex.y = cy;
                localIndex.y = ly;

                kernelIndex.x += kernelIndexDelta.x;
                chunkIndex.x++;
                localIndex.x = nextStart.x;
            }
            
            return pieces;
        }

        //Calculates the lowest distance (in voxels) from a changed voxel to the border of the kernel.
        //Should always be 1 or 2.
        public static int CalculateKernelMargin(Kernel kernel)
        {
            int3 end = kernel.size - 1;
            int kernelResZ = kernel.size.z;
            int kernelStepYZ = kernel.size.y * kernel.size.z;
            int margin = int.MaxValue;

            for (int x = 0; x < kernel.size.x; x++)
            {
                for (int y = 0; y < kernel.size.y; y++)
                {
                    for (int z = 0; z < kernel.size.z; z++)
                    {
                        int i = x * kernelStepYZ + y * kernelResZ + z;
                        int3 ic = new int3(x, y, z);
                        int3 m = math.min(ic, end - ic);
                        if (kernel.data[i] < 0f) { margin = math.min(margin, math.cmin(m)); }
                    }
                }
            }

            return margin;
        }
    }
}
