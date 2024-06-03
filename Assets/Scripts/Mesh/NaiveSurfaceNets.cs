using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

public static class NaiveSurfaceNets
{
	public static JobWrapper<Job> GenerateJob(NativeArray<float> data, int resolution, float3 offset, float voxelSize)
	{
		if (data.Length != (resolution * resolution * resolution)) { Debug.LogError("Array size does not match resolution."); return null; }
		if (resolution < 4) { Debug.LogError("Too low resolution"); return null; }
		if (voxelSize <= 0.00001f) { Debug.LogError("Wrong voxel size"); return null; }

		Job job = new Job();

        job.verts = MemoryManager.GetListFloat3();
        job.normals = MemoryManager.GetListFloat3();
        job.quads = MemoryManager.GetListInt4();
        job.vertexIndexBuffer = MemoryManager.GetArrayInt();

        job.resolution = resolution;
		job.data = data;
		job.offset = offset;
		job.voxelSize = voxelSize;

		return new JobWrapper<Job>(job);
	}

	[BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct Job : IJob
    {
        [WriteOnly]
        public NativeList<float3> verts;
		[WriteOnly]
		public NativeList<float3> normals;
		[WriteOnly]
		public NativeList<int4> quads;

        public NativeArray<int> vertexIndexBuffer;

        [ReadOnly]
        public NativeArray<float> data;
        [ReadOnly]
        public int resolution;
        [ReadOnly]
        public float3 offset;
        [ReadOnly]
        public float voxelSize;

		private int currentVertex;

        public void Execute()
        {
            int indexStep = resolution * resolution;
            int iterationCount = resolution * resolution * resolution;
            int res1 = resolution - 1;

            int indexStepX = resolution * resolution;
            int indexStepY = resolution;
            int indexStepZ = 1;
            int indexStepYZ = indexStepY + indexStepZ;
            int indexStepXZ = indexStepX + indexStepZ;
            int indexStepXY = indexStepX + indexStepY;
            int indexStepXYZ = indexStepX + indexStepY + indexStepZ;

			currentVertex = 0;

            for (int i = 0; i < iterationCount; i++)
            {
				int x = i / indexStep;
				int y = (i % indexStep) / resolution;
				int z = i % resolution;

				if (x == res1 || y == res1 || z == res1) { continue; }

				float cornerValue0, cornerValue1, cornerValue2, cornerValue3,
					cornerValue4, cornerValue5, cornerValue6, cornerValue7;
				int cornerMask0, cornerMask1, cornerMask2, cornerMask3,
					cornerMask4, cornerMask5, cornerMask6, cornerMask7;


				cornerValue0 = data[i];
				cornerValue1 = data[i + indexStepZ];
				cornerValue2 = data[i + indexStepXZ];
				cornerValue3 = data[i + indexStepX];
				cornerValue4 = data[i + indexStepY];
				cornerValue5 = data[i + indexStepYZ];
				cornerValue6 = data[i + indexStepXYZ];
				cornerValue7 = data[i + indexStepXY];

				cornerMask0 = (cornerValue0 < 0.0f) ? 1 : 0;
				cornerMask1 = (cornerValue1 < 0.0f) ? 1 : 0;
				cornerMask2 = (cornerValue2 < 0.0f) ? 1 : 0;
				cornerMask3 = (cornerValue3 < 0.0f) ? 1 : 0;
				cornerMask4 = (cornerValue4 < 0.0f) ? 1 : 0;
				cornerMask5 = (cornerValue5 < 0.0f) ? 1 : 0;
				cornerMask6 = (cornerValue6 < 0.0f) ? 1 : 0;
				cornerMask7 = (cornerValue7 < 0.0f) ? 1 : 0;

				int voxelType = (1 * cornerMask0) | (2 * cornerMask1) | (4 * cornerMask2) | (8 * cornerMask3) |
					(16 * cornerMask4) | (32 * cornerMask5) | (64 * cornerMask6) | (128 * cornerMask7);

				if (voxelType == 0 || voxelType == 255) { continue; }


				//Naive interpolation
				float3 v = new float3(offset.x + voxelSize * (float)(x), offset.y + voxelSize * (float)(y), offset.z + voxelSize * (float)(z));
				float3 u = new float3(0.0f, 0.0f, 0.0f);

				int edgeMask = sn_crossedEdges[voxelType];

				if ((edgeMask & 0b0000000000000001) > 0)
				{
					u.z += cornerValue0 / (cornerValue0 - cornerValue1);
				}
				if ((edgeMask & 0b0000000000000010) > 0)
				{
					u.x += cornerValue1 / (cornerValue1 - cornerValue2);
					u.z += 1.0f;
				}
				if ((edgeMask & 0b0000000000000100) > 0)
				{
					u.z += cornerValue3 / (cornerValue3 - cornerValue2);
					u.x += 1.0f;
				}
				if ((edgeMask & 0b0000000000001000) > 0)
				{
					u.x += cornerValue0 / (cornerValue0 - cornerValue3);
				}
				if ((edgeMask & 0b0000000000010000) > 0)
				{
					u.z += cornerValue4 / (cornerValue4 - cornerValue5);
					u.y += 1.0f;
				}
				if ((edgeMask & 0b0000000000100000) > 0)
				{
					u.x += cornerValue5 / (cornerValue5 - cornerValue6);
					u.y += 1.0f;
					u.z += 1.0f;
				}
				if ((edgeMask & 0b0000000001000000) > 0)
				{
					u.z += cornerValue7 / (cornerValue7 - cornerValue6);
					u.x += 1.0f;
					u.y += 1.0f;
				}
				if ((edgeMask & 0b0000000010000000) > 0)
				{
					u.x += cornerValue4 / (cornerValue4 - cornerValue7);
					u.y += 1.0f;
				}
				if ((edgeMask & 0b0000000100000000) > 0)
				{
					u.y += cornerValue0 / (cornerValue0 - cornerValue4);
				}
				if ((edgeMask & 0b0000001000000000) > 0)
				{
					u.y += cornerValue1 / (cornerValue1 - cornerValue5);
					u.z += 1.0f;
				}
				if ((edgeMask & 0b0000010000000000) > 0)
				{
					u.y += cornerValue2 / (cornerValue2 - cornerValue6);
					u.x += 1.0f;
					u.z += 1.0f;
				}
				if ((edgeMask & 0b0000100000000000) > 0)
				{
					u.y += cornerValue3 / (cornerValue3 - cornerValue7);
					u.x += 1.0f;
				}

				u = (u / sn_edgeCount[voxelType]) * voxelSize;
				v += u;

				verts.Add(v);
				vertexIndexBuffer[i] = currentVertex++;


				//Calculate the vertex normal.
				float3 n = float3.zero;
				n.x = cornerValue2 - cornerValue1 + cornerValue3 - cornerValue0 + cornerValue6 - cornerValue5 + cornerValue7 - cornerValue4;
				n.y = cornerValue4 - cornerValue0 + cornerValue5 - cornerValue1 + cornerValue6 - cornerValue2 + cornerValue7 - cornerValue3;
				n.z = cornerValue1 - cornerValue0 + cornerValue2 - cornerValue3 + cornerValue5 - cornerValue4 + cornerValue6 - cornerValue7;
				normals.Add(math.normalize(n));


				//If this vertex has completed any quads, build them. Skip if on the border.
				if ((x == 0) || (y == 0) || (z == 0)) { continue; }
				int faceBuildMask = sn_completeFaces[voxelType];
				int4 quad;
				if ((faceBuildMask & 0b00000001) > 0)
				{
					quad.x = vertexIndexBuffer[i];
					quad.y = vertexIndexBuffer[i - indexStepZ];
					quad.z = vertexIndexBuffer[i - indexStepYZ];
					quad.w = vertexIndexBuffer[i - indexStepY];
					quads.Add(quad);
				}
				if ((faceBuildMask & 0b00000010) > 0)
				{
					quad.w = vertexIndexBuffer[i];
					quad.z = vertexIndexBuffer[i - indexStepZ];
					quad.y = vertexIndexBuffer[i - indexStepYZ];
					quad.x = vertexIndexBuffer[i - indexStepY];
					quads.Add(quad);
				}
				if ((faceBuildMask & 0b00000100) > 0)
				{
					quad.x = vertexIndexBuffer[i];
					quad.y = vertexIndexBuffer[i - indexStepX];
					quad.z = vertexIndexBuffer[i - indexStepXZ];
					quad.w = vertexIndexBuffer[i - indexStepZ];
					quads.Add(quad);
				}
				if ((faceBuildMask & 0b00001000) > 0)
				{
					quad.w = vertexIndexBuffer[i];
					quad.z = vertexIndexBuffer[i - indexStepX];
					quad.y = vertexIndexBuffer[i - indexStepXZ];
					quad.x = vertexIndexBuffer[i - indexStepZ];
					quads.Add(quad);
				}
				if ((faceBuildMask & 0b00010000) > 0)
				{
					quad.x = vertexIndexBuffer[i];
					quad.y = vertexIndexBuffer[i - indexStepY];
					quad.z = vertexIndexBuffer[i - indexStepXY];
					quad.w = vertexIndexBuffer[i - indexStepX];
					quads.Add(quad);
				}
				if ((faceBuildMask & 0b00100000) > 0)
				{
					quad.w = vertexIndexBuffer[i];
					quad.z = vertexIndexBuffer[i - indexStepY];
					quad.y = vertexIndexBuffer[i - indexStepXY];
					quad.x = vertexIndexBuffer[i - indexStepX];
					quads.Add(quad);
				}
			}
        }
    }


	//Takes an 8-point mask (byte) and gives the amount of connecting edges.
	public static readonly uint[] sn_edgeCount =
    {
 0,  3,  3,  4,  3,  6,  4,  5,  3,  4,  6,  5,  4,  5,  5,  4,  3,  4,  6,  5,  6,  7,  7,  6,  6,  5,  9,  6,  7,  6,  8,  5,
 3,  6,  4,  5,  6,  9,  5,  6,  6,  7,  7,  6,  7,  8,  6,  5,  4,  5,  5,  4,  7,  8,  6,  5,  7,  6,  8,  5,  8,  7,  7,  4,
 3,  6,  6,  7,  4,  7,  5,  6,  6,  7,  9,  8,  5,  6,  6,  5,  6,  7,  9,  8,  7,  8,  8,  7,  9,  8, 12,  9,  8,  7,  9,  6,
 4,  7,  5,  6,  5,  8,  4,  5,  7,  8,  8,  7,  6,  7,  5,  4,  5,  6,  6,  5,  6,  7,  5,  4,  8,  7,  9,  6,  7,  6,  6,  3,
 3,  6,  6,  7,  6,  9,  7,  8,  4,  5,  7,  6,  5,  6,  6,  5,  4,  5,  7,  6,  7,  8,  8,  7,  5,  4,  8,  5,  6,  5,  7,  4,
 6,  9,  7,  8,  9, 12,  8,  9,  7,  8,  8,  7,  8,  9,  7,  6,  5,  6,  6,  5,  8,  9,  7,  6,  6,  5,  7,  4,  7,  6,  6,  3,
 4,  7,  7,  8,  5,  8,  6,  7,  5,  6,  8,  7,  4,  5,  5,  4,  5,  6,  8,  7,  6,  7,  7,  6,  6,  5,  9,  6,  5,  4,  6,  3,
 5,  8,  6,  7,  6,  9,  5,  6,  6,  7,  7,  6,  5,  6,  4,  3,  4,  5,  5,  4,  5,  6,  4,  3,  5,  4,  6,  3,  4,  3,  3,  0
    };

	//Takes an 8-point mask (byte) and gives a mask of the types of faces it should complete. Ordered: -X, +X, -Y, +Y, -Z, +Z, 0, 0
	public static readonly byte[] sn_completeFaces =
        {
0b00000000, 0b00101010, 0b00010000, 0b00001010, 0b00000000, 0b00101010, 0b00010000, 0b00001010, 0b00000001, 0b00101000, 0b00010001, 0b00001000, 0b00000001, 0b00101000, 0b00010001, 0b00001000,
0b00000100, 0b00100010, 0b00010100, 0b00000010, 0b00000100, 0b00100010, 0b00010100, 0b00000010, 0b00000101, 0b00100000, 0b00010101, 0b00000000, 0b00000101, 0b00100000, 0b00010101, 0b00000000,
0b00000000, 0b00101010, 0b00010000, 0b00001010, 0b00000000, 0b00101010, 0b00010000, 0b00001010, 0b00000001, 0b00101000, 0b00010001, 0b00001000, 0b00000001, 0b00101000, 0b00010001, 0b00001000,
0b00000100, 0b00100010, 0b00010100, 0b00000010, 0b00000100, 0b00100010, 0b00010100, 0b00000010, 0b00000101, 0b00100000, 0b00010101, 0b00000000, 0b00000101, 0b00100000, 0b00010101, 0b00000000,
0b00000000, 0b00101010, 0b00010000, 0b00001010, 0b00000000, 0b00101010, 0b00010000, 0b00001010, 0b00000001, 0b00101000, 0b00010001, 0b00001000, 0b00000001, 0b00101000, 0b00010001, 0b00001000,
0b00000100, 0b00100010, 0b00010100, 0b00000010, 0b00000100, 0b00100010, 0b00010100, 0b00000010, 0b00000101, 0b00100000, 0b00010101, 0b00000000, 0b00000101, 0b00100000, 0b00010101, 0b00000000,
0b00000000, 0b00101010, 0b00010000, 0b00001010, 0b00000000, 0b00101010, 0b00010000, 0b00001010, 0b00000001, 0b00101000, 0b00010001, 0b00001000, 0b00000001, 0b00101000, 0b00010001, 0b00001000,
0b00000100, 0b00100010, 0b00010100, 0b00000010, 0b00000100, 0b00100010, 0b00010100, 0b00000010, 0b00000101, 0b00100000, 0b00010101, 0b00000000, 0b00000101, 0b00100000, 0b00010101, 0b00000000,
0b00000000, 0b00101010, 0b00010000, 0b00001010, 0b00000000, 0b00101010, 0b00010000, 0b00001010, 0b00000001, 0b00101000, 0b00010001, 0b00001000, 0b00000001, 0b00101000, 0b00010001, 0b00001000,
0b00000100, 0b00100010, 0b00010100, 0b00000010, 0b00000100, 0b00100010, 0b00010100, 0b00000010, 0b00000101, 0b00100000, 0b00010101, 0b00000000, 0b00000101, 0b00100000, 0b00010101, 0b00000000,
0b00000000, 0b00101010, 0b00010000, 0b00001010, 0b00000000, 0b00101010, 0b00010000, 0b00001010, 0b00000001, 0b00101000, 0b00010001, 0b00001000, 0b00000001, 0b00101000, 0b00010001, 0b00001000,
0b00000100, 0b00100010, 0b00010100, 0b00000010, 0b00000100, 0b00100010, 0b00010100, 0b00000010, 0b00000101, 0b00100000, 0b00010101, 0b00000000, 0b00000101, 0b00100000, 0b00010101, 0b00000000,
0b00000000, 0b00101010, 0b00010000, 0b00001010, 0b00000000, 0b00101010, 0b00010000, 0b00001010, 0b00000001, 0b00101000, 0b00010001, 0b00001000, 0b00000001, 0b00101000, 0b00010001, 0b00001000,
0b00000100, 0b00100010, 0b00010100, 0b00000010, 0b00000100, 0b00100010, 0b00010100, 0b00000010, 0b00000101, 0b00100000, 0b00010101, 0b00000000, 0b00000101, 0b00100000, 0b00010101, 0b00000000,
0b00000000, 0b00101010, 0b00010000, 0b00001010, 0b00000000, 0b00101010, 0b00010000, 0b00001010, 0b00000001, 0b00101000, 0b00010001, 0b00001000, 0b00000001, 0b00101000, 0b00010001, 0b00001000,
0b00000100, 0b00100010, 0b00010100, 0b00000010, 0b00000100, 0b00100010, 0b00010100, 0b00000010, 0b00000101, 0b00100000, 0b00010101, 0b00000000, 0b00000101, 0b00100000, 0b00010101, 0b00000000
        };

	//Takes an 8-point mask (byte) and gives a mask of the crossed edges.
	public static readonly ushort[] sn_crossedEdges =
    {
0b0000000000000000, 0b0000000100001001, 0b0000001000000011, 0b0000001100001010, 0b0000010000000110, 0b0000010100001111, 0b0000011000000101, 0b0000011100001100,
0b0000100000001100, 0b0000100100000101, 0b0000101000001111, 0b0000101100000110, 0b0000110000001010, 0b0000110100000011, 0b0000111000001001, 0b0000111100000000,
0b0000000110010000, 0b0000000010011001, 0b0000001110010011, 0b0000001010011010, 0b0000010110010110, 0b0000010010011111, 0b0000011110010101, 0b0000011010011100,
0b0000100110011100, 0b0000100010010101, 0b0000101110011111, 0b0000101010010110, 0b0000110110011010, 0b0000110010010011, 0b0000111110011001, 0b0000111010010000,
0b0000001000110000, 0b0000001100111001, 0b0000000000110011, 0b0000000100111010, 0b0000011000110110, 0b0000011100111111, 0b0000010000110101, 0b0000010100111100,
0b0000101000111100, 0b0000101100110101, 0b0000100000111111, 0b0000100100110110, 0b0000111000111010, 0b0000111100110011, 0b0000110000111001, 0b0000110100110000,
0b0000001110100000, 0b0000001010101001, 0b0000000110100011, 0b0000000010101010, 0b0000011110100110, 0b0000011010101111, 0b0000010110100101, 0b0000010010101100,
0b0000101110101100, 0b0000101010100101, 0b0000100110101111, 0b0000100010100110, 0b0000111110101010, 0b0000111010100011, 0b0000110110101001, 0b0000110010100000,
0b0000010001100000, 0b0000010101101001, 0b0000011001100011, 0b0000011101101010, 0b0000000001100110, 0b0000000101101111, 0b0000001001100101, 0b0000001101101100,
0b0000110001101100, 0b0000110101100101, 0b0000111001101111, 0b0000111101100110, 0b0000100001101010, 0b0000100101100011, 0b0000101001101001, 0b0000101101100000,
0b0000010111110000, 0b0000010011111001, 0b0000011111110011, 0b0000011011111010, 0b0000000111110110, 0b0000000011111111, 0b0000001111110101, 0b0000001011111100,
0b0000110111111100, 0b0000110011110101, 0b0000111111111111, 0b0000111011110110, 0b0000100111111010, 0b0000100011110011, 0b0000101111111001, 0b0000101011110000,
0b0000011001010000, 0b0000011101011001, 0b0000010001010011, 0b0000010101011010, 0b0000001001010110, 0b0000001101011111, 0b0000000001010101, 0b0000000101011100,
0b0000111001011100, 0b0000111101010101, 0b0000110001011111, 0b0000110101010110, 0b0000101001011010, 0b0000101101010011, 0b0000100001011001, 0b0000100101010000,
0b0000011111000000, 0b0000011011001001, 0b0000010111000011, 0b0000010011001010, 0b0000001111000110, 0b0000001011001111, 0b0000000111000101, 0b0000000011001100,
0b0000111111001100, 0b0000111011000101, 0b0000110111001111, 0b0000110011000110, 0b0000101111001010, 0b0000101011000011, 0b0000100111001001, 0b0000100011000000,
0b0000100011000000, 0b0000100111001001, 0b0000101011000011, 0b0000101111001010, 0b0000110011000110, 0b0000110111001111, 0b0000111011000101, 0b0000111111001100,
0b0000000011001100, 0b0000000111000101, 0b0000001011001111, 0b0000001111000110, 0b0000010011001010, 0b0000010111000011, 0b0000011011001001, 0b0000011111000000,
0b0000100101010000, 0b0000100001011001, 0b0000101101010011, 0b0000101001011010, 0b0000110101010110, 0b0000110001011111, 0b0000111101010101, 0b0000111001011100,
0b0000000101011100, 0b0000000001010101, 0b0000001101011111, 0b0000001001010110, 0b0000010101011010, 0b0000010001010011, 0b0000011101011001, 0b0000011001010000,
0b0000101011110000, 0b0000101111111001, 0b0000100011110011, 0b0000100111111010, 0b0000111011110110, 0b0000111111111111, 0b0000110011110101, 0b0000110111111100,
0b0000001011111100, 0b0000001111110101, 0b0000000011111111, 0b0000000111110110, 0b0000011011111010, 0b0000011111110011, 0b0000010011111001, 0b0000010111110000,
0b0000101101100000, 0b0000101001101001, 0b0000100101100011, 0b0000100001101010, 0b0000111101100110, 0b0000111001101111, 0b0000110101100101, 0b0000110001101100,
0b0000001101101100, 0b0000001001100101, 0b0000000101101111, 0b0000000001100110, 0b0000011101101010, 0b0000011001100011, 0b0000010101101001, 0b0000010001100000,
0b0000110010100000, 0b0000110110101001, 0b0000111010100011, 0b0000111110101010, 0b0000100010100110, 0b0000100110101111, 0b0000101010100101, 0b0000101110101100,
0b0000010010101100, 0b0000010110100101, 0b0000011010101111, 0b0000011110100110, 0b0000000010101010, 0b0000000110100011, 0b0000001010101001, 0b0000001110100000,
0b0000110100110000, 0b0000110000111001, 0b0000111100110011, 0b0000111000111010, 0b0000100100110110, 0b0000100000111111, 0b0000101100110101, 0b0000101000111100,
0b0000010100111100, 0b0000010000110101, 0b0000011100111111, 0b0000011000110110, 0b0000000100111010, 0b0000000000110011, 0b0000001100111001, 0b0000001000110000,
0b0000111010010000, 0b0000111110011001, 0b0000110010010011, 0b0000110110011010, 0b0000101010010110, 0b0000101110011111, 0b0000100010010101, 0b0000100110011100,
0b0000011010011100, 0b0000011110010101, 0b0000010010011111, 0b0000010110010110, 0b0000001010011010, 0b0000001110010011, 0b0000000010011001, 0b0000000110010000,
0b0000111100000000, 0b0000111000001001, 0b0000110100000011, 0b0000110000001010, 0b0000101100000110, 0b0000101000001111, 0b0000100100000101, 0b0000100000001100,
0b0000011100001100, 0b0000011000000101, 0b0000010100001111, 0b0000010000000110, 0b0000001100001010, 0b0000001000000011, 0b0000000100001001, 0b0000000000000000
    };
}
