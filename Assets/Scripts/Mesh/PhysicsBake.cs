using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

public static class PhysicsBake
{
    public static JobWrapper<JobSingle> GenerateJob(Mesh mesh)
    {
        JobSingle job = new JobSingle();
        job.id = mesh.GetInstanceID();

        return new JobWrapper<JobSingle>(job);
    }

    public static void Bake(List<Mesh> meshes)
    {
        Job job = new();
        job.ids = new NativeArray<int>(meshes.Count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        int j = 0;
        for (int i = 0; i < meshes.Count; i++)
        {
            if (meshes[i].GetIndexCount(0) == 0) { continue; }
            job.ids[j++] = meshes[i].GetInstanceID();
        }

        job.ScheduleParallel(j, 1, default).Complete();

        job.ids.Dispose();
    }

    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct Job : IJobFor
    {
        [ReadOnly]
        public NativeArray<int> ids;

        public void Execute(int index)
        {
            Physics.BakeMesh(ids[index], false);
        }
    }

    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct JobSingle : IJob
    {
        [ReadOnly]
        public int id;

        public void Execute()
        {
            Physics.BakeMesh(id, false);
        }
    }
}
