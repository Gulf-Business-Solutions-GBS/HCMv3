using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.EngineRuns;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.ProjectManagement;
using HCMv3.ProjectManagement.Services;
using HCMv3.SystemSecurity;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Services;
using HCMv3.Tests.EngineRuns;
using HCMv3.TimeManagement;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.ProjectManagement;

/// <summary>
/// Unit tests for <see cref="ProjectCostEngineService"/> -- the port of legacy
/// <c>HCM10.Web/UserControls/ProjectManagement/uscProjectCosting.ascx.cs</c>.
///
/// <para>The money-bearing assertions (working-day pro-ration, percentage split) use figures chosen so the
/// expected value is exact in binary floating point, so any change to legacy's arithmetic is caught.</para>
/// </summary>
public class ProjectCostEngineService_Tests
{
    private const int TenantId = 1;
    private static readonly DateTime PeriodEnd = new DateTime(2024, 1, 31);

    #region << Pre-flight validation (legacy Main, lines 219-275) >>

    [Fact]
    public async Task Should_Throw_When_PayInDate_Is_Not_Set()
    {
        var harness = new Harness();
        var service = harness.Build();

        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() =>
            service.RunAsync(new ProjectCostEngineRunInput
            {
                PayrollRunType = PayrollRunType.Regular,
                PayrollAreaName = "PA"
            }));

