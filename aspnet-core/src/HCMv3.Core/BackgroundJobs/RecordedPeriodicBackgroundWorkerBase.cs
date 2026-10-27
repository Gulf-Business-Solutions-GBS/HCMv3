using System;
using System.Threading.Tasks;
using Abp.Threading.BackgroundWorkers;
using Abp.Threading.Timers;
using Abp.Timing;

namespace HCMv3.BackgroundJobs;

/// <summary>
/// A periodic background task whose every run is recorded, so the Background Jobs screen can say when it last ran and
/// whether it worked.
///
/// <para>
/// <b>The task's work goes in <see cref="DoRecordedWork"/>.</b> <c>DoWork</c> is sealed here so a task cannot skip the
/// recording by overriding it -- the failure being guarded against is a task that silently stops being watched.
/// </para>
///
/// <para>
/// <b>The error is rethrown after it is recorded.</b> ABP's timer logs a failed run; recording it must not swallow that,
/// or the log -- which has the stack trace -- would lose it.
/// </para>
///
/// <para>
/// The recorder is property-injected so the nine existing tasks keep their constructors. BackgroundJobs_Tests resolves
/// each registered task and checks the real recorder arrived, because a property that failed to inject would leave the
/// null recorder in place and the task unwatched without any error.
/// </para>
/// </summary>
public abstract class RecordedPeriodicBackgroundWorkerBase : PeriodicBackgroundWorkerBase
{
    protected RecordedPeriodicBackgroundWorkerBase(AbpTimer timer)
        : base(timer)
    {
    }

    public IBackgroundWorkerRecorder WorkerRecorder { get; set; } = NullBackgroundWorkerRecorder.Instance;

    protected sealed override void DoWork()
    {
        var startTime = Clock.Now;

        try
        {
            DoRecordedWork();
        }
        catch (Exception ex)
        {
            WorkerRecorder.RecordRun(GetType().Name, Timer.Period, startTime, ex);
            throw;
        }

        WorkerRecorder.RecordRun(GetType().Name, Timer.Period, startTime, null);
    }

    /// <summary>What the task does on each run.</summary>
    protected abstract void DoRecordedWork();
}

/// <inheritdoc cref="RecordedPeriodicBackgroundWorkerBase"/>
public abstract class RecordedAsyncPeriodicBackgroundWorkerBase : AsyncPeriodicBackgroundWorkerBase
{
    protected RecordedAsyncPeriodicBackgroundWorkerBase(AbpAsyncTimer timer)
        : base(timer)
    {
    }

    public IBackgroundWorkerRecorder WorkerRecorder { get; set; } = NullBackgroundWorkerRecorder.Instance;

    protected sealed override async Task DoWorkAsync()
    {
        var startTime = Clock.Now;

        try
        {
            await DoRecordedWorkAsync();
        }
        catch (Exception ex)
        {
            await WorkerRecorder.RecordRunAsync(GetType().Name, Timer.Period, startTime, ex);
            throw;
        }

        await WorkerRecorder.RecordRunAsync(GetType().Name, Timer.Period, startTime, null);
    }

    /// <summary>What the task does on each run.</summary>
    protected abstract Task DoRecordedWorkAsync();
}
