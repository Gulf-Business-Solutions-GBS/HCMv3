using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Threading;
using HCMv3.Ess.Dto;
using HCMv3.GoLiveSetup;
using HCMv3.GoLiveSetup.Dto;
using HCMv3.GoLiveSetup.ReferenceData;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// END TO END: leave raised and decided through ESS, then a REAL payroll run for the month -- the seeded payroll
/// catalogue, the real Time Engine and the real Payroll Engine, as the Go-Live "Run Payroll" step drives them.
///
/// <para>
/// Every other leave test stops at the leave row, and every payroll test inserts its leave directly with no
/// workflow status. Neither proves the thing that matters: that what an employee asks for, and what an approver
/// decides, is exactly what gets paid. In particular that PENDING leave (hidden by the <c>ApprovedOnly</c>
/// filter) never reaches payroll, and that each of the three returns and a cancellation land in the register.
/// </para>
///
/// <para>
/// <b>The month.</b> September 2026, paid 30/09. Employee E001, basic 500.000 KWD on the seeded fixed-26-day
/// billing, Friday weekly-off. Leave is the seeded Annual Paid Leave (code "1": counts weekends, quota-accrued,
/// return date required, leave pay 1003); unpaid late days go to the seeded Unpaid Leave (code "2").
/// Counters read back: 4000 Annual Leave Paid Days, 4001 Absent Unpaid Days.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class EssLeavePayroll_Tests : EssLeaveWorkflowTestBase
{
    private const string Area = "Default Payroll Area";
    private const string EmployeeCode = "E001";
    private static readonly DateTime PayDate = new(2026, 9, 30);

    private readonly IGoLiveSetupAppService _goLive;
    private readonly ITestOutputHelper _output;

    public EssLeavePayroll_Tests(ITestOutputHelper output)
    {
        _output = output;
        _goLive = Resolve<IGoLiveSetupAppService>();
    }

    // ------------------------------------------------------------------------------------------------ scenarios

    [Fact]
    public async Task Pending_Leave_Should_Not_Reach_Payroll()
    {
        var (meId, _) = await GivenAnEssEmployeeOnPayrollAsync();

        await _myLeaveAppService.ApplyForLeave(new ApplyForLeaveInput
        {
            AbsenceTypeId = AnnualLeaveTypeId(),
            StartDate = new DateTime(2026, 9, 13),
            EndDate = new DateTime(2026, 9, 17)
        });

        await RunPayrollAsync();

        (await NumberOf(meId, "4000")).ShouldBe(0);
        (await AmountOf(meId, "1003")).ShouldBe(0);
    }

    [Fact]
    public async Task Approved_Leave_Should_Be_Paid_As_Leave_Days()
    {
        var (meId, approverId) = await GivenAnEssEmployeeOnPayrollAsync();

        var leaveId = await ApplyAndApproveAsync(new DateTime(2026, 9, 13), new DateTime(2026, 9, 17), approverId);
        await ReturnAndApproveAsync(leaveId, new DateTime(2026, 9, 18), approverId);

        await RunPayrollAsync();

        (await NumberOf(meId, "4000")).ShouldBe(5);
        (await AmountOf(meId, "1003")).ShouldBeGreaterThan(0);

        // Leave is paid time, not lost time: the month is still worth one month's pay.
        (await EarningsOf(meId)).ShouldBe(500, tolerance: 0.001);
    }

    [Fact]
    public async Task A_Leave_Not_Returned_From_Should_Stop_The_Employees_Payroll_Until_The_Return_Is_Approved()
    {
        // Legacy "ReturnDate not found for LeaveType" (PayrollEngine Program.cs 1119-1131), with
        // EnableNotReturnFromLeaveLogic on. The ESS return is what clears it.
        var (meId, approverId) = await GivenAnEssEmployeeOnPayrollAsync();
        EnableNotReturnFromLeaveLogic();

        var leaveId = await ApplyAndApproveAsync(new DateTime(2026, 9, 13), new DateTime(2026, 9, 17), approverId);

        UsingDbContext(context =>
        {
            var row = context.Set<EmployeeAbsence>().Include(a => a.AbsenceType).Single(a => a.Id == leaveId);
            _output.WriteLine($"leave: {row.StartDate:d}-{row.EndDate:d} status={row.WorkflowStatus} returnDate={row.ReturnDate:o} isReturned={row.IsReturned} hours={row.AbsenceHours} returnRequired={row.AbsenceType.IsReturnDateRequired}");
            foreach (var s in context.Set<ManagePayrollSettings>().ToList())
            {
                _output.WriteLine($"payroll settings #{s.Id}: notReturn={s.EnableNotReturnFromLeaveLogic} cutoff={s.PayLeaveAsPerCutoff} generate={s.GenerateRegisterOnNotReturnFromLeave}");
            }
        });

        var blocked = await _goLive.RunPayroll(new GoLiveRunPayrollInput { PayDate = PayDate, PayrollAreaName = Area });
        Dump(blocked);
        var mine = blocked.Employees.Single(e => e.EmployeeCode == EmployeeCode);
        mine.Succeeded.ShouldBeFalse();
        mine.Message.ShouldContain("ReturnDate not found");
        mine.Message.ShouldContain("Annual Paid Leave");

        // The run leaves the month locked (released for payroll), and a locked month freezes leave: approving
        // the return now is refused. HR releases it for correction first, as in the real process.
        var returned = await _myLeaveAppService.ReturnFromMyLeave(new ReturnFromLeaveInput
        {
            EmployeeAbsenceId = leaveId,
            RejoiningDate = new DateTime(2026, 9, 18)
        });

        // Transactional, as every app service call is in production (the harness itself is not): the refused
        // approval must roll back whole, leaving the leave unreturned and the request still waiting.
        await Should.ThrowAsync<Abp.UI.UserFriendlyException>(() =>
            InTransactionAsync(() => _approvalService.ApproveAsync(returned.WorkflowRequestId, approverId, "ok")));
        LeaveRow(leaveId).IsReturned.ShouldBeFalse();

        ReleaseMonthForCorrection();
        await _approvalService.ApproveAsync(returned.WorkflowRequestId, approverId, "ok");
        LeaveRow(leaveId).IsReturned.ShouldBeTrue();

        await RunPayrollAsync();
        (await NumberOf(meId, "4000")).ShouldBe(5);
    }

    [Fact]
    public async Task Late_Return_Within_The_Balance_Should_Pay_The_Late_Days_As_Annual_Leave()
    {
        var (meId, approverId) = await GivenAnEssEmployeeOnPayrollAsync();

        // 13-17 Sep, back Tuesday 22nd: 18-21 late (annual leave counts the Friday).
        var leaveId = await ApplyAndApproveAsync(new DateTime(2026, 9, 13), new DateTime(2026, 9, 17), approverId);
        var result = await ReturnAndApproveAsync(leaveId, new DateTime(2026, 9, 22), approverId);
        result.Kind.ShouldBe(LeaveReturnKind.Late);

        await RunPayrollAsync();

        (await NumberOf(meId, "4000")).ShouldBe(9);
        (await NumberOf(meId, "4001")).ShouldBe(0);
        (await EarningsOf(meId)).ShouldBe(500, tolerance: 0.001);
    }

    [Fact]
    public async Task Late_Return_Beyond_The_Balance_Should_Deduct_The_Unpaid_Days()
    {
        // Back on the 22nd: 18-19 taken as paid annual leave, 20-21 as unpaid -- the split legacy proposes when only
        // two days of balance are left, entered explicitly here so the scenario does not depend on accrual.
        var (meId, approverId) = await GivenAnEssEmployeeOnPayrollAsync();

        var leaveId = await ApplyAndApproveAsync(new DateTime(2026, 9, 13), new DateTime(2026, 9, 17), approverId);

        var result = await _myLeaveAppService.ReturnFromMyLeave(new ReturnFromLeaveInput
        {
            EmployeeAbsenceId = leaveId,
            RejoiningDate = new DateTime(2026, 9, 22),
            PaidStartDate = new DateTime(2026, 9, 18),
            PaidEndDate = new DateTime(2026, 9, 19),
            UnpaidStartDate = new DateTime(2026, 9, 20),
            UnpaidEndDate = new DateTime(2026, 9, 21)
        });
        await _approvalService.ApproveAsync(result.WorkflowRequestId, approverId, "ok");

        result.PaidCalendarDays.ShouldBe(2);
        result.UnpaidCalendarDays.ShouldBe(2);

        await RunPayrollAsync();

        (await NumberOf(meId, "4000")).ShouldBe(7);
        (await NumberOf(meId, "4001")).ShouldBe(2);

        // Two unpaid days on fixed-26 billing: 500 - 2 x 500/26.
        (await EarningsOf(meId)).ShouldBe(500 - 2 * 500.0 / 26, tolerance: 0.001);
    }

    [Fact]
    public async Task Early_Return_Should_Pay_Only_The_Leave_Days_Actually_Taken()
    {
        var (meId, approverId) = await GivenAnEssEmployeeOnPayrollAsync();

        // 13-24 Sep granted, back on the 18th: leave becomes 13-17.
        var leaveId = await ApplyAndApproveAsync(new DateTime(2026, 9, 13), new DateTime(2026, 9, 24), approverId);
        var result = await ReturnAndApproveAsync(leaveId, new DateTime(2026, 9, 18), approverId);
        result.Kind.ShouldBe(LeaveReturnKind.Early);

        await RunPayrollAsync();

        (await NumberOf(meId, "4000")).ShouldBe(5);
        (await EarningsOf(meId)).ShouldBe(500, tolerance: 0.001);
    }

    [Fact]
    public async Task A_Cancelled_Leave_Should_Drop_Out_Of_Payroll()
    {
        var (meId, approverId) = await GivenAnEssEmployeeOnPayrollAsync();

        var leaveId = await ApplyAndApproveAsync(new DateTime(2026, 9, 13), new DateTime(2026, 9, 17), approverId);

        var cancellationId = await _myLeaveAppService.RequestCancellationOfMyLeave(new RequestLeaveCancellationInput { Id = leaveId });

        // Still approved while the cancellation waits: payroll pays it as leave.
        await RunPayrollAsync();
        (await NumberOf(meId, "4000")).ShouldBe(5);

        // The run locked the month; HR releases it for correction before the cancellation can be approved.
        ReleaseMonthForCorrection();
        await _approvalService.ApproveAsync(cancellationId, approverId, "ok");

        await RunPayrollAsync();
        (await NumberOf(meId, "4000")).ShouldBe(0);
        (await AmountOf(meId, "1003")).ShouldBe(0);
        (await EarningsOf(meId)).ShouldBe(500, tolerance: 0.001);
    }

    // ------------------------------------------------------------------------------------------------ arrange

    private async Task<(int EmployeeId, int ApproverId)> GivenAnEssEmployeeOnPayrollAsync()
    {
        var seeder = Resolve<IReferenceDataSeeder>();
        foreach (var pack in new[] { "universal", "kuwait", "payroll" })
        {
            AsyncHelper.RunSync(() => seeder.SeedPackAsync(pack, TenantId));
        }

        // The leaves below sit on fixed September 2026 dates; lift the backdating window so they stay valid
        // whatever day the suite runs.
        UsingDbContext(context => context.Set<AbsenceType>().Single(t => t.Code == "1").AllowBackDatedDays = -1);

        await _goLive.LoadEmployees(new GoLiveLoadEmployeesInput
        {
            OrganizationUnitName = "ABC Holding",
            PayrollAreaName = Area,
            Employees = new System.Collections.Generic.List<GoLiveEmployeeRowDto>
            {
                new GoLiveEmployeeRowDto
                {
                    Code = EmployeeCode,
                    FullName = "Employee " + EmployeeCode,
                    JoiningDate = new DateTime(2020, 1, 15),
                    CivilId = "290011501001",
                    BasicSalary = 500
                }
            }
        });

        // The loaded employee is the signed-in user, so ESS acts as them.
        var employeeId = UsingDbContext(context =>
        {
            var employee = context.Employees.Single(e => e.Code == EmployeeCode && !e.IsDeleted);
            employee.UserId = AbpSession.UserId;
            return employee.Id;
        });

        var approverId = EnableLeaveWorkflowWithApprover("E001APP");

        // One run before any leave exists, for two reasons: the accrual owns EmployeeAbsenceQuota and refuses to
        // run over a row it did not create, and an accrual-based leave type needs a quota row covering the dates
        // before it can be applied for at all (legacy "Leave Quota not found within the Deduction From - to
        // Dates"). The run locks the month, so it is released again for the ESS actions that follow.
        await RunPayrollAsync();
        ReleaseMonthForCorrection();

        return (employeeId, approverId);
    }

    private void EnableNotReturnFromLeaveLogic()
    {
        UsingDbContext(context =>
        {
            // BOTH are needed on a Regular run. With PayLeaveAsPerCutoff off, legacy ignores an unreturned leave
            // in a Regular run altogether (PayrollEngine Program.cs 926-930) -- pay simply stops at the leave's end.
            foreach (var settings in context.Set<ManagePayrollSettings>().ToList())
            {
                settings.EnableNotReturnFromLeaveLogic = true;
                settings.PayLeaveAsPerCutoff = true;
            }
        });
    }

    /// <summary>What HR does between a run and a correction: the locked month goes to "released for correction"
    /// (the same state Go-Live's own ReleaseForCorrectionAsync applies). The next run locks it again.</summary>
    private void ReleaseMonthForCorrection()
    {
        UsingDbContext(context =>
        {
            foreach (var record in context.Set<PayrollControlRecord>().Where(r => r.PeriodLocked).ToList())
            {
                record.ApplyState(PayrollPeriodState.ReleasedForCorrection, AbpSession.UserId, DateTime.Now);
            }
        });
    }

    /// <summary>A transactional unit of work, as every app service call gets in production.</summary>
    private async Task InTransactionAsync(Func<Task> action)
    {
        using var unitOfWork = Resolve<Abp.Domain.Uow.IUnitOfWorkManager>()
            .Begin(new Abp.Domain.Uow.UnitOfWorkOptions { IsTransactional = true });
        await action();
        await unitOfWork.CompleteAsync();
    }

    private int AnnualLeaveTypeId() =>
        UsingDbContext(context => context.Set<AbsenceType>().Single(t => t.Code == "1").Id);

    private async Task<int> ApplyAndApproveAsync(DateTime start, DateTime end, int approverId)
    {
        var applied = await _myLeaveAppService.ApplyForLeave(new ApplyForLeaveInput
        {
            AbsenceTypeId = AnnualLeaveTypeId(),
            StartDate = start,
            EndDate = end
        });

        await _approvalService.ApproveAsync(applied.WorkflowRequestId!.Value, approverId, "ok");
        return applied.EmployeeAbsenceId;
    }

    private async Task<ReturnFromLeaveResultDto> ReturnAndApproveAsync(int leaveId, DateTime rejoiningDate, int approverId)
    {
        var result = await _myLeaveAppService.ReturnFromMyLeave(new ReturnFromLeaveInput
        {
            EmployeeAbsenceId = leaveId,
            RejoiningDate = rejoiningDate
        });

        await _approvalService.ApproveAsync(result.WorkflowRequestId, approverId, "ok");
        return result;
    }

    private async Task RunPayrollAsync()
    {
        var run = await _goLive.RunPayroll(new GoLiveRunPayrollInput { PayDate = PayDate, PayrollAreaName = Area });
        Dump(run);
        run.Succeeded.ShouldBeTrue(run.Message + " | " + string.Join("; ", run.Employees.Select(e => e.EmployeeCode + ": " + e.Message)));
    }

    private async Task<GoLiveSalaryRegisterDto> RegisterOf(int employeeId)
    {
        var register = await _goLive.GetSalaryRegister(new GoLiveSalaryRegisterInput { PayDate = PayDate, EmployeeId = employeeId });

        foreach (var line in register.Lines.OrderBy(l => l.WageCode))
        {
            _output.WriteLine($"{line.WageCode,-6} {line.WageName,-40} {line.Group,-12} rate {line.Rate,10:0.000} no {line.Number,8:0.###} amt {line.Amount,10:0.000}");
        }

        return register;
    }

    private async Task<double> NumberOf(int employeeId, string wageCode) =>
        (await RegisterOf(employeeId)).Lines.Where(l => l.WageCode == wageCode).Sum(l => l.Number);

    private async Task<double> AmountOf(int employeeId, string wageCode) =>
        (await RegisterOf(employeeId)).Lines.Where(l => l.WageCode == wageCode).Sum(l => l.Amount);

    /// <summary>Basic salary plus annual leave pay: what the employee earns for the month's days.</summary>
    private async Task<double> EarningsOf(int employeeId)
    {
        var lines = (await RegisterOf(employeeId)).Lines;
        return lines.Where(l => l.WageCode == "1000" || l.WageCode == "1003").Sum(l => l.Amount);
    }

    private void Dump(GoLiveRunPayrollOutput run)
    {
        _output.WriteLine($"run: succeeded={run.Succeeded} stoppedAt={run.StoppedAt} {run.Message}");
        foreach (var e in run.Employees)
        {
            _output.WriteLine($"  {e.EmployeeCode}: {e.Succeeded} {e.Message}");
        }
    }
}
