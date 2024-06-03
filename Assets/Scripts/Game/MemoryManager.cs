using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Rendering;

public class MemoryManager : MonoBehaviour
{
    private static MemoryManager Instance = null;

    private bool initialized = false;

    private Stack<NativeList<float3>> listFloat3 = null;
    private Stack<NativeList<int4>> listInt4 = null;
    private Stack<NativeArray<int>> arrayInt = null;
    private Stack<NativeArray<float>> arrayFloat = null;
    private Stack<NativeArray<byte>> arrayByte = null;

    private NativeArray<VertexAttributeDescriptor> vertexAttributes;

    private void Awake()
    {
        if (Instance != null)
        {
#if !UNITY_EDITOR
            Debug.LogWarning("Memory manager already instanced.");
#endif
            Destroy(this);
        }
        else { Initialize(); }
    }

    private void OnDestroy()
    {
        Dispose();
    }


    private void Initialize()
    {
        Instance = this;
        initialized = true;
        DontDestroyOnLoad(gameObject);

        listFloat3 = new Stack<NativeList<float3>>();
        listInt4 = new Stack<NativeList<int4>>();
        arrayInt = new Stack<NativeArray<int>>();
        arrayFloat = new Stack<NativeArray<float>>();
        arrayByte = new Stack<NativeArray<byte>>();

        AllocateElements(ElementType.ListFloat3, 32);
        AllocateElements(ElementType.ListInt4, 16);
        AllocateElements(ElementType.ArrayInt, 16);
        AllocateElements(ElementType.ArrayFloat, 16);
        AllocateElements(ElementType.ArrayByte, 16);

        vertexAttributes = new NativeArray<VertexAttributeDescriptor>(2, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        vertexAttributes[0] = new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0);
        vertexAttributes[1] = new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3, 1);
    }

    private void AllocateElements(ElementType type, int count)
    {
        for(int i = 0; i < count; i++)
        {
            switch (type)
            {
                case ElementType.ListFloat3:
                    listFloat3.Push(new NativeList<float3>(4048, AllocatorManager.Persistent));
                    break;
                case ElementType.ListInt4:
                    listInt4.Push(new NativeList<int4>(4048, AllocatorManager.Persistent));
                    break;
                case ElementType.ArrayInt:
                    arrayInt.Push(new NativeArray<int>(Constants.ChunkIndexSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory));
                    break;
                case ElementType.ArrayFloat:
                    arrayFloat.Push(new NativeArray<float>(Constants.ChunkIndexSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory));
                    break;
                case ElementType.ArrayByte:
                    arrayByte.Push(new NativeArray<byte>(Constants.ChunkIndexSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory));
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }


    private void Dispose()
    {
        if (!initialized) { return; }

        foreach (var item in listFloat3)
        {
            item.Dispose();
        }

        foreach (var item in listInt4)
        {
            item.Dispose();
        }

        foreach (var item in arrayInt)
        {
            item.Dispose();
        }

        foreach (var item in arrayFloat)
        {
            item.Dispose();
        }

        foreach (var item in arrayByte)
        {
            item.Dispose();
        }

        vertexAttributes.Dispose();

        Instance = null;
    }

    public static NativeList<float3> GetListFloat3()
    {
        if (Instance == null) { Debug.LogError("No memory manager instanced."); return new NativeList<float3>(); }
        if (Instance.listFloat3.Count == 0) { Instance.AllocateElements(ElementType.ListFloat3, 4); }
        return Instance.listFloat3.Pop();
    }
    public static NativeList<int4> GetListInt4()
    {
        if (Instance == null) { Debug.LogError("No memory manager instanced."); return new NativeList<int4>(); }
        if (Instance.listInt4.Count == 0) { Instance.AllocateElements(ElementType.ListInt4, 4); }
        return Instance.listInt4.Pop();
    }
    public static NativeArray<int> GetArrayInt()
    {
        if (Instance == null) { Debug.LogError("No memory manager instanced."); return new NativeArray<int>(); }
        if (Instance.arrayInt.Count == 0) { Instance.AllocateElements(ElementType.ArrayInt, 4); }
        return Instance.arrayInt.Pop();
    }
    public static NativeArray<float> GetArrayFloat()
    {
        if (Instance == null) { Debug.LogError("No memory manager instanced."); return new NativeArray<float>(); }
        if (Instance.arrayFloat.Count == 0) { Instance.AllocateElements(ElementType.ArrayFloat, 4); }
        return Instance.arrayFloat.Pop();
    }

    public static NativeArray<byte> GetArrayByte()
    {
        if (Instance == null) { Debug.LogError("No memory manager instanced."); return new NativeArray<byte>(); }
        if (Instance.arrayByte.Count == 0) { Instance.AllocateElements(ElementType.ArrayByte, 4); }
        return Instance.arrayByte.Pop();
    }

    public static void Return(NativeList<float3> item)
    {
        item.Clear();
        if (Instance == null) { item.Dispose(); }
        else { Instance.listFloat3.Push(item); }
    }
    public static void Return(NativeList<int4> item)
    {
        item.Clear();
        if (Instance == null) { item.Dispose(); }
        else { Instance.listInt4.Push(item); }
    }
    public static void Return(NativeArray<int> item)
    {
        if (Instance == null) { item.Dispose(); }
        else { Instance.arrayInt.Push(item); }
    }
    public static void Return(NativeArray<float> item)
    {
        if (Instance == null) { item.Dispose(); }
        else { Instance.arrayFloat.Push(item); }
    }

    public static void Return(NativeArray<byte> item)
    {
        if (Instance == null) { item.Dispose(); }
        else { Instance.arrayByte.Push(item); }
    }

    public static NativeArray<VertexAttributeDescriptor> GetVertexAttributes()
    {
        if (Instance == null) { Debug.LogError("No memory manager instanced."); return new NativeArray<VertexAttributeDescriptor>(); }
        return Instance.vertexAttributes;
    }

    private enum ElementType { ListFloat3, ListInt4, ArrayInt, ArrayFloat, ArrayByte }
}