        ex.Message.ShouldContain("No Pay Date defined");
    }

    [Fact]
    public async Task Should_Throw_When_OffCycle_Run_Has_No_Payment_Date()
    {
        var harness = new Harness();
        var service = harness.Build();

        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() =>
            service.RunAsync(new ProjectCostEngineRunInput
            {
                PayrollRunType = PayrollRunType.OffCycle,
                PayInDate = PeriodEnd,
                PayrollAreaName = "PA"
            }));

        ex.Message.ShouldContain("Offcycle Payment Date");
    }

    [Fact]
    public async Task Should_Throw_When_Payroll_Control_Record_Not_Found()
    {
        var harness = new Harness();
        harness.PayrollControlRecords.Clear();
        var service = harness.Build();

        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() => service.RunAsync(RegularInput()));

        ex.Message.ShouldContain("Payroll Control Record not found");
    }

    [Fact]
    public async Task Should_Throw_When_Payroll_Calendar_Missing_For_Period()
    {
        var harness = new Harness();
        harness.PayrollCalendars.Clear();
        var service = harness.Build();

        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() => service.RunAsync(RegularInput()));

        ex.Message.ShouldContain("Payroll Calender");
    }

    [Fact]
    public async Task Should_Throw_When_Control_Record_Is_Not_Locked()
    {
        var harness = new Harness();
        harness.PayrollControlRecords[0].PeriodLocked = false;
        var service = harness.Build();

        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() => service.RunAsync(RegularInput()));

        ex.Message.ShouldContain("not locked or is not current");
    }

    /// <summary>
    /// Legacy has NO off-cycle arm (lines 232-250) and its screen hid the run type, always sending Regular. An
    /// off-cycle run is REFUSED where legacy would have returned an empty run -- nothing is costed, and the reason
    /// is on the log as the run-level failure row.
    /// </summary>
    [Fact]
    public async Task OffCycle_Run_Should_Be_Refused_And_Cost_Nothing()
    {
        var harness = new Harness();
        var service = harness.Build();

        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() => service.RunAsync(new ProjectCostEngineRunInput
        {
            PayrollRunType = PayrollRunType.OffCycle,
            PayInDate = PeriodEnd,
            PayrollAreaName = "PA",
            OffCyclePaymentDate = PeriodEnd
        }));

        ex.Message.ShouldBe(ProjectCostEngineService.OffCycleNotSupportedMessage);
        harness.ProjectRegisters.ShouldBeEmpty();
        harness.ProjectLogs.Count.ShouldBe(1);
        harness.ProjectLogs[0].EmployeeId.ShouldBeNull();
        harness.ProjectLogs[0].Message.ShouldBe(ProjectCostEngineService.OffCycleNotSupportedMessage);
        harness.Progress.StartedRuns.ShouldBeEmpty();
    }

    #endregion

    #region << ActualDays costing (legacy lines 683-1234) >>

    /// <summary>
    /// Legacy lines 790-806: a basic-pay wage type is pro-rated as
    /// <c>(Amount / Number) * actualWorkingDays</c>. 300/30 * 10 == 100 exactly.
    /// </summary>
    [Fact]
    public async Task Should_Prorate_BasicPay_WageType_By_Actual_Working_Days()
    {
        var harness = new Harness();
        harness.AddTimeRegisterWorkingDays(10);
        var service = harness.Build();

        var result = await service.RunAsync(RegularInput());

        result.EmployeeResults.Count.ShouldBe(1);
        result.EmployeeResults[0].Succeeded.ShouldBeTrue();

        harness.ProjectRegisters.Count.ShouldBe(1);
        harness.ProjectRegisters[0].Period.ShouldBe(PeriodEnd);

        harness.ProjectRegisterDetails.Count.ShouldBe(1);
        var detail = harness.ProjectRegisterDetails[0];
        detail.ProjectId.ShouldBe(Harness.ProjectId);
        detail.WageTypeId.ShouldBe(Harness.BasicWageTypeId);
        detail.Number.ShouldBe(10);
        detail.Amount.ShouldBe(100);
    }

    /// <summary>
    /// Public holidays and rest days count as worked days (legacy lines 794-799): 8 working + 1 holiday +
    /// 1 rest = 10 -> 300/30*10 = 100.
    /// </summary>
    [Fact]
    public async Task Should_Count_PublicHoliday_And_RestDay_As_Worked_Days()
    {
        var harness = new Harness();
        harness.AddTimeRegisterWorkingDays(8);
        harness.TimeRegisters.Add(new TimeRegister { Id = 100, EmployeeId = Harness.EmployeeId, DayDate = new DateTime(2024, 1, 20), PublicHoliday = 1 });
        harness.TimeRegisters.Add(new TimeRegister { Id = 101, EmployeeId = Harness.EmployeeId, DayDate = new DateTime(2024, 1, 21), RestDay = 1 });
        var service = harness.Build();

        await service.RunAsync(RegularInput());

        harness.ProjectRegisterDetails.Count.ShouldBe(1);
        harness.ProjectRegisterDetails[0].Number.ShouldBe(10);
        harness.ProjectRegisterDetails[0].Amount.ShouldBe(100);
    }

    /// <summary>Legacy lines 966-967: nothing is written when both figures are zero.</summary>
    [Fact]
    public async Task Should_Write_No_Detail_When_There_Are_No_Worked_Days()
    {
        var harness = new Harness();
        var service = harness.Build();

        await service.RunAsync(RegularInput());

        harness.ProjectRegisters.Count.ShouldBe(1);
        harness.ProjectRegisterDetails.ShouldBeEmpty();
    }

    /// <summary>
    /// Legacy lines 546-556: re-running a period REUSES the existing register and wipes its detail rows
    /// first, so figures never double up.
    /// </summary>
    [Fact]
    public async Task Should_Reuse_Existing_Register_And_Replace_Its_Details()
    {
        var harness = new Harness();
        harness.AddTimeRegisterWorkingDays(10);

        harness.ProjectRegisters.Add(new ProjectRegister
        {
            Id = 77,
            TenantId = TenantId,
            EmployeeId = Harness.EmployeeId,
            Period = PeriodEnd
        });
        harness.ProjectRegisterDetails.Add(new ProjectRegisterDetail
        {
            Id = 88,
            TenantId = TenantId,
            ProjectRegisterId = 77,
            ProjectId = Harness.ProjectId,
            WageTypeId = Harness.BasicWageTypeId,
            Number = 999,
            Amount = 999
        });

        var service = harness.Build();

        await service.RunAsync(RegularInput());

        harness.ProjectRegisters.Count.ShouldBe(1);
        harness.ProjectRegisters[0].Id.ShouldBe(77);

        harness.ProjectRegisterDetails.Count.ShouldBe(1);
        harness.ProjectRegisterDetails[0].Number.ShouldBe(10);
        harness.ProjectRegisterDetails[0].Amount.ShouldBe(100);
    }

    #endregion

    #region << Overtime costing (legacy lines 727-729, 808-819) -- wave P8 >>

    /// <summary>
    /// Legacy lines 808-818: a wage type reachable from an <c>OvertimeType</c> is costed as
    /// <c>ApprovedOvertimeHours * OvertimeType.OvertimeRate</c> for Number and
    /// <c>that * SalaryRegisterDetail.Rate</c> for Amount -- NOT pro-rated by working days.
    ///
    /// <para>Until wave P8 restored <c>OvertimeType.WageTypeId</c> the overtime set was always empty and this
    /// branch could never be taken, so the wage type fell through to the Earning/Deduction working-day
    /// pro-ration and the costed money differed from legacy. 6 approved hours * rate 1.5 = 9 Number;
    /// 9 * SalaryRegisterDetail.Rate 4 = 36 Amount.</para>
    /// </summary>
    [Fact]
    public async Task Should_Cost_Overtime_WageType_By_Approved_Hours_Times_OvertimeRate()
    {
        var harness = new Harness();
        harness.AddOvertimeWageType(overtimeRate: 1.5, approvedHours: 6, registerRate: 4);
        // Working days are present to prove the overtime branch WINS over day pro-ration.
        harness.AddTimeRegisterWorkingDays(10);

        var service = harness.Build();

        await service.RunAsync(RegularInput());

        var overtimeDetail = harness.ProjectRegisterDetails
            .Single(d => d.WageTypeId == Harness.OvertimeWageTypeId);

        overtimeDetail.Number.ShouldBe(9);
        overtimeDetail.Amount.ShouldBe(36);
    }

    /// <summary>
    /// The overtime branch must only claim wage types actually mapped by an <c>OvertimeType</c>. An
    /// OvertimeType with a NULL WageTypeId contributes nothing (legacy guards with
    /// <c>Where(r =&gt; r.WageType != null)</c>).
    /// </summary>
    [Fact]
    public async Task Should_Ignore_OvertimeType_With_No_WageType()
    {
        var harness = new Harness();
        harness.OvertimeTypes.Add(new OvertimeType
        {
            Id = 90, TenantId = TenantId, Code = "OT", NameEn = "OT", NameAr = "OT",
            OvertimeRate = 2, WageTypeId = null
        });
        harness.AddTimeRegisterWorkingDays(10);

        var service = harness.Build();

        await service.RunAsync(RegularInput());

        // Basic pay still pro-rated normally -- the null-mapped OvertimeType claimed nothing.
        harness.ProjectRegisterDetails.Count.ShouldBe(1);
        harness.ProjectRegisterDetails[0].WageTypeId.ShouldBe(Harness.BasicWageTypeId);
        harness.ProjectRegisterDetails[0].Amount.ShouldBe(100);
    }

    #endregion

    #region << Whole-run failure ProjectLog row (legacy lines 280-293) -- wave P8 >>

    /// <summary>
    /// Legacy's outer catch writes an EMPLOYEE-LESS ProjectLog row for a whole-run failure. This could not
    /// be persisted until wave P8 made <c>ProjectLog.EmployeeId</c> nullable.
    ///
    /// <para>DEVIATION asserted here on purpose: legacy SWALLOWED the exception, this port re-throws after
    /// logging (the background job is the caller's only failure channel).</para>
    /// </summary>
    [Fact]
    public async Task Should_Log_An_EmployeeLess_ProjectLog_Row_When_The_Whole_Run_Fails()
    {
        var harness = new Harness();
        harness.PayrollControlRecords.Clear();
        var service = harness.Build();

        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() => service.RunAsync(RegularInput()));
        ex.Message.ShouldContain("Payroll Control Record not found");

        harness.ProjectLogs.Count.ShouldBe(1);
        harness.ProjectLogs[0].EmployeeId.ShouldBeNull();
        harness.ProjectLogs[0].Status.ShouldBe(1);
        harness.ProjectLogs[0].Message.ShouldContain("Payroll Control Record not found");
    }

    #endregion

    #region << Percentage costing (legacy lines 1237-1272) >>

    /// <summary>
    /// Legacy lines 1264-1265: figures are split by the detail's Percentage, with no day counting at all.
    /// 30 * 40 / 100 == 12, 300 * 40 / 100 == 120.
    /// </summary>
    [Fact]
    public async Task Should_Split_By_Percentage_When_CostingMethod_Is_Percentage()
    {
        var harness = new Harness();
        harness.EmployeeProjects[0].CostingMethod = ProjectCostingMethod.Percentage;
        harness.EmployeeProjectDetails[0].Percentage = 40;
        // Deliberately no TimeRegister rows: the percentage path must not read them.
        var service = harness.Build();

        await service.RunAsync(RegularInput());

        harness.ProjectRegisterDetails.Count.ShouldBe(1);
        harness.ProjectRegisterDetails[0].Number.ShouldBe(12);
        harness.ProjectRegisterDetails[0].Amount.ShouldBe(120);
    }

    /// <summary>Legacy lines 1276-1279: the ActualHours branch is an empty block -- no rows, no error.</summary>
    [Fact]
    public async Task Should_Produce_Nothing_For_ActualHours_CostingMethod()
    {
        var harness = new Harness();
        harness.EmployeeProjects[0].CostingMethod = ProjectCostingMethod.ActualHours;
        harness.AddTimeRegisterWorkingDays(10);
        var service = harness.Build();

        var result = await service.RunAsync(RegularInput());

        result.EmployeeResults[0].Succeeded.ShouldBeTrue();
        harness.ProjectRegisterDetails.ShouldBeEmpty();
    }

    #endregion

    #region << Per-employee isolation and abort (legacy lines 397-400, 598-622) >>

    /// <summary>
    /// Legacy lines 527-531: a missing SalaryRegister fails only THAT employee, and the failure is recorded
    /// as a ProjectLog row with Status 1 rather than aborting the run.
    /// </summary>
    [Fact]
    public async Task Should_Log_Failure_Per_Employee_Instead_Of_Aborting()
    {
        var harness = new Harness();
        harness.SalaryRegisters.Clear();
        var service = harness.Build();

        var result = await service.RunAsync(RegularInput());

        result.Aborted.ShouldBeFalse();
        result.EmployeeResults.Count.ShouldBe(1);
        result.EmployeeResults[0].Succeeded.ShouldBeFalse();
        result.EmployeeResults[0].Message.ShouldContain("Salary Register not found");

        harness.ProjectLogs.Count.ShouldBe(1);
        harness.ProjectLogs[0].Status.ShouldBe(1);
    }

    /// <summary>
    /// Legacy lines 374-379: a ProjectLog row carrying StopFlag aborts the whole loop -- here one raised while the
    /// run is going (after the log was cleared), as a Stop pressed mid-run would.
    /// </summary>
    [Fact]
    public async Task Should_Abort_When_A_ProjectLog_Row_Gets_StopFlag_During_The_Run()
    {
        var harness = new Harness();
        harness.AddTimeRegisterWorkingDays(10);
        harness.Progress = new StopFlagRaisingProgressWriter(harness.ProjectLogs);

        var service = harness.Build();

        var result = await service.RunAsync(RegularInput());

        result.Aborted.ShouldBeTrue();
        harness.ProjectRegisters.ShouldBeEmpty();
        harness.Progress.CompletedStatus.ShouldBe(EngineRunStatus.Aborted);
        harness.ExecutionLogs[0].ExecutionStatus.ShouldBe(ExecutionLogStatus.ABORTED);
    }

    /// <summary>
    /// A StopFlag left over from an EARLIER run does not stop this one: legacy's Run button truncated the log
    /// before starting (lines 1519-1521), so an old flag never survived into a new run.
    /// </summary>
    [Fact]
    public async Task Should_Not_Abort_On_A_StopFlag_Left_From_An_Earlier_Run()
    {
        var harness = new Harness();
        harness.AddTimeRegisterWorkingDays(10);
        harness.ProjectLogs.Add(new ProjectLog
        {
            Id = 1,
            TenantId = TenantId,
            EmployeeId = Harness.EmployeeId,
            StopFlag = true,
            RunTimeStamp = DateTime.Now
        });

        var service = harness.Build();

        var result = await service.RunAsync(RegularInput());

        result.Aborted.ShouldBeFalse();
        result.EmployeeResults.Single().Succeeded.ShouldBeTrue();
        harness.ProjectLogs.ShouldNotContain(l => l.StopFlag);
    }

    /// <summary>The screen's Stop is set on the RUN (its progress row), not on a ProjectLog row.</summary>
    [Fact]
    public async Task Should_Abort_When_Stop_Is_Requested_On_The_Run()
    {
        var harness = new Harness();
        harness.AddTimeRegisterWorkingDays(10);
        harness.Progress.StopRequested = true;
        var service = harness.Build();

        var result = await service.RunAsync(RegularInput());

        result.Aborted.ShouldBeTrue();
        result.EmployeeResults.ShouldBeEmpty();
        harness.ProjectRegisters.ShouldBeEmpty();
        harness.ProjectLogs.ShouldBeEmpty();
        harness.Progress.CompletedStatus.ShouldBe(EngineRunStatus.Aborted);
    }

    /// <summary>
    /// Legacy lines 374-379 come FIRST for each employee: a stopped run does not go on to report the next
    /// employee's missing joining date.
    /// </summary>
    [Fact]
    public async Task Should_Check_The_Stop_Before_The_Employee_Validations()
    {
        var harness = new Harness();
        harness.Employees[0].JoiningDate = default(DateTime);
        harness.Progress.StopRequested = true;
        var service = harness.Build();

        var result = await service.RunAsync(RegularInput());

        result.Aborted.ShouldBeTrue();
        harness.ProjectLogs.ShouldBeEmpty();
    }

    /// <summary>
    /// Legacy lines 416-425: an employee whose contract ended before the period starts is skipped silently
    /// -- no ProjectLog row at all -- unless the run is an End Of Service off-cycle.
    /// </summary>
    [Fact]
    public async Task Should_Skip_Employee_Whose_Contract_Ended_Before_The_Period()
    {
        var harness = new Harness();
        harness.EmployeeContracts[0].EndDate = new DateTime(2023, 6, 30);
        var service = harness.Build();

        var result = await service.RunAsync(RegularInput());

        result.EmployeeResults.ShouldBeEmpty();
        harness.ProjectLogs.ShouldBeEmpty();
        harness.ProjectRegisters.ShouldBeEmpty();
        harness.Progress.SkippedCount.ShouldBe(1);
        harness.Progress.Employees.ShouldBeEmpty();
    }

    #endregion

    #region << Joining date and contract checks (legacy 27 July 2026, lines 381-426) >>

    /// <summary>Legacy lines 382-383: no joining date is an error for the employee, codes typed or not.</summary>
    [Fact]
    public async Task Should_Fail_The_Employee_With_No_Joining_Date()
    {
        var harness = new Harness();
        harness.Employees[0].JoiningDate = default(DateTime);
        var service = harness.Build();

        var result = await service.RunAsync(RegularInput());

        result.EmployeeResults.Single().Succeeded.ShouldBeFalse();
        result.EmployeeResults.Single().Message.ShouldBe("JoiningDate not found.");
        harness.ProjectLogs.Single().Status.ShouldBe(1);
    }

    /// <summary>
    /// Legacy lines 384-390: joined after the period end and NO codes typed -- skipped (no log row), and the
    /// run's employee count (NoOfEmployee) drops by one on the rows written after it.
    /// </summary>
    [Fact]
    public async Task Should_Skip_An_Employee_Who_Joined_After_The_Period_When_No_Codes_Are_Typed()
    {
        var harness = new Harness();
        harness.Employees[0].JoiningDate = new DateTime(2024, 2, 1);
        harness.AddEmployee(2, "E2");
        var service = harness.Build();

        var result = await service.RunAsync(RegularInput());

        result.EmployeeResults.Select(r => r.EmployeeCode).ShouldBe(new[] { "E2" });
        harness.ProjectLogs.Count.ShouldBe(1);
        harness.ProjectLogs[0].EmployeeId.ShouldBe(2);
        harness.ProjectLogs[0].NoOfEmployee.ShouldBe(1);
        harness.Progress.SkippedCount.ShouldBe(1);
        harness.Progress.TotalsPublished.ShouldBe(new[] { 2 });
    }

    /// <summary>Legacy lines 391-392: the same employee asked for by code is an ERROR, with legacy's message.</summary>
    [Fact]
    public async Task Should_Fail_An_Employee_Who_Joined_After_The_Period_When_Asked_For_By_Code()
    {
        var harness = new Harness();
        harness.Employees[0].JoiningDate = new DateTime(2024, 2, 1);
        var service = harness.Build();

        var input = RegularInput();
        input.EmployeeCodes = "E1";
        var result = await service.RunAsync(input);

        result.EmployeeResults.Single().Succeeded.ShouldBeFalse();
        result.EmployeeResults.Single().Message.ShouldBe("JoiningDate : '01/02/2024' > PayInDate : 31/01/2024.");
        harness.ProjectLogs.Single().Status.ShouldBe(1);
        harness.Progress.SkippedCount.ShouldBe(0);
    }

    /// <summary>
    /// Legacy lines 398-400: no contract at all is an error. (The first contract is looked up before the
    /// last, so the message is the first lookup's.)
    /// </summary>
    [Fact]
    public async Task Should_Fail_The_Employee_With_No_Contract()
    {
        var harness = new Harness();
        harness.EmployeeContracts.Clear();
        var service = harness.Build();

        var result = await service.RunAsync(RegularInput());

        result.EmployeeResults.Single().Message.ShouldBe("Employee Contract not found for Employee E1");
    }

    /// <summary>Legacy lines 401-407: the FIRST contract starts after the period end, no codes -- skipped.</summary>
    [Fact]
    public async Task Should_Skip_An_Employee_Whose_First_Contract_Starts_After_The_Period_When_No_Codes_Are_Typed()
    {
        var harness = new Harness();
        harness.EmployeeContracts[0].StartDate = new DateTime(2024, 2, 1);
        var service = harness.Build();

        var result = await service.RunAsync(RegularInput());

        result.EmployeeResults.ShouldBeEmpty();
        harness.ProjectLogs.ShouldBeEmpty();
        harness.Progress.SkippedCount.ShouldBe(1);
    }

    /// <summary>
    /// Legacy line 398 orders by START date: an earlier contract makes the employee eligible even when the
    /// latest one starts after the period.
    /// </summary>
    [Fact]
    public async Task Should_Use_The_Earliest_Contract_For_The_Start_Check()
    {
        var harness = new Harness();
        harness.AddTimeRegisterWorkingDays(10);
        harness.EmployeeContracts[0].EndDate = new DateTime(2024, 12, 31);
        harness.EmployeeContracts.Add(new EmployeeContract
        {
            Id = 2,
            TenantId = TenantId,
            EmployeeId = Harness.EmployeeId,
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2030, 12, 31)
        });
        var service = harness.Build();

        var result = await service.RunAsync(RegularInput());

        result.EmployeeResults.Single().Succeeded.ShouldBeTrue();
    }

    /// <summary>Legacy lines 408-409: the same, asked for by code, is an error with legacy's message.</summary>
    [Fact]
    public async Task Should_Fail_An_Employee_Whose_First_Contract_Starts_After_The_Period_When_Asked_For_By_Code()
    {
        var harness = new Harness();
        harness.EmployeeContracts[0].StartDate = new DateTime(2024, 2, 1);
        var service = harness.Build();

        var input = RegularInput();
        input.EmployeeCodes = "E1";
        var result = await service.RunAsync(input);

        result.EmployeeResults.Single().Message
            .ShouldBe("Contract Started on '01/02/2024'. Can't run Payroll for Date : '31/01/2024'");
    }

    /// <summary>Legacy lines 423-424: a contract that ended before the period, asked for by code, is an error.</summary>
    [Fact]
    public async Task Should_Fail_An_Employee_Whose_Contract_Ended_Before_The_Period_When_Asked_For_By_Code()
    {
        var harness = new Harness();
        harness.EmployeeContracts[0].EndDate = new DateTime(2023, 6, 30);
        var service = harness.Build();

        var input = RegularInput();
        input.EmployeeCodes = " E1 ";
        var result = await service.RunAsync(input);

        result.EmployeeResults.Single().Message
            .ShouldBe("Contract EndDate on '30/06/2023'. Can't run Payroll for Date : '31/01/2024'");
        harness.ProjectRegisters.ShouldBeEmpty();
    }

    /// <summary>
    /// Blank-only codes count as "no codes": the engine then selects by payroll area AND skips quietly, so the
    /// selection and the skip-or-error decision never disagree.
    /// </summary>
    [Fact]
    public async Task Should_Treat_Blank_Employee_Codes_As_No_Codes()
    {
        var harness = new Harness();
        harness.EmployeeContracts[0].EndDate = new DateTime(2023, 6, 30);
        var service = harness.Build();

        var input = RegularInput();
        input.EmployeeCodes = "   ";
        var result = await service.RunAsync(input);

        result.EmployeeResults.ShouldBeEmpty();
        harness.Progress.SkippedCount.ShouldBe(1);
    }

    #endregion

    #region << Clean log, one run at a time, progress, batches >>

    /// <summary>Legacy's Run button truncated ProjectLog before the run (lines 1519, 1706-1710).</summary>
    [Fact]
    public async Task Should_Start_Every_Run_With_An_Empty_Log()
    {
        var harness = new Harness();
        harness.AddTimeRegisterWorkingDays(10);
        harness.ProjectLogs.Add(new ProjectLog { Id = 50, TenantId = TenantId, EmployeeId = 99, Message = "old run", Status = 1 });
        harness.ProjectLogs.Add(new ProjectLog { Id = 51, TenantId = TenantId, Message = "old run-level failure", Status = 1 });
        var service = harness.Build();

        await service.RunAsync(RegularInput());

        harness.ProjectLogs.Count.ShouldBe(1);
        harness.ProjectLogs[0].Message.ShouldBe("Successfull");
        // ...so the execution log counts this run only.
        harness.ExecutionLogs[0].TotalRecords.ShouldBe(1);
        harness.ExecutionLogs[0].ExecutionStatus.ShouldBe(ExecutionLogStatus.SUCCESS);
    }

    /// <summary>The log is cleared in its own unit of work, committed before the run goes on.</summary>
    [Fact]
    public async Task Should_Clear_The_Log_In_Its_Own_Unit_Of_Work()
    {
        var harness = new Harness();
        harness.AddTimeRegisterWorkingDays(10);
        var service = harness.Build();

        await service.RunAsync(RegularInput());

        // One for clearing the log; the run itself is not batched here (CommitBatchSize 0).
        harness.UnitOfWorkManager.Received(1).Begin(TransactionScopeOption.RequiresNew);
    }

    /// <summary>
    /// ONE RUN AT A TIME. A refusal neither clears the running run's log nor adds a failed row to it (that row
    /// would be counted as a failed employee of the running run).
    /// </summary>
    [Fact]
    public async Task Should_Refuse_A_Second_Run_Without_Touching_The_Running_Runs_Log()
    {
        var harness = new Harness();
        harness.Progress.ActiveRunKey = ProjectCostEngineService.RunKey;
        harness.Progress.StartedByUserName = "admin";
        harness.ProjectLogs.Add(new ProjectLog { Id = 7, TenantId = TenantId, EmployeeId = 99, Message = "Successfull", Status = 0 });
        var service = harness.Build();

        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() => service.RunAsync(RegularInput()));

        ex.Message.ShouldStartWith("The Project Cost Engine is already running (started by admin at ");
        harness.ProjectLogs.Count.ShouldBe(1);
        harness.ProjectLogs[0].Id.ShouldBe(7);
        harness.ExecutionLogs.ShouldBeEmpty();
        harness.Progress.StartedRuns.ShouldBeEmpty();
    }

    /// <summary>The shared progress contract: begin, total, one row per employee with its duration, closed.</summary>
    [Fact]
    public async Task Should_Publish_Progress_Through_The_Shared_Engine_Run_Writer()
    {
        var harness = new Harness();
        harness.AddTimeRegisterWorkingDays(10);
        harness.AddEmployee(2, "E2");
        harness.SalaryRegisters.RemoveAll(r => r.EmployeeId == 2);
        var service = harness.Build();

        var input = RegularInput();
        input.CurrentUserName = "payroll.officer";
        await service.RunAsync(input);

        var started = harness.Progress.StartedRuns.Single();
        started.EngineType.ShouldBe(EngineType.ProjectCostEngine);
        started.RunKey.ShouldBe(ProjectCostEngineService.RunKey);
        started.StartedByUserName.ShouldBe("payroll.officer");
        started.Parameters.ShouldContain("Payroll Area : PA");
        started.Parameters.ShouldContain("Period : 31/01/2024");

        harness.Progress.TotalsPublished.ShouldBe(new[] { 2 });
        harness.Progress.Employees.Select(e => e.EmployeeCode).ShouldBe(new[] { "E1", "E2" });
        harness.Progress.Employees.Select(e => e.SequenceNo).ShouldBe(new[] { 1, 2 });
        harness.Progress.Employees[0].Succeeded.ShouldBeTrue();
        harness.Progress.Employees[0].Message.ShouldBe("Successfull");
        harness.Progress.Employees[1].Succeeded.ShouldBeFalse();
        harness.Progress.Employees[1].Message.ShouldContain("Salary Register not found");
        harness.Progress.Employees.ShouldAllBe(e => e.EngineRunId == RecordingEngineRunProgressWriter.TestRunId);

        harness.Progress.Completed.ShouldBeTrue();
        harness.Progress.CompletedStatus.ShouldBe(EngineRunStatus.CompletedWithErrors);
        harness.Progress.CompletedExecutionLogId.ShouldBe(harness.ExecutionLogs[0].Id);
        harness.Progress.CompletedFailureMessage.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Close_A_Clean_Run_As_Completed()
    {
        var harness = new Harness();
        harness.AddTimeRegisterWorkingDays(10);
        var service = harness.Build();

        await service.RunAsync(RegularInput());

        harness.Progress.CompletedStatus.ShouldBe(EngineRunStatus.Completed);
    }

    /// <summary>
    /// SAVE IN BATCHES: each batch in its own RequiresNew unit of work. Two employees in batches of one = two
    /// batch units of work, plus the one that cleared the log. The figures are the same as unbatched.
    /// </summary>
    [Fact]
    public async Task Should_Save_Each_Batch_In_Its_Own_Unit_Of_Work()
    {
        var harness = new Harness();
        harness.AddTimeRegisterWorkingDays(10);
        harness.AddEmployee(2, "E2");
        var service = harness.Build();

        var input = RegularInput();
        input.CommitBatchSize = 1;
        var result = await service.RunAsync(input);

        result.EmployeeResults.ShouldAllBe(r => r.Succeeded);
        result.EmployeeResults.Count.ShouldBe(2);
        harness.UnitOfWorkManager.Received(3).Begin(TransactionScopeOption.RequiresNew);
        harness.ProjectRegisterDetails.Single(d => harness.ProjectRegisters.Single(r => r.EmployeeId == Harness.EmployeeId).Id == d.ProjectRegisterId)
            .Amount.ShouldBe(100);
        harness.Progress.CompletedStatus.ShouldBe(EngineRunStatus.Completed);
    }

    /// <summary>A stop between batches ends the run: the next batch is not opened.</summary>
    [Fact]
    public async Task Should_Not_Open_Another_Batch_Once_Stopped()
    {
        var harness = new Harness();
        harness.AddTimeRegisterWorkingDays(10);
        harness.AddEmployee(2, "E2");
        harness.Progress.StopRequested = true;
        var service = harness.Build();

        var input = RegularInput();
        input.CommitBatchSize = 1;
        var result = await service.RunAsync(input);

        result.Aborted.ShouldBeTrue();
        // The log clear + the first batch, which found the stop.
        harness.UnitOfWorkManager.Received(2).Begin(TransactionScopeOption.RequiresNew);
    }

    /// <summary>
    /// A run-level failure AFTER the run began: the progress row is closed as Failed with the reason, the failure
    /// is marked as recorded (so the job writes no second row), and the legacy failure row is on the log, written
    /// in its own unit of work so it is not rolled back with the run.
    /// </summary>
    [Fact]
    public async Task Should_Record_A_Run_Level_Failure_On_The_Run_And_On_The_Log()
    {
        var harness = new Harness();
        harness.EmployeeRepositoryFailure = new InvalidOperationException("the database went away");
        var service = harness.Build();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.RunAsync(RegularInput()));

        ex.Data.Contains(ProjectCostEngineService.RunFailureRecordedKey).ShouldBeTrue();
        harness.Progress.CompletedStatus.ShouldBe(EngineRunStatus.Failed);
        harness.Progress.CompletedFailureMessage.ShouldBe("the database went away");
        harness.ProjectLogs.Single().EmployeeId.ShouldBeNull();
        harness.ProjectLogs.Single().Message.ShouldBe("the database went away");
        // The log clear + the failure row.
        harness.UnitOfWorkManager.Received(2).Begin(TransactionScopeOption.RequiresNew);
    }

    /// <summary>
    /// LEGACY QUIRK, pinned rather than fixed: CreateProjectRegister's finally finalises the execution log
    /// (lines 621-653) BEFORE Main's catch writes the failure row (lines 266-282). So a run that dies on
    /// employee selection is logged as SUCCESS with 0 records -- legacy's own result. The run's progress row
    /// is the one that says Failed.
    /// </summary>
    [Fact]
    public async Task Should_Finalise_The_Execution_Log_Before_The_Failure_Row_As_Legacy()
    {
        var harness = new Harness();
        harness.EmployeeRepositoryFailure = new InvalidOperationException("the database went away");
        var service = harness.Build();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RunAsync(RegularInput()));

        var log = harness.ExecutionLogs.Single();
        log.ExecutionStatus.ShouldBe(ExecutionLogStatus.SUCCESS);
        log.TotalRecords.ShouldBe(0);
        harness.Progress.CompletedStatus.ShouldBe(EngineRunStatus.Failed);
    }

    /// <summary>A refusal BEFORE the run began is not marked as recorded: the job opens a Failed row for it.</summary>
    [Fact]
    public async Task Should_Not_Mark_A_Preflight_Refusal_As_Recorded()
    {
        var harness = new Harness();
        harness.PayrollCalendars.Clear();
        var service = harness.Build();

        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() => service.RunAsync(RegularInput()));

        ex.Data.Contains(ProjectCostEngineService.RunFailureRecordedKey).ShouldBeFalse();
        harness.Progress.StartedRuns.ShouldBeEmpty();
    }

    #endregion

    #region << Execution log (legacy lines 314-346, 638-670) >>

    [Fact]
    public async Task Should_Write_An_ExecutionLog_Row_For_The_Run()
    {
        var harness = new Harness();
        harness.AddTimeRegisterWorkingDays(10);
        var service = harness.Build();

        await service.RunAsync(RegularInput());

        harness.ExecutionLogs.Count.ShouldBe(1);
        var log = harness.ExecutionLogs[0];
        log.Module.ShouldBe(ExecutionLogModule.ProjectEngine);
        log.ScreenName.ShouldBe("HCM10.Web.UserControls.ProjectManagement.uscProjectCosting");
        log.Parameters.ShouldContain("Payroll Area : PA");
        log.Parameters.ShouldContain("Period : 31/01/2024");
        log.ExecutionStatus.ShouldBe(ExecutionLogStatus.SUCCESS);
        log.TotalRecords.ShouldBe(1);
        log.Successfull.ShouldBe(1);
    }

    #endregion

    private static ProjectCostEngineRunInput RegularInput()
    {
        return new ProjectCostEngineRunInput
        {
            PayrollRunType = PayrollRunType.Regular,
            PayInDate = PeriodEnd,
            PayrollAreaName = "PA"
        };
    }

    /// <summary>
    /// Minimal but complete data set for one employee on one project for January 2024, with every guard in
    /// <c>ProcessEmployeeAsync</c> satisfied. Individual tests mutate the lists before calling
    /// <see cref="Build"/>.
    /// </summary>
    private sealed class Harness
    {
        public const int EmployeeId = 1;
        public const int ProjectId = 5;
        public const int BasicWageTypeId = 10;

        public readonly List<PayrollArea> PayrollAreas = new List<PayrollArea>();
        public readonly List<PayrollControlRecord> PayrollControlRecords = new List<PayrollControlRecord>();
        public readonly List<PayrollCalendar> PayrollCalendars = new List<PayrollCalendar>();
        public readonly List<Employee> Employees = new List<Employee>();
        public readonly List<EmployeeContract> EmployeeContracts = new List<EmployeeContract>();
        public readonly List<EmployeeOrgAssignment> EmployeeOrgAssignments = new List<EmployeeOrgAssignment>();
        public readonly List<EmployeePayrollStatus> EmployeePayrollStatuses = new List<EmployeePayrollStatus>();
        public readonly List<EmployeeAbsence> EmployeeAbsences = new List<EmployeeAbsence>();
        public readonly List<EmployeeOvertime> EmployeeOvertimes = new List<EmployeeOvertime>();

        /// <summary>Wave P8: OvertimeType now carries the legacy WageType FK, so the real resolver is used.</summary>
        public readonly List<OvertimeType> OvertimeTypes = new List<OvertimeType>();
        public readonly List<SalaryRegister> SalaryRegisters = new List<SalaryRegister>();
        public readonly List<SalaryRegisterDetail> SalaryRegisterDetails = new List<SalaryRegisterDetail>();
        public readonly List<WageType> WageTypes = new List<WageType>();
        public readonly List<AbsenceType> AbsenceTypes = new List<AbsenceType>();
        public readonly List<TicketType> TicketTypes = new List<TicketType>();
        public readonly List<TimeRegister> TimeRegisters = new List<TimeRegister>();
        public readonly List<ExecutionLog> ExecutionLogs = new List<ExecutionLog>();
        public readonly List<ProjectLog> ProjectLogs = new List<ProjectLog>();
        public readonly List<ProjectRegister> ProjectRegisters = new List<ProjectRegister>();
        public readonly List<ProjectRegisterDetail> ProjectRegisterDetails = new List<ProjectRegisterDetail>();
        public readonly List<EmployeeProject> EmployeeProjects = new List<EmployeeProject>();
        public readonly List<EmployeeProjectDetail> EmployeeProjectDetails = new List<EmployeeProjectDetail>();
        public readonly List<ProjectWageType> ProjectWageTypes = new List<ProjectWageType>();

        public Harness()
        {
            var payrollArea = new PayrollArea { Id = 1, TenantId = TenantId, Code = "PA", Name = "PA" };
            PayrollAreas.Add(payrollArea);

            PayrollControlRecords.Add(new PayrollControlRecord
            {
                Id = 1,
                TenantId = TenantId,
                PayrollAreaId = payrollArea.Id,
                PayrollArea = payrollArea,
                PayrollMonth = 1,
                PayrollYear = 2024,
                CalendarMonthStart = new DateTime(2024, 1, 1),
                CalendarMonthEnd = PeriodEnd,
                PeriodLocked = true
            });

            PayrollCalendars.Add(new PayrollCalendar { Id = 1, TenantId = TenantId, PayrollMonth = 1, PayrollYear = 2024 });

            Employees.Add(new Employee
            {
                Id = EmployeeId,
                TenantId = TenantId,
                Code = "E1",
                FirstNameEn = "E",
                LastNameEn = "One",
                FullNameEn = "E One",
                // Legacy 27 July 2026 (lines 382-383): an employee with no joining date fails.
                JoiningDate = new DateTime(2020, 1, 1)
            });

            EmployeeContracts.Add(new EmployeeContract
            {
                Id = 1,
                TenantId = TenantId,
                EmployeeId = EmployeeId,
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2030, 12, 31)
            });

            EmployeeOrgAssignments.Add(new EmployeeOrgAssignment
            {
                Id = 1,
                TenantId = TenantId,
                EmployeeId = EmployeeId,
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2030, 12, 31),
                PayrollAreaId = payrollArea.Id,
                PayrollArea = payrollArea
            });

            EmployeePayrollStatuses.Add(new EmployeePayrollStatus
            {
                Id = 1,
                TenantId = TenantId,
                EmployeeId = EmployeeId,
                DoNoAccountAfter = new DateTime(1, 1, 1),
                NextTimeEvaluationFrom = new DateTime(1, 1, 1)
            });

            SalaryRegisters.Add(new SalaryRegister
            {
                Id = 1,
                TenantId = TenantId,
                EmployeeId = EmployeeId,
                ForPeriod = PeriodEnd,
                InPeriod = PeriodEnd,
                PayrollRunType = PayrollRunType.Regular
            });

            WageTypes.Add(new WageType
            {
                Id = BasicWageTypeId,
                TenantId = TenantId,
                Code = "1000",
                NameEn = "Basic",
                NameAr = "Basic",
                BillingType = BillingType.Earning,
                IncludeInBasicPay = true,
                PayrollOrder = 1
            });

            SalaryRegisterDetails.Add(new SalaryRegisterDetail
            {
                Id = 1,
                TenantId = TenantId,
                SalaryRegisterId = 1,
                WageTypeId = BasicWageTypeId,
                Number = 30,
                Amount = 300,
                Rate = 10
            });

            EmployeeProjects.Add(new EmployeeProject
            {
                Id = 1,
                TenantId = TenantId,
                EmployeeId = EmployeeId,
                StartDate = new DateTime(2024, 1, 1),
                EndDate = PeriodEnd,
                CostingMethod = ProjectCostingMethod.ActualDays
            });

            EmployeeProjectDetails.Add(new EmployeeProjectDetail
            {
                Id = 1,
                TenantId = TenantId,
                EmployeeProjectId = 1,
                ProjectId = ProjectId,
                StartDate = new DateTime(2024, 1, 1),
                EndDate = PeriodEnd,
                Percentage = 100
            });

            ProjectWageTypes.Add(new ProjectWageType
            {
                Id = 1,
                TenantId = TenantId,
                ProjectId = ProjectId,
                WageTypeId = BasicWageTypeId
            });
        }

        public const int OvertimeWageTypeId = 20;

        /// <summary>
        /// Adds an overtime wage type to the project, its register line, and the OvertimeType that maps it
        /// (via the wave-P8 <c>WageTypeId</c> FK) plus one EmployeeOvertime row inside the project window.
        /// </summary>
        public void AddOvertimeWageType(double overtimeRate, double approvedHours, double registerRate)
        {
            WageTypes.Add(new WageType
            {
                Id = OvertimeWageTypeId,
                TenantId = TenantId,
                Code = "1500",
                NameEn = "Overtime",
                NameAr = "Overtime",
                BillingType = BillingType.Earning,
                PayrollOrder = 2
            });

            SalaryRegisterDetails.Add(new SalaryRegisterDetail
            {
                Id = 2,
                TenantId = TenantId,
                SalaryRegisterId = 1,
                WageTypeId = OvertimeWageTypeId,
                Number = 5,
                Amount = 50,
                Rate = registerRate
            });

            ProjectWageTypes.Add(new ProjectWageType
            {
                Id = 2,
                TenantId = TenantId,
                ProjectId = ProjectId,
                WageTypeId = OvertimeWageTypeId
            });

            OvertimeTypes.Add(new OvertimeType
            {
                Id = 91,
                TenantId = TenantId,
                Code = "OT1",
                NameEn = "OT1",
                NameAr = "OT1",
                OvertimeRate = overtimeRate,
                WageTypeId = OvertimeWageTypeId
            });

            EmployeeOvertimes.Add(new EmployeeOvertime
            {
                Id = 1,
                TenantId = TenantId,
                EmployeeId = EmployeeId,
                EffectiveDate = new DateTime(2024, 1, 15),
                OvertimeTypeId = 91,
                ApprovedOvertimeHours = approvedHours
            });
        }

        /// <summary>Records what the engine publishes; replace it before <see cref="Build"/> to change behaviour.</summary>
        public RecordingEngineRunProgressWriter Progress = new RecordingEngineRunProgressWriter();

        /// <summary>The unit-of-work manager the built service uses, for asserting RequiresNew units of work.</summary>
        public IUnitOfWorkManager UnitOfWorkManager { get; private set; }

        /// <summary>When set, the employee repository throws this -- a run-level failure on employee selection.</summary>
        public Exception EmployeeRepositoryFailure;

        /// <summary>
        /// A second employee on the same project with every guard satisfied, a salary register for the period and no
        /// register lines (so it succeeds with nothing to cost).
        /// </summary>
        public void AddEmployee(int id, string code)
        {
            var payrollArea = PayrollAreas[0];

            Employees.Add(new Employee
            {
                Id = id,
                TenantId = TenantId,
                Code = code,
                FullNameEn = code + " Name",
                JoiningDate = new DateTime(2020, 1, 1)
            });

            EmployeeContracts.Add(new EmployeeContract
            {
                Id = 100 + id,
                TenantId = TenantId,
                EmployeeId = id,
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2030, 12, 31)
            });

            EmployeeOrgAssignments.Add(new EmployeeOrgAssignment
            {
                Id = 100 + id,
                TenantId = TenantId,
                EmployeeId = id,
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2030, 12, 31),
                PayrollAreaId = payrollArea.Id,
                PayrollArea = payrollArea
            });

            EmployeePayrollStatuses.Add(new EmployeePayrollStatus
            {
                Id = 100 + id,
                TenantId = TenantId,
                EmployeeId = id,
                DoNoAccountAfter = new DateTime(1, 1, 1),
                NextTimeEvaluationFrom = new DateTime(1, 1, 1)
            });

            SalaryRegisters.Add(new SalaryRegister
            {
                Id = 100 + id,
                TenantId = TenantId,
                EmployeeId = id,
                ForPeriod = PeriodEnd,
                InPeriod = PeriodEnd,
                PayrollRunType = PayrollRunType.Regular
            });

            EmployeeProjects.Add(new EmployeeProject
            {
                Id = 100 + id,
                TenantId = TenantId,
                EmployeeId = id,
                StartDate = new DateTime(2024, 1, 1),
                EndDate = PeriodEnd,
                CostingMethod = ProjectCostingMethod.ActualDays
            });

            EmployeeProjectDetails.Add(new EmployeeProjectDetail
            {
                Id = 100 + id,
                TenantId = TenantId,
                EmployeeProjectId = 100 + id,
                ProjectId = ProjectId,
                StartDate = new DateTime(2024, 1, 1),
                EndDate = PeriodEnd,
                Percentage = 100
            });
        }

        public void AddTimeRegisterWorkingDays(int days)
        {
            for (var i = 0; i < days; i++)
            {
                TimeRegisters.Add(new TimeRegister
                {
                    Id = TimeRegisters.Count + 1,
                    TenantId = TenantId,
                    EmployeeId = EmployeeId,
                    DayDate = new DateTime(2024, 1, 1).AddDays(i),
                    ActualWorkingDay = 1
                });
            }
        }

        public ProjectCostEngineService Build()
        {
            var sysSettings = Substitute.For<ISysSettingsAccessor>();
            sysSettings.GetSecuritySettingsAsync().Returns(Task.FromResult<ManageSecuritySettings>(null));
            sysSettings.GetProjectCostingSettingsAsync().Returns(Task.FromResult<ManageProjectCostingSettings>(null));
            sysSettings.GetTimeSettingsAsync().Returns(Task.FromResult<ManageTimeSettings>(null));

            var rounding = Substitute.For<IPayrollRoundingService>();
            rounding.GetCurrencyDecimalRoundingAsync(Arg.Any<int>()).Returns(Task.FromResult(3));

            var employeeRepository = ProjectCostEngineFakes.RepoOver(Employees);
            if (EmployeeRepositoryFailure != null)
            {
                employeeRepository = Substitute.For<IRepository<Employee>>();
                employeeRepository.GetAll().Throws(EmployeeRepositoryFailure);
            }

            var service = new ProjectCostEngineService(
                employeeRepository,
                ProjectCostEngineFakes.RepoOver(EmployeeContracts),
                ProjectCostEngineFakes.RepoOver(EmployeeOrgAssignments),
                ProjectCostEngineFakes.RepoOver(EmployeePayrollStatuses),
                ProjectCostEngineFakes.RepoOver(EmployeeAbsences),
                ProjectCostEngineFakes.RepoOver(EmployeeOvertimes),
                ProjectCostEngineFakes.RepoOver(SalaryRegisters),
                ProjectCostEngineFakes.RepoOver(SalaryRegisterDetails),
                ProjectCostEngineFakes.RepoOver(PayrollControlRecords),
                ProjectCostEngineFakes.RepoOver(PayrollCalendars),
                ProjectCostEngineFakes.RepoOver(WageTypes),
                ProjectCostEngineFakes.RepoOver(AbsenceTypes),
                ProjectCostEngineFakes.RepoOver(TicketTypes),
                ProjectCostEngineFakes.RepoOver(TimeRegisters),
                ProjectCostEngineFakes.RepoOver(ExecutionLogs),
                ProjectCostEngineFakes.RepoOver(ProjectLogs),
                ProjectCostEngineFakes.RepoOver(ProjectRegisters),
                ProjectCostEngineFakes.RepoOver(ProjectRegisterDetails),
                ProjectCostEngineFakes.RepoOver(EmployeeProjects),
                ProjectCostEngineFakes.RepoOver(EmployeeProjectDetails),
                ProjectCostEngineFakes.RepoOver(ProjectWageTypes),
                sysSettings,
                rounding,
                new ProjectCostOvertimeWageTypeResolver(ProjectCostEngineFakes.RepoOver(OvertimeTypes)));

            UnitOfWorkManager = ProjectCostEngineFakes.UnitOfWorkManager();
            service.UnitOfWorkManager = UnitOfWorkManager;
            service.ProgressWriter = Progress;

            return service;
        }
    }

    /// <summary>
    /// Raises a legacy <c>ProjectLog.StopFlag</c> the moment the run opens its progress row -- i.e. after the log was
    /// cleared and before the first employee, as a Stop pressed mid-run would land.
    /// </summary>
    private sealed class StopFlagRaisingProgressWriter : RecordingEngineRunProgressWriter
    {
        private readonly List<ProjectLog> _projectLogs;

        public StopFlagRaisingProgressWriter(List<ProjectLog> projectLogs)
        {
            _projectLogs = projectLogs;
        }

        protected override void OnBegin()
        {
            _projectLogs.Add(new ProjectLog { Id = 900, TenantId = TenantId, StopFlag = true, RunTimeStamp = DateTime.Now });
        }
    }
}
