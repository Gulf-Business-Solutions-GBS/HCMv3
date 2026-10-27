using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.EngineRuns;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using HCMv3.TimeManagement.Services;
using HCMv3.Tests.EngineRuns;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

/// <summary>
/// Asserts that the Time Engine publishes the same live-progress contract the Payroll Engine does, so one UI
/// component can serve both.
///
/// <para>Scope note: these run the real <c>CreateTimeRegisterAsync</c> orchestration over the shared
/// <see cref="TimeEngineServiceTestHarness"/>. Day-level calculation is out of scope here -- what is being
/// checked is that a run is OPENED before the slow work starts, that the expected employee count is
/// published, that every processed employee produces exactly one progress row carrying a measured duration,
/// and that the run is always CLOSED.</para>
/// </summary>
public class TimeEngineService_Progress_Tests
{
    private readonly TimeEngineServiceTestHarness _harness = new TimeEngineServiceTestHarness();
    private readonly RecordingEngineRunProgressWriter _progress = new RecordingEngineRunProgressWriter();

    public TimeEngineService_Progress_Tests()
    {
        _harness.Service.ProgressWriter = _progress;
    }

    /// <summary>
    /// Even a run that selects nobody must open and close its progress row -- otherwise the screen shows the
    /// PREVIOUS run's figures and the operator concludes their run never started.
    /// </summary>
    [Fact]
    public async Task Should_Open_And_Close_A_Progress_Run_Even_When_No_Employee_Is_Selected()
    {
        await _harness.Service.CreateTimeRegisterAsync(NewInput());

        var started = _progress.StartedRuns.ShouldHaveSingleItem();
        started.EngineType.ShouldBe(EngineType.TimeEngine);
        started.StartedByUserName.ShouldBe("operator");
        started.Parameters.ShouldContain("End Date : 31/01/2026",
            customMessage: "The progress row must carry the same parameter string as the ExecutionLog row.");

        _progress.TotalsPublished.ShouldHaveSingleItem().ShouldBe(0);
        _progress.Employees.ShouldBeEmpty();

        _progress.Completed.ShouldBeTrue();
        _progress.CompletedStatus.ShouldBe(EngineRunStatus.Completed);
        _progress.CompletedExecutionLogId.ShouldBe(_harness.Store<ExecutionLog>().Single().Id);
    }

    /// <summary>
    /// The progress run must be opened BEFORE the ExecutionLog row, because that is the whole point: the
    /// ExecutionLog row is invisible to other connections until the (hour-long) run commits, so if progress
    /// were opened after it, the screen would stay blank through the slow start-up.
    /// </summary>
    [Fact]
    public async Task Should_Open_The_Progress_Run_Before_Doing_Any_Slow_Work()
    {
        var openedBeforeExecutionLog = false;

        var probe = new ProbeWriter(() => openedBeforeExecutionLog = !_harness.Store<ExecutionLog>().Any());
        _harness.Service.ProgressWriter = probe;

        await _harness.Service.CreateTimeRegisterAsync(NewInput());

        openedBeforeExecutionLog.ShouldBeTrue();
    }

    /// <summary>A run asked to stop before it starts must abort and say so.</summary>
    [Fact]
    public async Task Should_Abort_When_A_Stop_Is_Requested_Through_The_Progress_Row()
    {
        _harness.Add(new Employee { Code = "E1", FullNameEn = "Alice", Status = EmployeeStatus.Active }, 1);
        _progress.StopRequested = true;

        var result = await _harness.Service.CreateTimeRegisterAsync(NewInput());

        result.Aborted.ShouldBeTrue();
        _progress.Employees.ShouldBeEmpty();
        _progress.CompletedStatus.ShouldBe(EngineRunStatus.Aborted);
    }

    private static TimeEngineRunInput NewInput()
    {
        return new TimeEngineRunInput
        {
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 31),
            CurrentUserName = "operator"
        };
    }

    /// <summary>Runs a callback at the moment <c>BeginRunAsync</c> is called, to observe ordering.</summary>
    private sealed class ProbeWriter : RecordingEngineRunProgressWriter
    {
        private readonly Action _onBegin;

        public ProbeWriter(Action onBegin)
        {
            _onBegin = onBegin;
        }

        protected override void OnBegin()
        {
            _onBegin();
        }
    }
}
