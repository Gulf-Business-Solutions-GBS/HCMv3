using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using HCMv3.ProjectManagement;
using HCMv3.SystemSecurity;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// <see cref="FinanceRunLog"/>: legacy's ProjectLog + ExecutionLog bookkeeping for the two finance screens
/// (<c>uscCostingFile.ascx.cs</c> lines 397-424, 604-690, 2117-2264, 2484-2585).
/// </summary>
// ReSharper disable once InconsistentNaming
public class FinanceRunLog_Tests : FinancePostingTestBase
{
    private async Task<(FinanceRunLog Log, int RunId)> StartedRunAsync(FinanceRunProcess process = FinanceRunProcess.CostEngine)
    {
        SeedArea();
        var log = CreateRunLog();
        var id = await log.OpenAsync(process, Input(), "admin");
        (await log.TryClaimAsync(id)).ShouldBeTrue();
        return (log, id);
    }

    [Fact]
    public async Task Should_Open_The_Run_Record_With_The_Legacy_Parameter_String()
    {
        SeedArea();
        var log = CreateRunLog();

        var id = await log.OpenAsync(FinanceRunProcess.CostEngine, Input(employeeCode: "E1,E2"), "admin");

        var record = Context.ExecutionLogs.Single(e => e.Id == id);
        record.ExecutionStatus.ShouldBe(ExecutionLogStatus.INPRECESS);
        record.Module.ShouldBe(ExecutionLogModule.CostEngine);
        record.UserName.ShouldBe("admin");
        record.Parameters.ShouldBe(
            "Payroll Area : PA | Payroll RunType : Regular | Period : 30/09/2026 | Posting Date : 30/09/2026 | Employee Code : E1,E2");
    }

    [Fact]
    public async Task Should_Let_A_Run_Be_Claimed_Only_Once()
    {
        var (log, id) = await StartedRunAsync();

        (await log.TryClaimAsync(id)).ShouldBeFalse();
    }

    [Theory]
    [InlineData(new[] { 0, 0 }, ExecutionLogStatus.SUCCESS)]
    [InlineData(new[] { 1, 1 }, ExecutionLogStatus.FAILED)]
    [InlineData(new[] { 0, 1 }, ExecutionLogStatus.PARTIAL)]
    [InlineData(new int[0], ExecutionLogStatus.SUCCESS)]
    public async Task Should_Close_The_Run_With_The_Legacy_Status(int[] rowStatuses, ExecutionLogStatus expected)
    {
        var (log, id) = await StartedRunAsync();
        foreach (var status in rowStatuses)
        {
            await log.WriteAsync(id, null, PayInDate, "row", status, rowStatuses.Length);
        }

        await log.CloseAsync(id);

        var record = Context.ExecutionLogs.Single(e => e.Id == id);
        record.ExecutionStatus.ShouldBe(expected);
        record.TotalRecords.ShouldBe(rowStatuses.Length);
        record.Successfull.ShouldBe(rowStatuses.Count(s => s == 0));
    }

    [Fact]
    public async Task Should_Close_As_Aborted_After_A_Stop()
    {
        var (log, id) = await StartedRunAsync();
        await log.WriteAsync(id, null, PayInDate, "row", 0, 2);

        await log.RequestStopAsync();
        await log.CloseAsync(id);

        Context.ExecutionLogs.Single(e => e.Id == id).ExecutionStatus.ShouldBe(ExecutionLogStatus.ABORTED);
    }

    /// <summary>Legacy flags the FIRST existing ProjectLog row and inserts nothing (lines 2216-2223).</summary>
    [Fact]
    public async Task Should_Flag_The_First_Existing_Log_Row_On_Stop_As_Legacy_Did()
    {
        var (log, id) = await StartedRunAsync();
        await log.WriteAsync(id, null, PayInDate, "first", 0, 2);
        await log.WriteAsync(id, null, PayInDate, "second", 0, 2);

        await log.RequestStopAsync();

        var rows = LogRows();
        rows.Count.ShouldBe(2);
        rows[0].StopFlag.ShouldBeTrue();
        rows[1].StopFlag.ShouldBeFalse();
    }

    /// <summary>Legacy's Stop did nothing before the first row existed; the run is now told anyway.</summary>
    [Fact]
    public async Task Should_Honour_A_Stop_Pressed_Before_Any_Log_Row_Exists()
    {
        var (log, id) = await StartedRunAsync();

        await log.RequestStopAsync();

        LogRows().ShouldBeEmpty();
        (await log.IsStopRequestedAsync(id)).ShouldBeTrue();
    }

    /// <summary>
    /// ProjectLog is shared with the Project Cost Engine, which empties it when it starts (as legacy did). The
    /// run's counters and its Stop live on the run's own record and request, so they survive that.
    /// </summary>
    [Fact]
    public async Task Should_Keep_Counts_And_Stop_When_Another_Engine_Empties_The_Shared_Log()
    {
        var (log, id) = await StartedRunAsync();
        await log.WriteAsync(id, null, PayInDate, "ok", 0, 3);
        await log.WriteAsync(id, null, PayInDate, "bad", 1, 3);
        await log.RequestStopAsync();

        // The Project Cost Engine starts and wipes the table.
        Context.ProjectLogs.RemoveRange(Context.ProjectLogs.ToList());
        Context.SaveChanges();

        (await log.IsStopRequestedAsync(id)).ShouldBeTrue();

        var status = await log.GetStatusAsync(FinanceRunProcess.CostEngine);
        status.TotalRecords.ShouldBe(2);
        status.SuccessRecords.ShouldBe(1);
        status.FailedRecords.ShouldBe(1);

        await log.CloseAsync(id);
        Context.ExecutionLogs.Single(e => e.Id == id).ExecutionStatus.ShouldBe(ExecutionLogStatus.ABORTED);
    }

