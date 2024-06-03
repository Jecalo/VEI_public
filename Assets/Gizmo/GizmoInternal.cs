using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine.Rendering;
using Unity.VisualScripting;
using System.Threading;

public static class GizmoInternal
{
    public static readonly int DefaultGizmoBufferSize = 1024;
    public static readonly int SphereResolution = 32;
    public static readonly Color DefaultColor = Color.cyan;

    public static Mesh CubeMesh { get; private set; }
    public static Mesh SphereMesh { get; private set; }
    public static Mesh LineMesh { get; private set; }
    public static Mesh TriMesh { get; private set; }

    public static Material Material_Cubes { get; private set; }
    public static Material Material_Spheres { get; private set; }
    public static Material Material_Lines{ get; private set; }
    public static Material Material_Tris { get; private set; }


    public static NativeList<PreProps> Cubes;
    public static NativeList<PreProps> Spheres;
    public static NativeList<PropsLine> Lines;
    public static NativeList<PropsTri> Tris;

    private static int CurrentFrame;

    private static Shader LineShader;
    private static Shader LineShaderLine;
    private static Shader TriShader;


#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init() { }
#endif

    static GizmoInternal()
    {
#if UNITY_EDITOR
        UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += () => {
            if (Cubes.IsCreated) { Cubes.Dispose(); }
            if (Spheres.IsCreated) { Spheres.Dispose(); }
            if (Lines.IsCreated) { Lines.Dispose(); }
            if (Tris.IsCreated) { Tris.Dispose(); }
        };

        //Safety callback, in case Gizmos are being added while in the editor
        if (!UnityEditor.EditorApplication.isPlaying)
        { RenderPipelineManager.endContextRendering += (ScriptableRenderContext context, List<Camera> cameras) => { EndOfFrame(); }; }
#endif


        LineShader = Resources.Load<Shader>("line_shader");
        LineShaderLine = Resources.Load<Shader>("line_shader_line");
        TriShader = Resources.Load<Shader>("tri_shader");

        if (LineShader == null || LineShaderLine == null || TriShader == null) { Debug.LogError("Could not load line shader"); return; }

        CubeMesh = GenerateWireCube();
        SphereMesh = GenerateWireSphere(SphereResolution);
        LineMesh = GenerateLineMesh();
        TriMesh = GenerateTriMesh();

        Material_Cubes = new Material(LineShader);
        Material_Cubes.enableInstancing = true;
        Material_Spheres = new Material(LineShader);
        Material_Spheres.enableInstancing = true;
        Material_Lines = new Material(LineShaderLine);
        Material_Lines.enableInstancing = true;
        Material_Tris = new Material(TriShader);
        Material_Tris.enableInstancing = true;

        Cubes = new NativeList<PreProps>(DefaultGizmoBufferSize, Allocator.Persistent);
        Spheres = new NativeList<PreProps>(DefaultGizmoBufferSize, Allocator.Persistent);
        Lines = new NativeList<PropsLine>(DefaultGizmoBufferSize, Allocator.Persistent);
        Tris = new NativeList<PropsTri>(DefaultGizmoBufferSize, Allocator.Persistent);

        CurrentFrame = -1;
    }
    
    public static bool CheckState()
    {
        bool state = true;
        int e = -1;
        if      (LineShader == null) { state = false; e = 0; }
        else if (LineShaderLine == null) { state = false; e = 1; }
        else if (TriShader == null) { state = false; e = 2; }
        else if (Material_Cubes == null) { state = false; e = 3; }
        else if (Material_Spheres == null) { state = false; e = 4; }
        else if (Material_Lines == null) { state = false; e = 5; }
        else if (Material_Tris == null) { state = false; e = 6; }
        else if (CubeMesh == null) { state = false; e = 7; }
        else if (SphereMesh == null) { state = false; e = 8; }
        else if (LineMesh == null) { state = false; e = 9; }
        else if (TriMesh == null) { state = false; e = 10; }
        else if (!Cubes.IsCreated) { state = false; e = 11; }
        else if (!Spheres.IsCreated) { state = false; e = 12; }
        else if (!Lines.IsCreated) { state = false; e = 13; }
        else if (!Tris.IsCreated) { state = false; e = 14; }

        if (!state) { Debug.LogWarning("GizmoInternal state is not ready: " + e); }

        return state;
    }

    public static void DiscardOld()
    {
        if (Time.renderedFrameCount != CurrentFrame)
        {
            if (CurrentFrame == -1) { CurrentFrame = Time.renderedFrameCount; }
            else { Debug.Log("Discarding old gizmos."); EndOfFrame(); }
        }
    }

    public static void EndOfFrame()
    {
        Cubes.Clear();
        Spheres.Clear();
        Lines.Clear();
        Tris.Clear();
        //CurrentFrame = -1;
    }


