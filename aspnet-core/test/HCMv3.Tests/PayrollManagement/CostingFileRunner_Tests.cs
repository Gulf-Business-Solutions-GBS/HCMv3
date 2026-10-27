using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Uow;
using Abp.Localization;
using Abp.Runtime.Session;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Costing;
using HCMv3.SystemSecurity;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// <see cref="CostingFileRunner"/> and <see cref="CostingFileBackgroundJob"/>: legacy <c>Main</c> ->
/// <c>CreateCostRegister</c> as a background run that logs each employee as it finishes and never throws.
/// </summary>
// ReSharper disable once InconsistentNaming
public class CostingFileRunner_Tests : FinancePostingTestBase
{
    private readonly ICostEngineService _engine = Substitute.For<ICostEngineService>();

    private (CostingFileRunner Runner, FinanceRunLog Log) Create()
    {
        var log = CreateRunLog();
        return (new CostingFileRunner(_engine, log) { LocalizationManager = LocalIocManager.Resolve<ILocalizationManager>() }, log);
    }

    [Fact]
    public async Task Should_Write_A_Log_Row_For_Each_Employee_As_The_Engine_Finishes_It()
    {
        SeedArea();
        var e1 = AddEmployee("E1");
        var e2 = AddEmployee("E2");
        var (runner, log) = Create();
        var id = await log.OpenAsync(FinanceRunProcess.CostEngine, Input(), "admin");

        var rowsSeenAfterFirst = -1;
        _engine.RunAsync(Arg.Any<CostingRunInput>(), Arg.Any<ICostingRunObserver>()).Returns(async call =>
        {
            var observer = call.ArgAt<ICostingRunObserver>(1);
            await observer.OnEmployeeCompletedAsync(new CostingRunLogEntry { EmployeeId = e1, Status = 0, Message = "Successfull" }, 2);
            rowsSeenAfterFirst = Context.ProjectLogs.Count();
            await observer.OnEmployeeCompletedAsync(new CostingRunLogEntry { EmployeeId = e2, Status = 1, Message = "not configured" }, 2);
            return new CostingRunResult { TotalEmployees = 2, SuccessCount = 1, FailedCount = 1 };
        });

        await runner.RunAsync(FinanceRunJobArgs.From(Input(), TenantId, 2, id, null));

        // Visible mid-run, as legacy's per-employee commit was.
        rowsSeenAfterFirst.ShouldBe(1);

        var rows = LogRows();
        rows.Select(r => r.EmployeeId).ShouldBe(new int?[] { e1, e2 });
        rows.ShouldAllBe(r => r.NoOfEmployee == 2 && r.InPeriod == PayInDate);
        rows[1].Status.ShouldBe(1);

        Context.ExecutionLogs.Single().ExecutionStatus.ShouldBe(ExecutionLogStatus.PARTIAL);
    }

    [Fact]
    public async Task Should_Relay_The_Stop_Button_To_The_Engine()
    {
        SeedArea();
        var (runner, log) = Create();
        var id = await log.OpenAsync(FinanceRunProcess.CostEngine, Input(), "admin");

        bool? stopSeen = null;
        _engine.RunAsync(Arg.Any<CostingRunInput>(), Arg.Any<ICostingRunObserver>()).Returns(async call =>
        {
            await log.RequestStopAsync();
            stopSeen = await call.ArgAt<ICostingRunObserver>(1).IsStopRequestedAsync();
            return new CostingRunResult { Aborted = true };
        });

        await runner.RunAsync(FinanceRunJobArgs.From(Input(), TenantId, 2, id, null));

        stopSeen.ShouldBe(true);
        Context.ExecutionLogs.Single().ExecutionStatus.ShouldBe(ExecutionLogStatus.ABORTED);
    }

    /// <summary>Legacy Main's catch (lines 362-375): an employee-less failed row; the run is closed; nothing is thrown.</summary>
    [Fact]
    public async Task Should_Log_An_Engine_Failure_And_Not_Throw()
    {
        SeedArea();
        var (runner, log) = Create();
        var id = await log.OpenAsync(FinanceRunProcess.CostEngine, Input(), "admin");
        _engine.RunAsync(Arg.Any<CostingRunInput>(), Arg.Any<ICostingRunObserver>())
            .ThrowsAsync(new InvalidOperationException("No Payroll Area or Emp Code entered."));

        await Should.NotThrowAsync(() => runner.RunAsync(FinanceRunJobArgs.From(Input(), TenantId, 2, id, null)));

        var row = LogRows().Single();
        row.EmployeeId.ShouldBeNull();
        row.Status.ShouldBe(1);
        row.Message.ShouldBe("No Payroll Area or Emp Code entered.");
        Context.ExecutionLogs.Single().ExecutionStatus.ShouldBe(ExecutionLogStatus.FAILED);
    }

    [Fact]
    public async Task Should_Not_Run_The_Engine_Again_When_The_Job_Is_Executed_A_Second_Time()
    {
        SeedArea();
        var (runner, log) = Create();
        var id = await log.OpenAsync(FinanceRunProcess.CostEngine, Input(), "admin");
        await log.TryClaimAsync(id);

        await runner.RunAsync(FinanceRunJobArgs.From(Input(), TenantId, 2, id, null));

        await _engine.DidNotReceive().RunAsync(Arg.Any<CostingRunInput>(), Arg.Any<ICostingRunObserver>());
        Context.ExecutionLogs.Single().ExecutionStatus.ShouldBe(ExecutionLogStatus.FAILED);
    }

    [Fact]
    public async Task Should_Not_Throw_From_The_Background_Job_Even_When_The_Unit_Of_Work_Fails()
    {
        var unitOfWorkManager = Substitute.For<IUnitOfWorkManager>();
        unitOfWorkManager.Begin(Arg.Any<UnitOfWorkOptions>()).Throws(new InvalidOperationException("db down"));

        var job = new CostingFileBackgroundJob(Create().Runner, unitOfWorkManager, Substitute.For<IAbpSession>());

        await Should.NotThrowAsync(() => job.ExecuteAsync(new FinanceRunJobArgs { TenantId = 1, ExecutionLogId = 1 }));
    }
}
