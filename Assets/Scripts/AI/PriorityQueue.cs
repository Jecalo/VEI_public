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

public struct PriorityQueue
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
    private NativeList<int> heapIndex;

    public int Length { get { return heap.Length; } }

    public PriorityQueue(Allocator allocator, int size = 32, int indexSize = 128)
    {
        heap = new NativeList<pqNode>(size, allocator);
        heapIndex = new NativeList<int>(indexSize, allocator);
        heapIndex.Resize(heapIndex.Capacity, NativeArrayOptions.UninitializedMemory);
        for (int i = 0; i < indexSize; i++) { heapIndex[i] = -1; }
    }

    public void Init(Allocator allocator, int size = 32, int indexSize = 128)
    {
        if (!heap.IsCreated) { heap = new NativeList<pqNode>(size, allocator); }
        if (!heapIndex.IsCreated) { heapIndex = new NativeList<int>(indexSize, allocator); }
        heapIndex.Resize(heapIndex.Capacity, NativeArrayOptions.UninitializedMemory);
        for (int i = 0; i < indexSize; i++) { heapIndex[i] = -1; }
    }

    public void Dispose()
    {
        if (heap.IsCreated) { heap.Dispose(); }
        if (heapIndex.IsCreated) { heapIndex.Dispose(); }
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
        //ExtendIndexes(key);
        //if (heapIndex[key] != -1) { throw new Exception("Key already present."); }
        int l = heap.Length;
        heapIndex[key] = l;
        heap.Add(new pqNode(value, key));
        SiftUp(l);
    }

    public int Pop()
    {
        if (heap.Length == 0) { return -1; }
        int r = heap[0].key;
        heapIndex[heap[0].key] = -1;
        heap.RemoveAtSwapBack(0);
        SiftDown(0);
        return r;
    }

    public bool Contains(int key)
    {
        return key < heapIndex.Length && heapIndex[key] != -1;
    }

    public void Change(int value, int key)
    {
        //if (key >= heapIndex.Length || heapIndex[key] == -1) { throw new Exception("Key not present."); }
        int i = heapIndex[key];
        if (heap[i].value > value)
        {
            heap[i] = new pqNode(value, key);
            SiftDown(i);
        }
        else if (heap[i].value < value)
        {
            heap[i] = new pqNode(value, key);
            SiftUp(i);
        }
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
            heapIndex[heap[i].key] = i;
            i = child;
            child = i * 2 + 1;

            if (child >= length) { break; }
            if ((child + 1) < length && heap[child + 1].value < heap[child].value) { child++; }
        }
        while (node.value > heap[child].value);

        heap[i] = node;
        heapIndex[heap[i].key] = i;
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
            heapIndex[heap[i].key] = i;
            i = parent;
            if (i <= 0) { break; }
            parent = (parent - 1) / 2;
        }
        while (node.value < heap[parent].value);

        heap[i] = node;
        heapIndex[heap[i].key] = i;
    }

    private void ExtendIndexes(int i)
    {
        if (heapIndex.Length > i) { return; }
        int j = heapIndex.Length;
        heapIndex.Resize(i + 1, NativeArrayOptions.UninitializedMemory);
        for (int k = j; k <= i; k++) { heapIndex[k] = -1; }
    }

    //public bool Check(int i)
    //{
    //    if (i >= ((heap.Length - 1) / 2)) { return true; }
    //    int a = i * 2 + 1;
    //    int b = i * 2 + 2;
    //    return (heap[i].value <= heap[a].value) && (heap[i].value <= heap[b].value) && Check(a) && Check(b);
    //}
}