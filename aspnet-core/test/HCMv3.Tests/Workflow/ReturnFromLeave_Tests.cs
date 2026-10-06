using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.Ess;
using HCMv3.Ess.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings;
using HCMv3.TimeManagement;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// Covers returning from leave -- which is deliberately NOT a transaction type of its own.
///
/// <para>
/// Legacy had a <c>ReturnFromLeaveRequest</c> business object, its own table and its own approval route. In
/// HCMv3 there is one leave table, so a return is an amendment on the leave: on time stamps the return, early
/// pulls the end date in, and late (legacy parity) books the gap on approval as new approved leave rows --
/// paid of the same type up to the balance, the rest unpaid. The payload is JSON on the request, so none of
/// this is a schema change.
/// </para>
///
/// <para>
/// The test that matters most in this file is the rejected one. A return is a change to a leave that is
/// already approved and already in payroll's view; declining it must leave that leave exactly as it was,
/// dates, return stamp and all. Getting that wrong would mean an employee whose early return was refused
/// nonetheless loses the days.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class ReturnFromLeave_Tests : EssLeaveWorkflowTestBase
{
    [Fact]
    public async Task Returning_On_Time_Should_Stamp_The_Return_Without_Moving_Any_Dates()
    {
        // Nothing changed about the leave -- the employee took exactly what was granted. All that is being
        // recorded is that they are back.
        var meId = CreateCurrentUserEmployee("RET100");
        var approverId = EnableLeaveWorkflowWithApprover("RET100APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var start = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 5, approverId);

        var before = LeaveRow(leaveId);

        var result = await _myLeaveAppService.ReturnFromMyLeave(new ReturnFromLeaveInput
        {
            EmployeeAbsenceId = leaveId,
            RejoiningDate = before.EndDate.AddDays(1)
        });

        result.IsEarlyReturn.ShouldBeFalse();
        result.DaysReturned.ShouldBe(0);

        await _approvalService.ApproveAsync(result.WorkflowRequestId, approverId, "welcome back");

        var after = LeaveRow(leaveId);
        after.IsReturned.ShouldBeTrue();
        after.ReturnDate.Date.ShouldBe(before.EndDate.Date.AddDays(1));

        after.StartDate.ShouldBe(before.StartDate);
        after.EndDate.ShouldBe(before.EndDate);
        after.TotalLeaveDays.ShouldBe(before.TotalLeaveDays);
        after.WorkflowStatus.ShouldBe(RequestStatus.Approved);
    }

    [Fact]
    public async Task Returning_Early_Should_Shorten_The_Leave_And_Keep_The_Original_End_Date()
    {
        // InitialEndDate is the point: after the change, the row's EndDate is the day they actually came back
        // to, and the only remaining record of what was granted is InitialEndDate.
        var meId = CreateCurrentUserEmployee("RET110");
        var approverId = EnableLeaveWorkflowWithApprover("RET110APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var start = DateTime.Today.AddDays(30);
        var originalEnd = start.AddDays(4);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 5, approverId);

        // Back two days early: last day of leave becomes start+2.
        var rejoiningDate = start.AddDays(3);

        var result = await _myLeaveAppService.ReturnFromMyLeave(new ReturnFromLeaveInput
        {
            EmployeeAbsenceId = leaveId,
            RejoiningDate = rejoiningDate,
            Reason = "Work came up"
        });

        result.IsEarlyReturn.ShouldBeTrue();
        result.NewEndDate.ShouldBe(start.AddDays(2).Date);
        result.DaysReturned.ShouldBe(2);

        await _approvalService.ApproveAsync(result.WorkflowRequestId, approverId, "fine");

        var after = LeaveRow(leaveId);
        after.EndDate.Date.ShouldBe(start.AddDays(2).Date);
        after.InitialEndDate.Date.ShouldBe(originalEnd.Date);
        after.ReturnDate.Date.ShouldBe(rejoiningDate.Date);
        after.IsReturned.ShouldBeTrue();
        after.TotalLeaveDays.ShouldBe(3);
        after.WorkflowStatus.ShouldBe(RequestStatus.Approved);
    }

    [Fact]
    public async Task Returning_Early_Should_Give_The_Unused_Days_Back_To_The_Quota()
    {
        var meId = CreateCurrentUserEmployee("RET115");
        var approverId = EnableLeaveWorkflowWithApprover("RET115APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var start = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 5, approverId);

        var result = await _myLeaveAppService.ReturnFromMyLeave(new ReturnFromLeaveInput
        {
            EmployeeAbsenceId = leaveId,
            RejoiningDate = start.AddDays(3)
        });

        await _approvalService.ApproveAsync(result.WorkflowRequestId, approverId, "fine");

        var quota = UsingDbContext(context => context.Set<EmployeeAbsenceQuota>()
            .IgnoreQueryFilters()
            .Single(q => q.EmployeeId == meId && q.AbsenceTypeId == typeId));

        // Five were taken out on approval of the leave; three of them were actually used.
        quota.QuotaUsed.ShouldBe(3);
    }

    [Fact]
    public async Task A_Rejected_Return_Should_Leave_The_Leave_Completely_Untouched()
    {
        // THE test. A refused early return must not cost the employee the days: the leave keeps its dates, is
        // still approved, and carries no return stamp at all -- as though the return had never been asked for.
        var meId = CreateCurrentUserEmployee("RET120");
        var approverId = EnableLeaveWorkflowWithApprover("RET120APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var start = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 5, approverId);

        var before = LeaveRow(leaveId);

        var result = await _myLeaveAppService.ReturnFromMyLeave(new ReturnFromLeaveInput
        {
            EmployeeAbsenceId = leaveId,
            RejoiningDate = start.AddDays(3),
            Reason = "Back early"
        });

        await _approvalService.RejectAsync(result.WorkflowRequestId, approverId, "Finish your leave");

        var after = LeaveRow(leaveId);
        after.WorkflowStatus.ShouldBe(RequestStatus.Approved);
        after.StartDate.ShouldBe(before.StartDate);
        after.EndDate.ShouldBe(before.EndDate);
        after.TotalLeaveDays.ShouldBe(before.TotalLeaveDays);

        // The three fields the return would have written stay exactly as they were.
        after.IsReturned.ShouldBeFalse();
        after.ReturnDate.ShouldBe(before.ReturnDate);
        after.InitialEndDate.ShouldBe(before.InitialEndDate);
    }

    [Fact]
    public async Task A_Rejected_Return_Should_Not_Disturb_The_Quota_Either()
    {
        var meId = CreateCurrentUserEmployee("RET125");
        var approverId = EnableLeaveWorkflowWithApprover("RET125APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var start = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 5, approverId);

        var result = await _myLeaveAppService.ReturnFromMyLeave(new ReturnFromLeaveInput
        {
            EmployeeAbsenceId = leaveId,
            RejoiningDate = start.AddDays(3)
        });

        await _approvalService.RejectAsync(result.WorkflowRequestId, approverId, "Finish your leave");

        var quota = UsingDbContext(context => context.Set<EmployeeAbsenceQuota>()
            .IgnoreQueryFilters()
            .Single(q => q.EmployeeId == meId && q.AbsenceTypeId == typeId));

        quota.QuotaUsed.ShouldBe(5);
    }

    [Fact]
    public async Task An_Early_Return_Should_Be_Refused_For_A_Leave_Type_That_Must_Be_Taken_In_Full()
    {
        // Maternity is the usual example: the entitlement is not a pot of days the employee may hand back.
        var meId = CreateCurrentUserEmployee("RET130");
        var approverId = EnableLeaveWorkflowWithApprover("RET130APP");
        var typeId = CreateAbsenceType(allowToReturnEarly: false, nameEn: "Maternity");
        CreateQuota(meId, typeId, accrued: 30);

        var start = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 5, approverId);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _myLeaveAppService.ReturnFromMyLeave(new ReturnFromLeaveInput
            {
                EmployeeAbsenceId = leaveId,
                RejoiningDate = start.AddDays(3)
            }));

        exception.Message.ShouldContain("Maternity");

        // And nothing was left half-raised against the leave.
        LeaveRow(leaveId).IsReturned.ShouldBeFalse();
    }

    [Fact]
    public async Task An_On_Time_Return_Should_Still_Be_Allowed_For_A_Type_That_Forbids_Returning_Early()
    {
        // The rule is about cutting the leave short, not about recording that the employee came back.
        var meId = CreateCurrentUserEmployee("RET140");
        var approverId = EnableLeaveWorkflowWithApprover("RET140APP");
        var typeId = CreateAbsenceType(allowToReturnEarly: false, nameEn: "Maternity");
        CreateQuota(meId, typeId, accrued: 30);

        var start = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 5, approverId);

        var result = await _myLeaveAppService.ReturnFromMyLeave(new ReturnFromLeaveInput
        {
            EmployeeAbsenceId = leaveId,
            RejoiningDate = start.AddDays(5)
        });

        await _approvalService.ApproveAsync(result.WorkflowRequestId, approverId, "ok");

        LeaveRow(leaveId).IsReturned.ShouldBeTrue();
    }

    // ---------------------------------------------------------------------------------------------------------
    // LATE joining -- legacy AbsenceRequest ActionType.ReturnFromLeave, DaysDifference > 0 (lines 1485-1643).
    // The gap is booked on approval as further paid leave of the same type (from the quota) and/or unpaid
    // leave (type code "2"). The original leave keeps its dates and is only marked returned.
    // ---------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Late_Return_Preview_Should_Propose_All_Paid_When_The_Balance_Covers_The_Gap()
    {
        var meId = CreateCurrentUserEmployee("RET150");
        var approverId = EnableLeaveWorkflowWithApprover("RET150APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var start = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 5, approverId);

        // Approved to start+4, back on start+8: three extra days (start+5, +6, +7).
        var preview = await _myLeaveAppService.PreviewReturnFromMyLeave(new ReturnFromLeaveInput
        {
            EmployeeAbsenceId = leaveId,
            RejoiningDate = start.AddDays(8)
        });

        preview.Kind.ShouldBe(LeaveReturnKind.Late);
        preview.DaysDifference.ShouldBe(3);
        preview.NewEndDate.ShouldBe(start.AddDays(4).Date);
        preview.Bookable.ShouldBe(25);
        preview.PaidStartDate.ShouldBe(start.AddDays(5).Date);
        preview.PaidEndDate.ShouldBe(start.AddDays(7).Date);
        preview.UnpaidStartDate.ShouldBeNull();
        preview.UnpaidEndDate.ShouldBeNull();
    }

    [Fact]
    public async Task My_Leaves_Should_Show_A_Late_Return_As_One_Row_With_The_Extra_Days_Folded_In()
    {
        var meId = CreateCurrentUserEmployee("RET158");
        var approverId = EnableLeaveWorkflowWithApprover("RET158APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var start = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 5, approverId);

        var result = await _myLeaveAppService.ReturnFromMyLeave(new ReturnFromLeaveInput
        {
            EmployeeAbsenceId = leaveId,
            RejoiningDate = start.AddDays(8),
            Reason = "Flight delayed"
        });
        await _approvalService.ApproveAsync(result.WorkflowRequestId, approverId, "ok");

        // Payroll has two records, the leave and the three late days...
        LeaveRowsOf(meId).Count.ShouldBe(2);

        // ...the employee has one leave: the one they took, as long as it turned out to be.
        var list = await _myLeaveAppService.GetMyLeaves(new GetMyLeavesInput { IncludeHistory = true });
        list.TotalCount.ShouldBe(1);

        var row = list.Items.Single();
        row.Id.ShouldBe(leaveId);
        row.StartDate.ShouldBe(start.Date);
        row.EndDate.ShouldBe(start.AddDays(7).Date);
        row.TotalLeaveDays.ShouldBe(8);
        row.RequestAction.ShouldBe(WorkflowRequestAction.Return);
        row.DisplayStatus.ShouldBe(MyLeaveRequestStatus.Approved);
    }

    [Fact]
    public async Task Late_Return_With_Enough_Balance_Should_Book_The_Gap_As_Paid_Leave_Of_The_Same_Type()
    {
        var meId = CreateCurrentUserEmployee("RET151");
        var approverId = EnableLeaveWorkflowWithApprover("RET151APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var start = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 5, approverId);
        var before = LeaveRow(leaveId);

        var result = await _myLeaveAppService.ReturnFromMyLeave(new ReturnFromLeaveInput
        {
            EmployeeAbsenceId = leaveId,
            RejoiningDate = start.AddDays(8),
            Reason = "Flight delayed"
        });

        result.Kind.ShouldBe(LeaveReturnKind.Late);
        result.PaidCalendarDays.ShouldBe(3);
        result.UnpaidCalendarDays.ShouldBe(0);

        // Nothing is booked until somebody agrees.
        LeaveRowsOf(meId).Count.ShouldBe(1);

        await _approvalService.ApproveAsync(result.WorkflowRequestId, approverId, "ok");

        var rows = LeaveRowsOf(meId);
        rows.Count.ShouldBe(2);

        // The original: same dates, marked returned, and -- as legacy left it -- no rejoining date of its own.
        var original = rows.Single(r => r.Id == leaveId);
        original.StartDate.ShouldBe(before.StartDate);
        original.EndDate.ShouldBe(before.EndDate);
        original.IsReturned.ShouldBeTrue();
        original.ReturnDate.ShouldBe(default(DateTime));

        var paid = rows.Single(r => r.Id != leaveId);
        paid.AbsenceTypeId.ShouldBe(typeId);
        paid.StartDate.ShouldBe(start.AddDays(5).Date);
        paid.EndDate.ShouldBe(start.AddDays(7).Date);
        paid.TotalLeaveDays.ShouldBe(3);
        paid.LeavePaymentType.ShouldBe(LeavePaymentType.PayWithPayroll);
        paid.WorkflowStatus.ShouldBe(RequestStatus.Approved);
        paid.WorkflowRequestId.ShouldBe(result.WorkflowRequestId);
        paid.IsReturned.ShouldBeTrue();
        paid.ReturnDate.ShouldBe(start.AddDays(8).Date);

        // 5 for the leave + 3 late days, all against the same quota.
        QuotaRow(meId, typeId).QuotaUsed.ShouldBe(8);
    }

    [Fact]
    public async Task Late_Return_Beyond_The_Balance_Should_Split_The_Gap_Into_Paid_And_Unpaid_Leave()
    {
        var meId = CreateCurrentUserEmployee("RET152");
        var approverId = EnableLeaveWorkflowWithApprover("RET152APP");
        var typeId = CreateAbsenceType();
        var unpaidTypeId = EnsureUnpaidLeaveType();

        // Six days of entitlement, five taken: one day left for a three-day late gap.
        CreateQuota(meId, typeId, accrued: 6);

        var start = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 5, approverId);

        var preview = await _myLeaveAppService.PreviewReturnFromMyLeave(new ReturnFromLeaveInput
        {
            EmployeeAbsenceId = leaveId,
            RejoiningDate = start.AddDays(8)
        });

        preview.PaidStartDate.ShouldBe(start.AddDays(5).Date);
        preview.PaidEndDate.ShouldBe(start.AddDays(5).Date);
        preview.UnpaidStartDate.ShouldBe(start.AddDays(6).Date);
        preview.UnpaidEndDate.ShouldBe(start.AddDays(7).Date);

        var result = await _myLeaveAppService.ReturnFromMyLeave(new ReturnFromLeaveInput
        {
            EmployeeAbsenceId = leaveId,
            RejoiningDate = start.AddDays(8)
        });

        await _approvalService.ApproveAsync(result.WorkflowRequestId, approverId, "ok");

        var rows = LeaveRowsOf(meId);
        rows.Count.ShouldBe(3);

        var paid = rows.Single(r => r.Id != leaveId && r.AbsenceTypeId == typeId);
        paid.StartDate.ShouldBe(start.AddDays(5).Date);
        paid.EndDate.ShouldBe(start.AddDays(5).Date);

        // The rejoining date belongs to the LAST generated row -- the one the employee actually came back from.
        // ...but the row is marked returned like every row of this leave, so it offers no further actions.
        paid.IsReturned.ShouldBeTrue();
        paid.ReturnDate.ShouldBe(default(DateTime));

        var unpaid = rows.Single(r => r.AbsenceTypeId == unpaidTypeId);
        unpaid.StartDate.ShouldBe(start.AddDays(6).Date);
        unpaid.EndDate.ShouldBe(start.AddDays(7).Date);
        unpaid.TotalLeaveDays.ShouldBe(2);
        unpaid.WorkflowStatus.ShouldBe(RequestStatus.Approved);
        unpaid.IsReturned.ShouldBeTrue();
        unpaid.ReturnDate.ShouldBe(start.AddDays(8).Date);

        QuotaRow(meId, typeId).QuotaUsed.ShouldBe(6);
    }

    [Fact]
    public async Task Late_Return_With_No_Balance_Left_Should_Book_The_Whole_Gap_As_Unpaid()
    {
        var meId = CreateCurrentUserEmployee("RET153");
        var approverId = EnableLeaveWorkflowWithApprover("RET153APP");
        var typeId = CreateAbsenceType();
        var unpaidTypeId = EnsureUnpaidLeaveType();
        CreateQuota(meId, typeId, accrued: 5);

        var start = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 5, approverId);

        var result = await _myLeaveAppService.ReturnFromMyLeave(new ReturnFromLeaveInput
        {
            EmployeeAbsenceId = leaveId,
            RejoiningDate = start.AddDays(8)
        });

        result.PaidCalendarDays.ShouldBe(0);
        result.UnpaidCalendarDays.ShouldBe(3);

        await _approvalService.ApproveAsync(result.WorkflowRequestId, approverId, "ok");

        var unpaid = LeaveRowsOf(meId).Single(r => r.AbsenceTypeId == unpaidTypeId);
        unpaid.StartDate.ShouldBe(start.AddDays(5).Date);
        unpaid.EndDate.ShouldBe(start.AddDays(7).Date);

        QuotaRow(meId, typeId).QuotaUsed.ShouldBe(5);
        LeaveRow(leaveId).IsReturned.ShouldBeTrue();
    }

    [Fact]
    public async Task Late_Return_Should_Honour_A_Split_The_Employee_Entered()
    {
        // Legacy let the employee choose to take the gap unpaid even with balance left.
        var meId = CreateCurrentUserEmployee("RET154");
        var approverId = EnableLeaveWorkflowWithApprover("RET154APP");
        var typeId = CreateAbsenceType();
        var unpaidTypeId = EnsureUnpaidLeaveType();
        CreateQuota(meId, typeId, accrued: 30);

        var start = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 5, approverId);

        var result = await _myLeaveAppService.ReturnFromMyLeave(new ReturnFromLeaveInput
        {
            EmployeeAbsenceId = leaveId,
            RejoiningDate = start.AddDays(8),
            UnpaidStartDate = start.AddDays(5),
            UnpaidEndDate = start.AddDays(7)
        });

        await _approvalService.ApproveAsync(result.WorkflowRequestId, approverId, "ok");

        LeaveRowsOf(meId).Count(r => r.AbsenceTypeId == unpaidTypeId).ShouldBe(1);
        LeaveRowsOf(meId).Count(r => r.AbsenceTypeId == typeId).ShouldBe(1);
        QuotaRow(meId, typeId).QuotaUsed.ShouldBe(5);
    }

    [Fact]
    public async Task Late_Return_Should_Be_Refused_When_Paid_And_Unpaid_Days_Do_Not_Cover_The_Gap()
    {
        // Legacy: "Difference Days should be equal to Total PaidUnpaidLeave days."
        var meId = CreateCurrentUserEmployee("RET155");
        var approverId = EnableLeaveWorkflowWithApprover("RET155APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var start = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 5, approverId);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _myLeaveAppService.ReturnFromMyLeave(new ReturnFromLeaveInput
            {
                EmployeeAbsenceId = leaveId,
                RejoiningDate = start.AddDays(8),
                PaidStartDate = start.AddDays(5),
                PaidEndDate = start.AddDays(6)
            }));

        exception.Message.ShouldContain("3 day(s) late");
        LeaveRow(leaveId).IsReturned.ShouldBeFalse();
    }

    [Fact]
    public async Task Late_Return_Should_Be_Refused_When_Paid_Leave_Does_Not_Start_The_Day_After_The_Leave()
    {
        // Legacy: "PaidLeave StartDate should be dd/MM/yyyy."
        var meId = CreateCurrentUserEmployee("RET156");
        var approverId = EnableLeaveWorkflowWithApprover("RET156APP");
        var typeId = CreateAbsenceType();
        EnsureUnpaidLeaveType();
        CreateQuota(meId, typeId, accrued: 30);

        var start = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 5, approverId);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _myLeaveAppService.ReturnFromMyLeave(new ReturnFromLeaveInput
            {
                EmployeeAbsenceId = leaveId,
                RejoiningDate = start.AddDays(8),
                UnpaidStartDate = start.AddDays(5),
                UnpaidEndDate = start.AddDays(5),
                PaidStartDate = start.AddDays(6),
                PaidEndDate = start.AddDays(7)
            }));

        exception.Message.ShouldContain(start.AddDays(5).ToString("dd/MM/yyyy"));
    }

    [Fact]
    public async Task Late_Return_Paid_Days_Should_Be_Refused_Beyond_The_Bookable_Balance()
    {
        // Legacy's quota test for every action: balance + previous days >= total + DeductFromQuota.
        var meId = CreateCurrentUserEmployee("RET157");
        var approverId = EnableLeaveWorkflowWithApprover("RET157APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 6);

        var start = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 5, approverId);

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _myLeaveAppService.ReturnFromMyLeave(new ReturnFromLeaveInput
            {
                EmployeeAbsenceId = leaveId,
                RejoiningDate = start.AddDays(8),
                PaidStartDate = start.AddDays(5),
                PaidEndDate = start.AddDays(7)
            }));
    }

    [Fact]
    public async Task Late_Return_Should_Be_Refused_When_Another_Leave_Starts_In_The_Gap()
    {
        // Legacy "Leave entry exists." -- those days are already accounted for.
        var meId = CreateCurrentUserEmployee("RET158");
        var approverId = EnableLeaveWorkflowWithApprover("RET158APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var start = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 5, approverId);
        await CreateApprovedLeaveAsync(typeId, start.AddDays(6), days: 1, approverId);

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _myLeaveAppService.ReturnFromMyLeave(new ReturnFromLeaveInput
            {
                EmployeeAbsenceId = leaveId,
                RejoiningDate = start.AddDays(8),
                PaidStartDate = start.AddDays(5),
                PaidEndDate = start.AddDays(7)
            }));
    }

    [Fact]
    public async Task A_Rejected_Late_Return_Should_Book_Nothing_And_Leave_The_Quota_Alone()
    {
        var meId = CreateCurrentUserEmployee("RET159");
        var approverId = EnableLeaveWorkflowWithApprover("RET159APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var start = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 5, approverId);

        var result = await _myLeaveAppService.ReturnFromMyLeave(new ReturnFromLeaveInput
        {
            EmployeeAbsenceId = leaveId,
            RejoiningDate = start.AddDays(8)
        });

        await _approvalService.RejectAsync(result.WorkflowRequestId, approverId, "Speak to HR");

        LeaveRowsOf(meId).Count.ShouldBe(1);
        LeaveRow(leaveId).IsReturned.ShouldBeFalse();
        QuotaRow(meId, typeId).QuotaUsed.ShouldBe(5);
    }

    // ---------------------------------------------------------------------------------------------------------
    // Week-off gap, day count, and the future-date rule.
    // ---------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Rejoining_After_Only_A_Weekend_Should_Count_As_On_Time()
    {
        // Legacy DaysDifference: a gap made up only of days the type excludes is zero. Leave Sunday-Thursday,
        // back on the following Sunday -- the Friday and Saturday in between are not two days late.
        var meId = CreateCurrentUserEmployee("RET190");
        var approverId = EnableLeaveWorkflowWithApprover("RET190APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);
        SetupFridaySaturdayWeekend(meId);

        var sunday = NextSunday(DateTime.Today.AddDays(30));
        var leaveId = await CreateApprovedLeaveAsync(typeId, sunday, days: 5, approverId);

        var preview = await _myLeaveAppService.PreviewReturnFromMyLeave(new ReturnFromLeaveInput
        {
            EmployeeAbsenceId = leaveId,
            RejoiningDate = sunday.AddDays(7)
        });

        preview.Kind.ShouldBe(LeaveReturnKind.OnTime);
        preview.DaysDifference.ShouldBe(0);

        var result = await _myLeaveAppService.ReturnFromMyLeave(new ReturnFromLeaveInput
        {
            EmployeeAbsenceId = leaveId,
            RejoiningDate = sunday.AddDays(7)
        });

        await _approvalService.ApproveAsync(result.WorkflowRequestId, approverId, "ok");

        var after = LeaveRow(leaveId);
        after.IsReturned.ShouldBeTrue();
        after.ReturnDate.ShouldBe(sunday.AddDays(7));
        after.EndDate.ShouldBe(sunday.AddDays(4));
        LeaveRowsOf(meId).Count.ShouldBe(1);
    }

    [Fact]
    public async Task Rejoining_After_A_Weekend_Plus_A_Working_Day_Should_Count_The_Full_Calendar_Gap_As_Late()
    {
        // Legacy returns the whole CALENDAR gap once any day in it is countable: Fri, Sat, Sun = 3, not 1.
        var meId = CreateCurrentUserEmployee("RET191");
        var approverId = EnableLeaveWorkflowWithApprover("RET191APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);
        SetupFridaySaturdayWeekend(meId);

        var sunday = NextSunday(DateTime.Today.AddDays(30));
        var leaveId = await CreateApprovedLeaveAsync(typeId, sunday, days: 5, approverId);

        var preview = await _myLeaveAppService.PreviewReturnFromMyLeave(new ReturnFromLeaveInput
        {
            EmployeeAbsenceId = leaveId,
            RejoiningDate = sunday.AddDays(8)
        });

        preview.Kind.ShouldBe(LeaveReturnKind.Late);
        preview.DaysDifference.ShouldBe(3);
    }

    [Fact]
    public async Task Late_Paid_Days_Across_A_Weekend_Should_Charge_Only_The_Working_Days()
    {
        // Back on Monday after a Sun-Thu leave: Fri, Sat, Sun late. DeductFromQuota = 3 calendar - 2 weekend
        // = 1, so the generated paid leave is Sunday alone and the quota is charged one day.
        var meId = CreateCurrentUserEmployee("RET192");
        var approverId = EnableLeaveWorkflowWithApprover("RET192APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);
        SetupFridaySaturdayWeekend(meId);

        var sunday = NextSunday(DateTime.Today.AddDays(30));
        var leaveId = await CreateApprovedLeaveAsync(typeId, sunday, days: 5, approverId);

        var result = await _myLeaveAppService.ReturnFromMyLeave(new ReturnFromLeaveInput
        {
            EmployeeAbsenceId = leaveId,
            RejoiningDate = sunday.AddDays(8)
        });

        result.PaidCalendarDays.ShouldBe(3);

        await _approvalService.ApproveAsync(result.WorkflowRequestId, approverId, "ok");

        var paid = LeaveRowsOf(meId).Single(r => r.Id != leaveId);
        paid.StartDate.ShouldBe(sunday.AddDays(7));
        paid.EndDate.ShouldBe(sunday.AddDays(7));
        paid.TotalLeaveDays.ShouldBe(1);

        QuotaRow(meId, typeId).QuotaUsed.ShouldBe(6);
    }

    [Fact]
    public async Task Applying_For_Leave_Across_A_Weekend_Should_Charge_Only_The_Working_Days()
    {
        // Legacy EmployeeAbsence.UpdateQuotaInformation: calendar days less the days the type excludes.
        var meId = CreateCurrentUserEmployee("RET193");
        var approverId = EnableLeaveWorkflowWithApprover("RET193APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);
        SetupFridaySaturdayWeekend(meId);

        // Sunday to the following Sunday: 8 calendar days, 2 of them weekend.
        var sunday = NextSunday(DateTime.Today.AddDays(30));
        var leaveId = await CreateApprovedLeaveAsync(typeId, sunday, days: 8, approverId);

        var leave = LeaveRow(leaveId);
        leave.CalendarDays.ShouldBe(8);
        leave.TotalLeaveDays.ShouldBe(6);
        QuotaRow(meId, typeId).QuotaUsed.ShouldBe(6);
    }

    [Fact]
    public async Task Returning_Early_Across_A_Weekend_Should_Refund_Only_The_Working_Days()
    {
        // Sun-next Sun (6 working days charged), back on Thursday: kept Sun-Wed = 4, refund 2.
        var meId = CreateCurrentUserEmployee("RET194");
        var approverId = EnableLeaveWorkflowWithApprover("RET194APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);
        SetupFridaySaturdayWeekend(meId);

        var sunday = NextSunday(DateTime.Today.AddDays(30));
        var leaveId = await CreateApprovedLeaveAsync(typeId, sunday, days: 8, approverId);

        var result = await _myLeaveAppService.ReturnFromMyLeave(new ReturnFromLeaveInput
        {
            EmployeeAbsenceId = leaveId,
            RejoiningDate = sunday.AddDays(4)
        });

        result.IsEarlyReturn.ShouldBeTrue();
        result.DaysReturned.ShouldBe(2);

        await _approvalService.ApproveAsync(result.WorkflowRequestId, approverId, "ok");

        LeaveRow(leaveId).TotalLeaveDays.ShouldBe(4);
        QuotaRow(meId, typeId).QuotaUsed.ShouldBe(4);
    }

    [Fact]
    public async Task A_Future_Rejoining_Date_Should_Be_Refused_Unless_The_Setting_Allows_It()
    {
        // Legacy: "ReturnDate can't be future date." unless TimeSettings.AllowFutureDatedReturnFromLeave.
        var meId = CreateCurrentUserEmployee("RET195");
        var approverId = EnableLeaveWorkflowWithApprover("RET195APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var start = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 5, approverId);

        SetAllowFutureDatedReturn(false);

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _myLeaveAppService.ReturnFromMyLeave(new ReturnFromLeaveInput
            {
                EmployeeAbsenceId = leaveId,
                RejoiningDate = start.AddDays(5)
            }));

        LeaveRow(leaveId).IsReturned.ShouldBeFalse();
    }

    [Fact]
    public async Task A_Rejoining_Date_On_Or_Before_The_Leave_Start_Should_Be_Refused()
    {
        // That is not a return, it is a leave that never happened -- cancel it instead.
        var meId = CreateCurrentUserEmployee("RET160");
        var approverId = EnableLeaveWorkflowWithApprover("RET160APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var start = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 5, approverId);

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _myLeaveAppService.ReturnFromMyLeave(new ReturnFromLeaveInput
            {
                EmployeeAbsenceId = leaveId,
                RejoiningDate = start
            }));
    }

    [Fact]
    public async Task A_Second_Return_Against_The_Same_Leave_Should_Be_Refused()
    {
        var meId = CreateCurrentUserEmployee("RET170");
        var approverId = EnableLeaveWorkflowWithApprover("RET170APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var start = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 5, approverId);

        var result = await _myLeaveAppService.ReturnFromMyLeave(new ReturnFromLeaveInput
        {
            EmployeeAbsenceId = leaveId,
            RejoiningDate = start.AddDays(3)
        });

        await _approvalService.ApproveAsync(result.WorkflowRequestId, approverId, "ok");

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _myLeaveAppService.ReturnFromMyLeave(new ReturnFromLeaveInput
            {
                EmployeeAbsenceId = leaveId,
                RejoiningDate = start.AddDays(2)
            }));
    }

    [Fact]
    public async Task Raising_A_Return_Should_Not_Change_The_Leave_Until_It_Is_Approved()
    {
        // Same promise every amendment makes, restated for the return path: payroll must see what it saw
        // before, right up to the moment somebody agrees.
        var meId = CreateCurrentUserEmployee("RET180");
        var approverId = EnableLeaveWorkflowWithApprover("RET180APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var start = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 5, approverId);

        var before = LeaveRow(leaveId);

        await _myLeaveAppService.ReturnFromMyLeave(new ReturnFromLeaveInput
        {
            EmployeeAbsenceId = leaveId,
            RejoiningDate = start.AddDays(3)
        });

        var after = LeaveRow(leaveId);
        after.EndDate.ShouldBe(before.EndDate);
        after.IsReturned.ShouldBeFalse();
        after.WorkflowStatus.ShouldBe(RequestStatus.Approved);
    }
}
