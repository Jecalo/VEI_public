using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine.UIElements;
using System;



public struct PriorityQueueSimple
{
    public struct pqNode
    {
        public int value;   //Priority
        public int key;     //Contents

        public pqNode(int v, int k)
        {
            value = v;
            key = k;
        }
    }

    private NativeList<pqNode> heap;

    public int Length { get { return heap.Length; } }

    public PriorityQueueSimple(Allocator allocator, int size = 32)
    {
        heap = new NativeList<pqNode>(size, allocator);
    }

    public void Init(Allocator allocator, int size = 32)
    {
        if (!heap.IsCreated) { heap = new NativeList<pqNode>(size, allocator); }
    }

    public void Dispose()
    {
        if (heap.IsCreated) { heap.Dispose(); }
    }

    public void Clear()
    {
        heap.Clear();
    }

    public NativeList<pqNode> GetHeap()
    {
        return heap;
    }

    public int Peek()
    {
        return (heap.Length == 0) ? -1 : heap[0].key;
    }

    public void Push(int value, int key)
    {
        int l = heap.Length;
        heap.Add(new pqNode(value, key));
        SiftUp(l);
    }

    public int Pop()
    {
        if (heap.Length == 0) { return -1; }
        int r = heap[0].key;
        heap.RemoveAtSwapBack(0);
        SiftDown(0);
        return r;
    }

    private void SiftDown(int i)
    {
        int length = heap.Length;
        int child = i * 2 + 1;
        if (length < 2 || child >= length) { return; }
        if ((child + 1) < length && heap[child + 1].value < heap[child].value) { child++; }
        if (heap[i].value <= heap[child].value) { return; }

        pqNode node = heap[i];

        do
        {
            heap[i] = heap[child];
            i = child;
            child = i * 2 + 1;

            if (child >= length) { break; }
            if ((child + 1) < length && heap[child + 1].value < heap[child].value) { child++; }
        }
        while (node.value > heap[child].value);

        heap[i] = node;
    }

    private void SiftUp(int i)
    {
        int parent = (i - 1) / 2;
        if (heap.Length < 2 || i <= 0) { return; }
        if (heap[i].value > heap[parent].value) { return; }
        pqNode node = heap[i];

        do
        {
            heap[i] = heap[parent];
            i = parent;
            if (i <= 0) { break; }
            parent = (parent - 1) / 2;
        }
        while (node.value < heap[parent].value);

        heap[i] = node;
    }
}