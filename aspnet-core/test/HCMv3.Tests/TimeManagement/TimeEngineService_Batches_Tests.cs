using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Abp.Domain.Uow;
using HCMv3.EngineRuns;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using HCMv3.SystemSettings;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Services;
using HCMv3.Tests.EngineRuns;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

/// <summary>
/// SAVE IN BATCHES and FAIL, DON'T HANG, at the engine level. Legacy committed per employee and every 500 changes
/// (<c>HCM10.TimeEngine/Program.cs</c> lines 1599-1602, 1705-1711); HCMv3 kept the whole run in one unit of work
/// and closed the run as Completed before anything was saved. Now: <see cref="TimeEngineRunInput.CommitBatchSize"/>
/// employees per <c>RequiresNew</c> unit of work, the run marked finished only after the last save, and a run that
/// fails -- before its loop or in it -- closed as Failed with the reason instead of left Running.
///
/// <para>The unit-of-work manager is a double here, so the ORDER of saves and progress writes can be observed.
/// That the figures are the same as in one unit of work is proved against a real database by
/// <c>TimeEngineBatches_Integration_Tests</c>.</para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class TimeEngineService_Batches_Tests
{
    private readonly TimeEngineServiceTestHarness _harness = new TimeEngineServiceTestHarness();
    private readonly RecordingEngineRunProgressWriter _progress = new RecordingEngineRunProgressWriter();
    private readonly IUnitOfWorkManager _unitOfWorkManager = Substitute.For<IUnitOfWorkManager>();
    private readonly IActiveUnitOfWork _outerUnitOfWork = Substitute.For<IActiveUnitOfWork>();

    /// <summary>What was true at each batch save: "employees on screen:run closed".</summary>
    private readonly List<string> _saves = new List<string>();

    /// <summary>1-based number of the batch save that throws; 0 = none.</summary>
    private int _failingSave;

    public TimeEngineService_Batches_Tests()
    {
        _harness.Service.ProgressWriter = _progress;
        _harness.Service.UnitOfWorkManager = _unitOfWorkManager;

        _unitOfWorkManager.Current.Returns(_outerUnitOfWork);
        _unitOfWorkManager.Begin(Arg.Any<TransactionScopeOption>()).Returns(_ => NewBatchUnitOfWork());

        // No joining date: each employee fails at once with legacy's "JoiningDate not found." -- enough to give
        // every employee a progress row without a whole time-register fixture.
        _harness.Add(new Employee { Code = "E1", FullNameEn = "One", Status = EmployeeStatus.Active }, 1);
        _harness.Add(new Employee { Code = "E2", FullNameEn = "Two", Status = EmployeeStatus.Active }, 2);
        _harness.Add(new Employee { Code = "E3", FullNameEn = "Three", Status = EmployeeStatus.Active }, 3);
    }

    [Fact]
    public void Should_Save_Ten_Employees_At_A_Time_In_The_Job()
    {
        TimeEngineBatches.Size.ShouldBe(10);
    }

    /// <summary>Go-Live Setup and the payroll engine's autorun pass nothing and keep their single unit of work.</summary>
    [Fact]
    public async Task Should_Keep_The_Whole_Run_In_The_Callers_Unit_Of_Work_By_Default()
    {
        await _harness.Service.CreateTimeRegisterAsync(NewInput(commitBatchSize: 0));

        _unitOfWorkManager.DidNotReceive().Begin(Arg.Any<TransactionScopeOption>());
        await _outerUnitOfWork.DidNotReceive().SaveChangesAsync();
        _progress.Employees.Count.ShouldBe(3);
    }

    [Fact]
    public async Task Should_Save_In_Batches_Of_The_Given_Size()
    {
        await _harness.Service.CreateTimeRegisterAsync(NewInput(commitBatchSize: 2));

        _unitOfWorkManager.Received(2).Begin(TransactionScopeOption.RequiresNew);
        _saves.Count.ShouldBe(2, "three employees in batches of two are a full batch and a part one");
    }

    [Fact]
    public async Task Should_Report_A_Batched_Run_On_One_Progress_Row()
    {
        await _harness.Service.CreateTimeRegisterAsync(NewInput(commitBatchSize: 2));

        _progress.StartedRuns.ShouldHaveSingleItem();
        _progress.TotalsPublished.ShouldBe(new[] { 3 });
        _progress.Employees.Select(e => e.SequenceNo).ShouldBe(new[] { 1, 2, 3 });
        _progress.Employees.Select(e => e.EmployeeCode).ShouldBe(new[] { "E1", "E2", "E3" });
    }

    /// <summary>
    /// The run is closed only after the last batch is saved, and the execution log -- the one thing left in the
    /// caller's unit of work -- is saved before that too.
    /// </summary>
    [Fact]
    public async Task Should_Mark_The_Run_Finished_Only_After_The_Last_Batch_Is_Saved()
    {
        await _harness.Service.CreateTimeRegisterAsync(NewInput(commitBatchSize: 2));

        _saves.ShouldAllBe(s => s.EndsWith(":False"), "no batch may be saved after the run was closed");
        _progress.Completed.ShouldBeTrue();
        await _outerUnitOfWork.Received(1).SaveChangesAsync();
    }

    /// <summary>
    /// A batch's employees reach the screen only once the batch is saved: a batch that then failed to save would
    /// otherwise leave them shown as processed when nothing of theirs was kept.
    /// </summary>
    [Fact]
    public async Task Should_Hold_Back_A_Batches_Progress_Until_It_Is_Saved()
    {
        await _harness.Service.CreateTimeRegisterAsync(NewInput(commitBatchSize: 2));

        _saves.ShouldBe(new[] { "0:False", "2:False" });
        _progress.Employees.Count.ShouldBe(3);
    }

    [Fact]
    public async Task Should_Report_The_Same_Employees_In_Batches_As_In_One_Unit_Of_Work()
    {
        var single = new TimeEngineServiceTestHarness();
        var singleProgress = new RecordingEngineRunProgressWriter();
        single.Service.ProgressWriter = singleProgress;
        single.Add(new Employee { Code = "E1", FullNameEn = "One", Status = EmployeeStatus.Active }, 1);
        single.Add(new Employee { Code = "E2", FullNameEn = "Two", Status = EmployeeStatus.Active }, 2);
        single.Add(new Employee { Code = "E3", FullNameEn = "Three", Status = EmployeeStatus.Active }, 3);

        var inOne = await single.Service.CreateTimeRegisterAsync(NewInput(commitBatchSize: 0));
        var inBatches = await _harness.Service.CreateTimeRegisterAsync(NewInput(commitBatchSize: 2));

        Describe(_progress).ShouldBe(Describe(singleProgress));
        inBatches.TotalRecords.ShouldBe(inOne.TotalRecords);
        inBatches.Failed.ShouldBe(inOne.Failed);
        _progress.CompletedStatus.ShouldBe(singleProgress.CompletedStatus);
    }

    /// <summary>
    /// Legacy: a commit that failed escaped the employee's finally and ended the run through the outer catch. Here
    /// the batch that will not save ends the run as Failed with the reason; its employees are reported as not
    /// saved, and what earlier batches saved stays reported as it was.
    /// </summary>
    [Fact]
    public async Task Should_Fail_The_Run_When_A_Batch_Will_Not_Save()
    {
        _failingSave = 2;

        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => _harness.Service.CreateTimeRegisterAsync(NewInput(commitBatchSize: 2)));

        exception.Data.Contains(TimeEngineService.RunFailureRecordedKey).ShouldBeTrue();
        _progress.CompletedStatus.ShouldBe(EngineRunStatus.Failed);
        _progress.CompletedFailureMessage.ShouldContain("disk full");

        _progress.Employees.Count.ShouldBe(3);
        _progress.Employees[0].Message.ShouldBe("JoiningDate not found.");
        _progress.Employees[2].Succeeded.ShouldBeFalse();
        _progress.Employees[2].Message.ShouldStartWith("Not saved: ");
        _progress.Employees[2].Message.ShouldContain("disk full");
    }

    /// <summary>
    /// A run that fails before its loop (here: the time settings cannot be read) is closed as Failed with the
    /// reason, not left "Running" until its heartbeat ages out -- and the caller is told it is already recorded.
    /// </summary>
    [Fact]
    public async Task Should_Mark_A_Run_That_Fails_Before_Its_Loop_As_Failed()
    {
        _harness.SysSettingsAccessor.GetTimeSettingsAsync().Returns(Task.FromResult<ManageTimeSettings>(null));

        var exception = await Should.ThrowAsync<NullReferenceException>(
            () => _harness.Service.CreateTimeRegisterAsync(NewInput(commitBatchSize: 2)));

        exception.Data.Contains(TimeEngineService.RunFailureRecordedKey).ShouldBeTrue();
        _progress.StartedRuns.ShouldHaveSingleItem();
        _progress.Completed.ShouldBeTrue();
        _progress.CompletedStatus.ShouldBe(EngineRunStatus.Failed);
        _progress.CompletedFailureMessage.ShouldNotBeNullOrWhiteSpace();
        _progress.Employees.ShouldBeEmpty();
    }

    /// <summary>
    /// Legacy's outer catch wrote a failed TimeLog row with no employee carrying the reason, and its finally still
    /// finalised the execution log (Program.cs lines 1716-1762). (The execution log's status itself is computed by
    /// the unchanged FinalizeExecutionLogAsync from those rows; the list-backed harness cannot read it back.)
    /// </summary>
    [Fact]
    public async Task Should_Log_The_Reason_Of_A_Run_That_Failed_After_Writing_Its_Execution_Log()
    {
        _harness.PayrollAreaResolver.GetEmployeeIdsInPayrollAreaAsync("Area A", Arg.Any<DateTime>())
            .Returns(Task.FromException<List<int>>(new InvalidOperationException("area lookup broke")));

        var input = NewInput(commitBatchSize: 2);
        input.PayrollAreaName = "Area A";

        await Should.ThrowAsync<InvalidOperationException>(() => _harness.Service.CreateTimeRegisterAsync(input));

        var log = _harness.Store<ExecutionLog>().ShouldHaveSingleItem();

        var timeLog = _harness.Store<TimeLog>().ShouldHaveSingleItem();
        timeLog.EmployeeId.ShouldBeNull();
        timeLog.Status.ShouldBe(1);
        timeLog.Message.ShouldBe("area lookup broke");

        _progress.CompletedStatus.ShouldBe(EngineRunStatus.Failed);
        _progress.CompletedExecutionLogId.ShouldBe(log.Id);
    }

    private IUnitOfWorkCompleteHandle NewBatchUnitOfWork()
    {
        var handle = Substitute.For<IUnitOfWorkCompleteHandle>();
        handle.CompleteAsync().Returns(_ =>
        {
            _saves.Add(_progress.Employees.Count + ":" + _progress.Completed);

            if (_failingSave == _saves.Count)
            {
                throw new InvalidOperationException("disk full");
            }

            return Task.CompletedTask;
        });

        return handle;
    }

    private static List<string> Describe(RecordingEngineRunProgressWriter progress)
    {
        return progress.Employees
            .Select(e => e.SequenceNo + "|" + e.EmployeeCode + "|" + e.Succeeded + "|" + e.Message)
            .ToList();
    }

    private static TimeEngineRunInput NewInput(int commitBatchSize)
    {
        return new TimeEngineRunInput
        {
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 31),
            CurrentUserName = "operator",
            CommitBatchSize = commitBatchSize
        };
    }
}
