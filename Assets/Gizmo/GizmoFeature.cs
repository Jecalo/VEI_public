using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;

public class GizmoFeature : ScriptableRendererFeature
{
    class GizmoPass : ScriptableRenderPass
    {
        private ComputeBuffer buffer_cubes;
        private ComputeBuffer buffer_spheres;
        private ComputeBuffer buffer_lines;
        private ComputeBuffer buffer_tris;
        private ComputeBuffer argsBuffer;
        private ComputeBuffer argsBuffer2;
        private ComputeBuffer argsBuffer3;
        private ComputeBuffer argsBuffer4;

        private int propID;

        public bool Initialized { get; private set; }

        public GizmoPass()
        {
            if (!GizmoInternal.CheckState()) { Initialized = false; return; }

            Initialized = true;

            renderPassEvent = RenderPassEvent.AfterRendering;

            buffer_cubes = new ComputeBuffer(GizmoInternal.DefaultGizmoBufferSize, GizmoInternal.Props.Size, ComputeBufferType.Structured, ComputeBufferMode.SubUpdates);
            buffer_spheres = new ComputeBuffer(GizmoInternal.DefaultGizmoBufferSize, GizmoInternal.Props.Size, ComputeBufferType.Structured, ComputeBufferMode.SubUpdates);
            buffer_lines = new ComputeBuffer(GizmoInternal.DefaultGizmoBufferSize, GizmoInternal.PropsLine.Size, ComputeBufferType.Structured, ComputeBufferMode.SubUpdates);
            buffer_tris = new ComputeBuffer(GizmoInternal.DefaultGizmoBufferSize, GizmoInternal.PropsTri.Size, ComputeBufferType.Structured, ComputeBufferMode.SubUpdates);
            argsBuffer = new ComputeBuffer(1, 20, ComputeBufferType.IndirectArguments);
            argsBuffer2 = new ComputeBuffer(1, 20, ComputeBufferType.IndirectArguments);
            argsBuffer3 = new ComputeBuffer(1, 20, ComputeBufferType.IndirectArguments);
            argsBuffer4 = new ComputeBuffer(1, 20, ComputeBufferType.IndirectArguments);

            propID = Shader.PropertyToID("_Props");
            GizmoInternal.Material_Cubes.SetBuffer(propID, buffer_cubes);
            GizmoInternal.Material_Spheres.SetBuffer(propID, buffer_spheres);
            GizmoInternal.Material_Lines.SetBuffer(propID, buffer_lines);
            GizmoInternal.Material_Tris.SetBuffer(propID, buffer_tris);

#if UNITY_EDITOR
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += () => {
                if (buffer_cubes != null) { buffer_cubes.Release(); buffer_cubes = null; }
                if (buffer_spheres != null) { buffer_spheres.Release(); buffer_spheres = null; }
                if (buffer_lines != null) { buffer_lines.Release(); buffer_lines = null; }
                if (buffer_tris != null) { buffer_tris.Release(); buffer_tris = null; }
                if (argsBuffer != null) { argsBuffer.Release(); argsBuffer = null; }
                if (argsBuffer2 != null) { argsBuffer2.Release(); argsBuffer2 = null; }
                if (argsBuffer3 != null) { argsBuffer3.Release(); argsBuffer3 = null; }
                if (argsBuffer4 != null) { argsBuffer4.Release(); argsBuffer4 = null; }
            };

            if (UnityEditor.EditorApplication.isPlaying) { UpdateInjector.OnPreUpdate += () => { GizmoInternal.EndOfFrame(); }; }
#else
            UpdateInjector.OnPreUpdate += () => { GizmoInternal.EndOfFrame(); };
#endif
        }

        private void DrawCubes(ref CommandBuffer cmd)
        {
            int count = GizmoInternal.Cubes.Length;
            if (count == 0) { return; }

            if (count > buffer_cubes.count)
            {
                int newSize = buffer_cubes.count;
                while (newSize < count) { newSize *= 2; }

                buffer_cubes.Release();
                buffer_cubes = new ComputeBuffer(newSize, GizmoInternal.Props.Size, ComputeBufferType.Structured, ComputeBufferMode.SubUpdates);
                GizmoInternal.Material_Cubes.SetBuffer(propID, buffer_cubes);
            }

            uint[] args = new uint[5] { GizmoInternal.CubeMesh.GetIndexCount(0), (uint)count, 0, 0, 0 };
            argsBuffer.SetData(args);

            var job = new GizmoInternal.PropsCalcJob() {
                preprops = GizmoInternal.Cubes.AsArray(),
                buffer = buffer_cubes.BeginWrite<GizmoInternal.Props>(0, count) };
            job.ScheduleParallel(count, 16, default).Complete();
            buffer_cubes.EndWrite<GizmoInternal.Props>(count);

            cmd.DrawMeshInstancedIndirect(GizmoInternal.CubeMesh, 0, GizmoInternal.Material_Cubes, -1, argsBuffer);
        }