    [Fact]
    public async Task Should_Report_A_Running_Run_And_Refuse_To_Count_A_Silent_One()
    {
        var (log, id) = await StartedRunAsync();

        (await log.GetStatusAsync(FinanceRunProcess.CostEngine)).RunStatus.ShouldBe(FinanceRunStatus.Running);
        (await log.GetActiveRunAsync()).ExecutionLogId.ShouldBe(id);

        // Nothing heard for longer than the limit: the server was restarted mid-run.
        var record = Context.ExecutionLogs.Single(e => e.Id == id);
        record.ActionDateTime = DateTime.Now.AddMinutes(-FinanceRunLog.StaleAfterMinutes - 5);
        record.LastModificationTime = record.ActionDateTime;
        Context.SaveChanges();

        (await log.GetStatusAsync(FinanceRunProcess.CostEngine)).RunStatus.ShouldBe(FinanceRunStatus.Interrupted);
        (await log.GetActiveRunAsync()).ShouldBeNull();
    }

    [Fact]
    public async Task Should_Keep_Each_Screens_Status_To_Its_Own_Runs()
    {
        var (log, _) = await StartedRunAsync(FinanceRunProcess.PostJv);

        (await log.GetStatusAsync(FinanceRunProcess.PostJv)).IsRunning.ShouldBeTrue();
        (await log.GetStatusAsync(FinanceRunProcess.CostEngine)).RunStatus.ShouldBe(FinanceRunStatus.NotStarted);
    }

    [Fact]
    public async Task Should_Show_Completed_With_Errors_Then_Not_Started_After_Clear_Status()
    {
        var (log, id) = await StartedRunAsync();
        await log.WriteAsync(id, null, PayInDate, "bad", 1, 1);
        await log.CloseAsync(id);

        (await log.GetStatusAsync(FinanceRunProcess.CostEngine)).RunStatus.ShouldBe(FinanceRunStatus.CompletedWithErrors);

        await log.ClearAsync();

        LogRows().ShouldBeEmpty();
        var status = await log.GetStatusAsync(FinanceRunProcess.CostEngine);
        status.RunStatus.ShouldBe(FinanceRunStatus.NotStarted);
        status.TotalRecords.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Show_Progress_And_The_Current_Employee_As_Legacy_Did()
    {
        var (log, id) = await StartedRunAsync();
        var e1 = AddEmployee("E0000000001");
        await log.WriteAsync(id, e1, PayInDate, CostingEngineSuccess, 0, 4);

        var status = await log.GetStatusAsync(FinanceRunProcess.CostEngine);

        status.NoOfEmployee.ShouldBe(4);
        status.ProgressPercent.ShouldBe(25);

        // "E0000000001 Emp E0000000001" is 27 characters: within legacy's 30, shown whole.
        status.CurrentEmployee.ShouldBe("E0000000001 Emp E0000000001");
    }

    private const string CostingEngineSuccess = "Successfull";

    /// <summary>Legacy grid: All / Successful / Failed, with the employee's code and name (lines 2943-2962).</summary>
    [Fact]
    public async Task Should_Filter_The_Show_Details_Grid_And_Count_Each_Filter()
    {
        var (log, id) = await StartedRunAsync();
        var e1 = AddEmployee("E1");
        var e2 = AddEmployee("E2");
        await log.WriteAsync(id, e1, PayInDate, "Successfull", 0, 2);
        await log.WriteAsync(id, e2, PayInDate, "WageType : 'Basic' not configured", 1, 2);

        var failed = await log.GetLogAsync(new GetFinanceRunLogInput { StatusFilter = FinanceRunLogFilter.Failed, MaxResultCount = 10 });

        failed.AllCount.ShouldBe(2);
        failed.SuccessCount.ShouldBe(1);
        failed.FailedCount.ShouldBe(1);
        failed.TotalCount.ShouldBe(1);
        failed.Items.Single().EmpCode.ShouldBe("E2");
        failed.Items.Single().Employee.ShouldBe("Emp E2");
        failed.Items.Single().Message.ShouldContain("not configured");
    }

    [Fact]
    public async Task Should_Mark_A_Re_Executed_Run_As_Interrupted()
    {
        var (log, id) = await StartedRunAsync();

        await log.MarkInterruptedAsync(id, PayInDate);

        Context.ExecutionLogs.Single(e => e.Id == id).ExecutionStatus.ShouldBe(ExecutionLogStatus.FAILED);
        var row = LogRows().Single();
        row.Status.ShouldBe(1);
        row.Message.ShouldBe(Text("FinanceRunInterrupted"));
    }
}
