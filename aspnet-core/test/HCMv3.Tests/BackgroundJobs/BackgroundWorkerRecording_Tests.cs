using System;
using System.Linq;
using System.Reflection;
using Abp.Threading.Timers;
using HCMv3.BackgroundJobs;
using HCMv3.Test.Base;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.BackgroundJobs;

/// <summary>
/// Recording the runs of scheduled tasks: the recorder, the base class that calls it, and -- the part most likely to
/// break silently -- that every real task actually has the recorder.
/// </summary>
// ReSharper disable once InconsistentNaming
public class BackgroundWorkerRecording_Tests : AppTestBase
{
    private readonly IBackgroundWorkerRecorder _recorder;

    public BackgroundWorkerRecording_Tests()
    {
        _recorder = Resolve<IBackgroundWorkerRecorder>();
    }

    // ---------------------------------------------------------------- the recorder

    [Fact]
    public void Should_Record_A_Successful_Run()
    {
        _recorder.RecordRun("OkWorker", 60000, DateTime.Now.AddSeconds(-2), null);

        var status = Status("OkWorker");
        status.RunCount.ShouldBe(1);
        status.LastSucceeded.ShouldBe(true);
        status.LastSuccessTime.ShouldNotBeNull();
        status.FailureCount.ShouldBe(0);
        status.IntervalMilliseconds.ShouldBe(60000);
    }

    [Fact]
    public void Should_Update_The_Same_Row_On_Every_Run()
    {
        // One row per task, not one per run -- two tasks run every few minutes.
        _recorder.RecordRun("RepeatWorker", 1000, DateTime.Now, null);
        _recorder.RecordRun("RepeatWorker", 1000, DateTime.Now, null);
        _recorder.RecordRun("RepeatWorker", 1000, DateTime.Now, null);

        UsingDbContext(null, c => c.BackgroundWorkerStatuses.Count(s => s.WorkerName == "RepeatWorker")).ShouldBe(1);
        Status("RepeatWorker").RunCount.ShouldBe(3);
    }

    [Fact]
    public void Should_Record_A_Failure_With_Its_Reason()
    {
        _recorder.RecordRun("BadWorker", 1000, DateTime.Now, new InvalidOperationException("outer", new TimeoutException("the database timed out")));

        var status = Status("BadWorker");
        status.LastSucceeded.ShouldBe(false);
        status.FailureCount.ShouldBe(1);
        status.LastError.ShouldContain("the database timed out");
    }

    [Fact]
    public void Should_Keep_The_Last_Error_After_A_Later_Success()
    {
        // A fault that comes and goes should stay visible, not vanish with the next good run.
        _recorder.RecordRun("FlakyWorker", 1000, DateTime.Now, new Exception("intermittent"));
        _recorder.RecordRun("FlakyWorker", 1000, DateTime.Now, null);

        var status = Status("FlakyWorker");
        status.LastSucceeded.ShouldBe(true);
        status.LastError.ShouldContain("intermittent");
        status.LastFailureTime.ShouldNotBeNull();
        status.RunCount.ShouldBe(2);
        status.FailureCount.ShouldBe(1);
    }

    [Fact]
    public void Should_Cut_An_Overlong_Error_To_Fit()
    {
        _recorder.RecordRun("VerboseWorker", 1000, DateTime.Now, new Exception(new string('x', 10000)));

        Status("VerboseWorker").LastError.Length.ShouldBe(BackgroundJobsConsts.MaxErrorLength);
    }

    // ---------------------------------------------------------------- the base class

    [Fact]
    public void Should_Record_A_Run_Of_A_Recorded_Worker()
    {
        var worker = new SampleWorker(Resolve<AbpTimer>(), fail: false) { WorkerRecorder = _recorder };

        InvokeDoWork(worker);

        worker.Ran.ShouldBeTrue();
        Status(nameof(SampleWorker)).LastSucceeded.ShouldBe(true);
    }

    [Fact]
    public void Should_Record_And_Rethrow_A_Failing_Run()
    {
        // Rethrown so ABP's timer still logs it with the stack trace.
        var worker = new SampleWorker(Resolve<AbpTimer>(), fail: true) { WorkerRecorder = _recorder };

        Should.Throw<InvalidOperationException>(() => InvokeDoWork(worker));

        var status = Status(nameof(SampleWorker));
        status.LastSucceeded.ShouldBe(false);
        status.LastError.ShouldContain("sample failure");
    }

    [Fact]
    public void Should_Give_Every_Registered_Task_The_Real_Recorder()
    {
        // The recorder is property-injected. If injection failed, the task would keep the null recorder and run
        // unwatched with no error at all -- so each real task is resolved and checked.
        var names = BackgroundJobsAppService.KnownWorkerNames();
        names.ShouldContain("TimeEngineAutorunBackgroundWorker");
        names.ShouldContain("WorkflowEscalationWorker");
        names.ShouldContain("ExpiredSessionCleanupWorker");

        var workerTypes = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith("HCMv3", StringComparison.Ordinal) == true)
            .SelectMany(a => a.GetTypes())
            .Where(t => !t.IsAbstract && names.Contains(t.Name))
            .ToList();

        workerTypes.Count.ShouldBeGreaterThanOrEqualTo(9);

        // Only tasks the test container can build. A few depend on services registered by the web host alone -- the
        // unpaid-subscription reminder needs the payment URL generator -- and cannot be built here at all. Checked by
        // handler state rather than by catching exceptions, so a task that fails for any other reason still fails.
        var buildable = workerTypes
            .Where(t => LocalIocManager.IsRegistered(t)
                        && LocalIocManager.IocContainer.Kernel.GetHandler(t)?.CurrentState == Castle.MicroKernel.HandlerState.Valid)
            .ToList();

        // The tasks this screen matters most for must be among them, so the filter cannot quietly skip everything.
        foreach (var required in new[]
                 {
                     "TimeEngineAutorunBackgroundWorker", "WorkflowEscalationWorker",
                     "ReportScheduleDispatcherWorker", "ExpiredAuditLogDeleterWorker"
                 })
        {
            buildable.ShouldContain(t => t.Name == required, $"{required} could not be built in the test container");
        }

        foreach (var type in buildable)
        {
            var worker = LocalIocManager.Resolve(type);
            var recorder = type.GetProperty("WorkerRecorder")!.GetValue(worker);

            recorder.ShouldBeOfType<BackgroundWorkerRecorder>($"{type.Name} did not receive the recorder");
        }
    }

    // ---------------------------------------------------------------- helpers

    private BackgroundWorkerStatus Status(string name)
    {
        return UsingDbContext(null, c => c.BackgroundWorkerStatuses.Single(s => s.WorkerName == name));
    }

    private static void InvokeDoWork(object worker)
    {
        var method = worker.GetType().GetMethod("DoWork", BindingFlags.Instance | BindingFlags.NonPublic)!;

        try
        {
            method.Invoke(worker, null);
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            throw ex.InnerException;
        }
    }

    private sealed class SampleWorker : RecordedPeriodicBackgroundWorkerBase
    {
        private readonly bool _fail;

        public SampleWorker(AbpTimer timer, bool fail)
            : base(timer)
        {
            _fail = fail;
            Timer.Period = 5000;
        }

        public bool Ran { get; private set; }

        protected override void DoRecordedWork()
        {
            Ran = true;

            if (_fail)
            {
                throw new InvalidOperationException("sample failure");
            }
        }
    }
}