    private static Mesh GenerateWireCube()
    {
        Mesh mesh = new Mesh() { name = "gizmoCubeMesh" };

        mesh.vertices = new Vector3[] {
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, -0.5f, -0.5f),
            new Vector3(-0.5f, 0.5f, -0.5f),
            new Vector3(-0.5f, -0.5f, 0.5f),
            new Vector3(0.5f, 0.5f, -0.5f),
            new Vector3(-0.5f, 0.5f, 0.5f),
            new Vector3(0.5f, -0.5f, 0.5f),
            new Vector3(0.5f, 0.5f, 0.5f)
        };
        mesh.SetIndices(new int[] {
            0, 1, 0, 2, 0, 3,
            7, 6, 7, 5, 7, 4,
            1, 4, 1, 6,
            2, 4, 2, 5,
            3, 5, 3, 6
        }, MeshTopology.Lines, 0);

        return mesh;
    }

    private static Mesh GenerateWireSphere(int res)
    {
        Mesh mesh = new Mesh() { name = "gizmoSphereMesh" };

        if (res < 4) { res = 4; }

        Vector3[] verts = new Vector3[res * 3];
        int[] lines = new int[res * 2 * 3];
        float angle = 360f / res;

        for (int i = 0; i < res; i++)
        {
            verts[i] = Quaternion.Euler(i * angle, 0f, 0f) * Vector3.up;
            verts[i + res] = Quaternion.Euler(0f, i * angle, 0f) * Vector3.forward;
            verts[i + res * 2] = Quaternion.Euler(0f, 0f, i * angle) * Vector3.up;
        }

        for (int i = 0; i < res; i++)
        {
            lines[i * 2] = i;
            lines[i * 2 + 1] = i + 1;
        }
        lines[res * 2 - 1] = 0;

        for (int i = res; i < (res * 2); i++)
        {
            lines[i * 2] = i;
            lines[i * 2 + 1] = i + 1;
        }
        lines[res * 2 * 2 - 1] = res;

        for (int i = (res * 2); i < (res * 3); i++)
        {
            lines[i * 2] = i;
            lines[i * 2 + 1] = i + 1;
        }
        lines[res * 2 * 3 - 1] = res * 2;

        mesh.vertices = verts;
        mesh.SetIndices(lines, MeshTopology.Lines, 0);

        return mesh;
    }

    private static Mesh GenerateLineMesh()
    {
        Mesh mesh = new Mesh() { name = "gizmoLineMesh" };
        mesh.vertices = new Vector3[] { Vector3.zero, Vector3.zero };
        mesh.SetIndices(new int[] { 0, 1 }, MeshTopology.Lines, 0);
        return mesh;
    }

    private static Mesh GenerateTriMesh()
    {
        Mesh mesh = new Mesh() { name = "gizmoTriMesh" };
        mesh.vertices = new Vector3[] { Vector3.zero, Vector3.zero, Vector3.zero };
        mesh.SetIndices(new int[] { 0, 1, 2 }, MeshTopology.Triangles, 0);
        return mesh;
    }

    public struct PreProps
    {
        public float3 t, r, s;
        public Color c;

        public PreProps(in float3 t, in float3 r, in float3 s, in Color c)
        {
            this.t = t;
            this.r = r;
            this.s = s;
            this.c = c;
        }
    }

    public struct Props
    {
        public float4x4 trs;
        public Vector4 color;

        public Props(in float4x4 trs, in Color color)
        {
            this.trs = trs;
            this.color = color;
        }

        public readonly static int Size = 80;
    }

    public struct PropsLine
    {
        public float3 a, b;
        public Vector4 color;

        public PropsLine(in float3 a, in float3 b, in Color color)
        {
            this.a = a;
            this.b = b;
            this.color = color;
        }

        public PropsLine(in float3 a, in float3 b, in Vector4 color)
        {
            this.a = a;
            this.b = b;
            this.color = color;
        }

        public readonly static int Size = 40;
    }

    public struct PropsTri
    {
        public float3 a, b, c;
        public Vector4 color;

        public PropsTri(in float3 a, in float3 b, in float3 c, in Color color)
        {
            this.a = a;
            this.b = b;
            this.c = c;
            this.color = color;
        }

        public readonly static int Size = 52;
    }


    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct PropsCalcJob : IJobFor
    {
        [ReadOnly]
        public NativeArray<PreProps> preprops;
        [WriteOnly]
        public NativeArray<Props> buffer;

        public void Execute(int index)
        {
            var p = preprops[index];
            buffer[index] = new Props(float4x4.TRS(p.t, quaternion.Euler(math.radians(p.r)), p.s), p.c);
        }
    }
}
