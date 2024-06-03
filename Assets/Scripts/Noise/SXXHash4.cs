using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;


public struct SXXHash
{

    const uint primeA = 0b10011110001101110111100110110001;
    const uint primeB = 0b10000101111010111100101001110111;
    const uint primeC = 0b11000010101100101010111000111101;
    const uint primeD = 0b00100111110101001110101100101111;
    const uint primeE = 0b00010110010101100110011110110001;

    uint accumulator;

    public SXXHash(int seed) { accumulator = (uint)seed + primeE; }
    public SXXHash(uint seed) { accumulator = seed + primeE; }

    public void Hash()
    {
        accumulator ^= accumulator >> 15;
        accumulator *= primeB;
        accumulator ^= accumulator >> 13;
        accumulator *= primeC;
        accumulator ^= accumulator >> 16;
    }

    public void Consume(int value) { accumulator = RotateLeft(accumulator + (uint)value * primeC, 17) * primeD; }
    public void Consume(uint value) { accumulator = RotateLeft(accumulator + value * primeC, 17) * primeD; }
    public void Consume(byte value) { accumulator = RotateLeft(accumulator + value * primeE, 11) * primeA; }


    public uint State { get { return accumulator; } set { accumulator = value; } }

    private static uint RotateLeft(uint data, int steps) { return (data << steps) | (data >> 32 - steps); }

    public uint ByteA => State & 255;
    public uint ByteB => (State >> 8) & 255;
    public uint ByteC => (State >> 16) & 255;
    public uint ByteD => (State >> 24);
    public float UnitFloatA => ByteA * (1f / 255f);
    public float UnitFloatB => ByteB * (1f / 255f);
    public float UnitFloatC => ByteC * (1f / 255f);
    public float UnitFloatD => ByteD * (1f / 255f);
}

public struct SXXHash4
{

    const uint primeB = 0b10000101111010111100101001110111;
    const uint primeC = 0b11000010101100101010111000111101;
    const uint primeD = 0b00100111110101001110101100101111;
    const uint primeE = 0b00010110010101100110011110110001;

    uint4 accumulator;

    public SXXHash4(int seed) { accumulator = (uint)seed + primeE; }
    public SXXHash4(uint seed) { accumulator = seed + primeE; }

    public void Hash()
    {
        accumulator ^= accumulator >> 15;
        accumulator *= primeB;
        accumulator ^= accumulator >> 13;
        accumulator *= primeC;
        accumulator ^= accumulator >> 16;
    }

    public uint4 State { get { return accumulator; } set { accumulator = value; } }

    public void Consume(int4 value) { accumulator = RotateLeft(accumulator + (uint4)value * primeC, 17) * primeD; }
    public void Consume(uint4 value) { accumulator = RotateLeft(accumulator + value * primeC, 17) * primeD; }

    private static uint4 RotateLeft(uint4 data, int steps) { return (data << steps) | (data >> 32 - steps); }

    public uint4 ByteA => State & 255;
    public uint4 ByteB => (State >> 8) & 255;
    public uint4 ByteC => (State >> 16) & 255;
    public uint4 ByteD => (State >> 24);
    public float4 UnitFloatA => (float4)ByteA * (1f / 255f);
    public float4 UnitFloatB => (float4)ByteB * (1f / 255f);
    public float4 UnitFloatC => (float4)ByteC * (1f / 255f);
    public float4 UnitFloatD => (float4)ByteD * (1f / 255f);

    public void ConsumeCoords(int4x3 value) { Consume(value.c0); Consume(value.c1); Consume(value.c2); }

    public void ConsumeCoords(int4 u, int4 v, int4 w) { Consume(u); Consume(v); Consume(w); }
}