        private void DrawSpheres(ref CommandBuffer cmd)
        {
            int count = GizmoInternal.Spheres.Length;
            if (count == 0) { return; }

            if (count > buffer_spheres.count)
            {
                int newSize = buffer_spheres.count;
                while (newSize < count) { newSize *= 2; }

                buffer_spheres.Release();
                buffer_spheres = new ComputeBuffer(newSize, GizmoInternal.Props.Size, ComputeBufferType.Structured, ComputeBufferMode.SubUpdates);
                GizmoInternal.Material_Spheres.SetBuffer(propID, buffer_spheres);
            }

            uint[] args = new uint[5] { GizmoInternal.SphereMesh.GetIndexCount(0), (uint)count, 0, 0, 0 };
            argsBuffer2.SetData(args);

            var job = new GizmoInternal.PropsCalcJob()
            {
                preprops = GizmoInternal.Spheres.AsArray(),
                buffer = buffer_spheres.BeginWrite<GizmoInternal.Props>(0, count)
            };
            job.ScheduleParallel(count, 16, default).Complete();
            buffer_spheres.EndWrite<GizmoInternal.Props>(count);

            cmd.DrawMeshInstancedIndirect(GizmoInternal.SphereMesh, 0, GizmoInternal.Material_Spheres, -1, argsBuffer2);
        }

        private void DrawLines(ref CommandBuffer cmd)
        {
            int count = GizmoInternal.Lines.Length;
            if (count == 0) { return; }

            if (count > buffer_lines.count)
            {
                int newSize = buffer_lines.count;
                while (newSize < count) { newSize *= 2; }

                buffer_lines.Release();
                buffer_lines = new ComputeBuffer(newSize, GizmoInternal.PropsLine.Size, ComputeBufferType.Structured, ComputeBufferMode.SubUpdates);
                GizmoInternal.Material_Lines.SetBuffer(propID, buffer_lines);
            }

            uint[] args = new uint[5] { 2, (uint)count, 0, 0, 0 };
            argsBuffer3.SetData(args);

            var b = buffer_lines.BeginWrite<GizmoInternal.PropsLine>(0, count);
            b.CopyFrom(GizmoInternal.Lines.AsArray());
            buffer_lines.EndWrite<GizmoInternal.PropsLine>(count);

            cmd.DrawMeshInstancedIndirect(GizmoInternal.LineMesh, 0, GizmoInternal.Material_Lines, -1, argsBuffer3);
        }

        private void DrawTris(ref CommandBuffer cmd)
        {
            int count = GizmoInternal.Tris.Length;
            if (count == 0) { return; }

            if (count > buffer_tris.count)
            {
                int newSize = buffer_tris.count;
                while (newSize < count) { newSize *= 2; }

                buffer_tris.Release();
                buffer_tris = new ComputeBuffer(newSize, GizmoInternal.PropsTri.Size, ComputeBufferType.Structured, ComputeBufferMode.SubUpdates);
                GizmoInternal.Material_Tris.SetBuffer(propID, buffer_tris);
            }

            uint[] args = new uint[5] { 3, (uint)count, 0, 0, 0 };
            argsBuffer4.SetData(args);

            var b = buffer_tris.BeginWrite<GizmoInternal.PropsTri>(0, count);
            b.CopyFrom(GizmoInternal.Tris.AsArray());
            buffer_tris.EndWrite<GizmoInternal.PropsTri>(count);

            cmd.DrawMeshInstancedIndirect(GizmoInternal.TriMesh, 0, GizmoInternal.Material_Tris, -1, argsBuffer4);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(name: "GizmoPass");

            DrawCubes(ref cmd);
            DrawSpheres(ref cmd);
            DrawLines(ref cmd);
            DrawTris(ref cmd);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }


    private GizmoPass renderPass;

    public override void Create()
    {
#if UNITY_EDITOR
        if (UnityEditor.EditorApplication.isPlaying) { renderPass = new GizmoPass(); }
#else
        renderPass = new GizmoPass();
#endif
    }


    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
#if UNITY_EDITOR
        if (UnityEditor.EditorApplication.isPlaying && renderPass.Initialized) { renderer.EnqueuePass(renderPass); }
#else
        if (renderPass.Initialized) { renderer.EnqueuePass(renderPass); }
#endif
    }
}