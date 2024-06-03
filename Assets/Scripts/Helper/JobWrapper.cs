using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;


public class JobWrapper<T> where T : struct, IJob
{
    public T JobData;
    public JobHandle Handle { get; private set; }



    public JobWrapper(T job)
    {
        JobData = job;
    }

    public void Run()
    {
        JobData.Run();
    }

    public JobHandle Schedule()
    {
        Handle = JobData.Schedule(default);
        return Handle;
    }

    public JobHandle Schedule(JobHandle dependency)
    {
        Handle = JobData.Schedule(dependency);
        return Handle;
    }
}

public class JobForWrapper<T> where T : struct, IJobFor
{
    public T JobData;
    public JobHandle Handle { get; private set; }

    private readonly int jobLength;
    private readonly int jobBatchCount;

    public JobForWrapper(T job, int jobLength, int jobBatchCount)
    {
        JobData = job;
        this.jobLength = jobLength;
        this.jobBatchCount = jobBatchCount;
    }

    public void Run()
    {
        JobData.Run(jobLength);
    }

    public JobHandle Schedule()
    {
        Handle = JobData.ScheduleParallel(jobLength, jobBatchCount, default);
        return Handle;
    }

    public JobHandle Schedule(JobHandle dependency)
    {
        Handle = JobData.ScheduleParallel(jobLength, jobBatchCount, dependency);
        return Handle;
    }
}


//public interface IJobWrapper<T>
//{
//    public void Run();
//    public void Schedule();
//    public void Schedule(JobHandle dependency);
//    public JobHandle Handle { get; }
//}


//public class IJW<T> : IJobWrapper<T> where T : struct, IJob
//{
//    public readonly T job;
//    private JobHandle jobHandle;

//    public IJW(T job)
//    {
//        this.job = job;
//    }

//    public void Run()
//    {
//        job.Run();
//    }

//    public void Schedule()
//    {
//        jobHandle = job.Schedule(default);
//    }

//    public void Schedule(JobHandle dependency)
//    {
//        jobHandle = job.Schedule(dependency);
//    }

//    public JobHandle Handle { get { return jobHandle; } }
//}

//public class IJW_f<T> : IJobWrapper<T> where T : struct, IJobFor
//{
//    public readonly T job;
//    private readonly int jobLength;
//    private readonly int jobBatchCount;
//    private JobHandle jobHandle;

//    public IJW_f(T job, int jobLength, int jobBatchCount)
//    {
//        this.job = job;
//        this.jobLength = jobLength;
//        this.jobBatchCount = jobBatchCount;
//    }

//    public void Run()
//    {
//        job.Run(jobLength);
//    }

//    public void Schedule()
//    {
//        jobHandle = job.ScheduleParallel(jobLength, jobBatchCount, default);
//    }

//    public void Schedule(JobHandle dependency)
//    {
//        jobHandle = job.ScheduleParallel(jobLength, jobBatchCount, dependency);
//    }

//    public JobHandle Handle { get { return jobHandle; } }
//}