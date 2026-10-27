using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.Ess.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// Leave salary paid IN ADVANCE, asked for from ESS -- legacy's <c>LeavePaymentType.PayInAdvance</c> plus its
/// payment-date rules. The off-cycle run pays whatever this leaves on the row, so the date matters: it is what
/// <c>OffCycleAdvanceLeaveScenario_Tests</c> assumes an advance-paid leave carries.
/// </summary>
// ReSharper disable once InconsistentNaming
public class LeavePayInAdvance_Tests : EssLeaveWorkflowTestBase
{
    private int _meId;
    private int _approverId;

    private int GivenAnEmployee(string code)
    {
        _meId = CreateCurrentUserEmployee(code);
        _approverId = EnableLeaveWorkflowWithApprover(code + "APP");
        return _meId;
    }

    private void SetAdvancePaymentDateSettings(bool before15, bool after15) =>
        UsingDbContext(context =>
        {
            var settings = context.Set<ManagePayrollSettings>().FirstOrDefault();
            if (settings == null)
            {
                context.Set<ManagePayrollSettings>().Add(new ManagePayrollSettings
                {
                    TenantId = TenantId,
                    LeaveAdvPaymentdateBefore15 = before15,
                    LeaveAdvPaymentdateAfter15 = after15
                });
            }
            else
            {
                settings.LeaveAdvPaymentdateBefore15 = before15;
                settings.LeaveAdvPaymentdateAfter15 = after15;
            }
        });

    private EmployeeAbsence LeaveOf(int leaveId) => LeaveRow(leaveId);

    /// <summary>The 5th of a month at least two months out, so the whole month is in the future.</summary>
    private static DateTime EarlyInAFutureMonth =>
        new DateTime(DateTime.Today.AddMonths(2).Year, DateTime.Today.AddMonths(2).Month, 5);

    private static DateTime LateInAFutureMonth =>
        new DateTime(DateTime.Today.AddMonths(2).Year, DateTime.Today.AddMonths(2).Month, 20);

    [Fact]
    public async Task An_Employee_Should_Be_Able_To_Ask_For_The_Leave_Salary_In_Advance()
    {
        GivenAnEmployee("ADV100");
        var typeId = CreateAbsenceType();
        CreateQuota(_meId, typeId, accrued: 30);
        SetAdvancePaymentDateSettings(before15: true, after15: true);

        var start = EarlyInAFutureMonth;

        var applied = await _myLeaveAppService.ApplyForLeave(new ApplyForLeaveInput
        {
            AbsenceTypeId = typeId,
            StartDate = start,
            EndDate = start.AddDays(4),
            LeavePaymentType = LeavePaymentType.PayInAdvance
        });

        var leave = LeaveOf(applied.EmployeeAbsenceId);
        leave.LeavePaymentType.ShouldBe(LeavePaymentType.PayInAdvance);

        // Legacy's default before approval: the 1st, for a leave starting on or before the 15th.
        leave.PaymentDate.ShouldBe(new DateTime(start.Year, start.Month, 1));
    }

    [Fact]
    public async Task A_Leave_Starting_After_The_15th_Should_Default_To_The_16th()
    {
        GivenAnEmployee("ADV110");
        var typeId = CreateAbsenceType();
        CreateQuota(_meId, typeId, accrued: 30);
        SetAdvancePaymentDateSettings(before15: true, after15: true);

        var start = LateInAFutureMonth;

        var applied = await _myLeaveAppService.ApplyForLeave(new ApplyForLeaveInput
        {
            AbsenceTypeId = typeId,
            StartDate = start,
            EndDate = start.AddDays(3),
            LeavePaymentType = LeavePaymentType.PayInAdvance
        });

        LeaveOf(applied.EmployeeAbsenceId).PaymentDate.ShouldBe(new DateTime(start.Year, start.Month, 16));
    }

    [Fact]
    public async Task Approval_Should_Move_The_Payment_Date_To_A_Working_Day()
    {
        // Legacy recomputes on approval: the first day that is not a week-off, holiday or rest day, searching
        // from the 1st (leave starting on or before the 15th). With a Friday/Saturday weekend, a month whose
        // 1st is a Friday pays on the Sunday.
        GivenAnEmployee("ADV120");
        var typeId = CreateAbsenceType();
        CreateQuota(_meId, typeId, accrued: 30);
        SetAdvancePaymentDateSettings(before15: true, after15: true);
        SetupFridaySaturdayWeekend(_meId);

        var month = FirstFutureMonthStartingOnA(DayOfWeek.Friday);
        var start = new DateTime(month.Year, month.Month, 10);

        var applied = await _myLeaveAppService.ApplyForLeave(new ApplyForLeaveInput
        {
            AbsenceTypeId = typeId,
            StartDate = start,
            EndDate = start.AddDays(4),
            LeavePaymentType = LeavePaymentType.PayInAdvance
        });

        await _approvalService.ApproveAsync(applied.WorkflowRequestId!.Value, _approverId, "ok");

        // 1st Friday and 2nd Saturday are skipped; the 3rd is the Sunday.
        LeaveOf(applied.EmployeeAbsenceId).PaymentDate.ShouldBe(new DateTime(month.Year, month.Month, 3));
    }

    [Fact]
    public async Task A_Payment_Date_After_The_Start_Month_Should_Be_Refused()
    {
        GivenAnEmployee("ADV130");
        var typeId = CreateAbsenceType();
        CreateQuota(_meId, typeId, accrued: 30);

        var start = EarlyInAFutureMonth;

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _myLeaveAppService.ApplyForLeave(new ApplyForLeaveInput
            {
                AbsenceTypeId = typeId,
                StartDate = start,
                EndDate = start.AddDays(4),
                LeavePaymentType = LeavePaymentType.PayInAdvance,
                PaymentDate = start.AddMonths(1)
            }));

        exception.Message.ShouldContain("payment date");
    }

    [Fact]
    public async Task An_Advance_With_No_Payment_Date_At_All_Should_Be_Refused()
    {
        // The settings offer no default, so nothing fills the date in and legacy's own check fires.
        GivenAnEmployee("ADV140");
        var typeId = CreateAbsenceType();
        CreateQuota(_meId, typeId, accrued: 30);
        SetAdvancePaymentDateSettings(before15: false, after15: false);

        var start = EarlyInAFutureMonth;

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _myLeaveAppService.ApplyForLeave(new ApplyForLeaveInput
            {
                AbsenceTypeId = typeId,
                StartDate = start,
                EndDate = start.AddDays(4),
                LeavePaymentType = LeavePaymentType.PayInAdvance
            }));
    }

    [Fact]
    public async Task An_Ordinary_Leave_Should_Still_Be_Paid_With_The_Month()
    {
        GivenAnEmployee("ADV150");
        var typeId = CreateAbsenceType();
        CreateQuota(_meId, typeId, accrued: 30);

        var applied = await _myLeaveAppService.ApplyForLeave(new ApplyForLeaveInput
        {
            AbsenceTypeId = typeId,
            StartDate = EarlyInAFutureMonth,
            EndDate = EarlyInAFutureMonth.AddDays(2)
        });

        var leave = LeaveOf(applied.EmployeeAbsenceId);
        leave.LeavePaymentType.ShouldBe(LeavePaymentType.PayWithPayroll);
        leave.PaymentDate.ShouldBe(default(DateTime));
    }

    private static DateTime FirstFutureMonthStartingOnA(DayOfWeek day)
    {
        var month = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(2);

        while (month.DayOfWeek != day)
        {
            month = month.AddMonths(1);
        }

        return month;
    }
}
