using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.Ess.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// The unified leave screen's actions -- amend, cancel, return -- as requests of their own against the one
/// leave row: sent back and resent, withdrawn, rejected and visible as rejected, routed through a chain of
/// their own; and the rules the leave type sets on who may apply and with what.
/// </summary>
// ReSharper disable once InconsistentNaming
public class LeaveUnifiedActions_Tests : EssLeaveWorkflowTestBase
{
    private static DateTime Soon => DateTime.Today.AddDays(30);

    private int AllDaysCountType(Action<AbsenceType> configure = null) => CreateAbsenceType(t =>
    {
        t.IncludeWeekend = true;
        t.IncludePublicHoliday = true;
        t.IncludeRestDay = true;
        configure?.Invoke(t);
    });

    private void SetEmployee(int employeeId, Action<Employee> change)
    {
        UsingDbContext(context => change(context.Set<Employee>().Single(e => e.Id == employeeId)));
    }

    private WorkflowRequest RequestRow(int requestId)
    {
        return UsingDbContext(context => context.Set<WorkflowRequest>().Single(r => r.Id == requestId));
    }

    private async Task<MyLeaveDto> MyLeaveAsync(int leaveId)
    {
        return (await _myLeaveAppService.GetMyLeaves(new GetMyLeavesInput { IncludeHistory = true }))
            .Items.Single(l => l.Id == leaveId);
    }

    // ------------------------------------------------------------------ sent back, resent, withdrawn

    [Fact]
    public async Task A_Sent_Back_Amendment_Should_Be_Resendable_With_Corrected_Dates()
    {
        var meId = CreateCurrentUserEmployee("UNI100");
        var approverId = EnableLeaveWorkflowWithApprover("UNI100APP");
        var typeId = AllDaysCountType();
        CreateQuota(meId, typeId, accrued: 30);

        var leaveId = await CreateApprovedLeaveAsync(typeId, Soon, days: 5, approverId);

        var requestId = await _myLeaveAppService.AmendMyLeave(new AmendMyLeaveInput
        {
            EmployeeAbsenceId = leaveId,
            AbsenceTypeId = typeId,
            StartDate = Soon,
            EndDate = Soon.AddDays(11),
            Reason = "Longer trip"
        });

        await _approvalService.SendBackAsync(requestId, approverId, "Too long");

        var dto = await MyLeaveAsync(leaveId);
        dto.HasPendingAmendment.ShouldBeTrue();
        dto.PendingIsSentBack.ShouldBeTrue();
        dto.PendingAction.ShouldBe(WorkflowRequestAction.Amendment);

        var resent = await _myLeaveAppService.ResubmitMyLeaveChange(new ResubmitMyLeaveChangeInput
        {
            EmployeeAbsenceId = leaveId,
            EndDate = Soon.AddDays(7)
        });

        // The same request goes round again: one number, one log.
        resent.ShouldBe(requestId);
        RequestRow(requestId).RequestStatus.ShouldBe(RequestStatus.Open);

        // Still untouched until approved...
        LeaveRow(leaveId).EndDate.ShouldBe(Soon.AddDays(4));

        await _approvalService.ApproveAsync(requestId, approverId, "ok");

        // ...and then it carries the CORRECTED dates, not the ones first asked for.
        LeaveRow(leaveId).EndDate.ShouldBe(Soon.AddDays(7));
        QuotaRow(meId, typeId).QuotaUsed.ShouldBe(8);
    }

    [Fact]
    public async Task Withdrawing_A_Sent_Back_Change_Should_Free_The_Leave_For_Another_Action()
    {
        var meId = CreateCurrentUserEmployee("UNI110");
        var approverId = EnableLeaveWorkflowWithApprover("UNI110APP");
        var typeId = AllDaysCountType();
        CreateQuota(meId, typeId, accrued: 30);

        var leaveId = await CreateApprovedLeaveAsync(typeId, Soon, days: 5, approverId);

        var requestId = await _myLeaveAppService.RequestCancellationOfMyLeave(
            new RequestLeaveCancellationInput { Id = leaveId, Reason = "Plans changed" });
        await _approvalService.SendBackAsync(requestId, approverId, "Why?");

        await _myLeaveAppService.WithdrawMyLeaveChange(new WithdrawMyLeaveChangeInput { Id = leaveId, Reason = "Keeping it" });

        RequestRow(requestId).RequestStatus.ShouldBe(RequestStatus.Closed);

        var leave = LeaveRow(leaveId);
        leave.WorkflowStatus.ShouldBe(RequestStatus.Approved);
        QuotaRow(meId, typeId).QuotaUsed.ShouldBe(5);

        var dto = await MyLeaveAsync(leaveId);
        dto.HasPendingAmendment.ShouldBeFalse();
        dto.CanAmend.ShouldBeTrue();
        dto.CanRequestCancellation.ShouldBeTrue();
        // Not yet: this leave starts a month from now, and a return is only offered once a leave has begun.
        dto.CanReturn.ShouldBeFalse();
    }

    [Fact]
    public async Task Withdrawing_A_Change_Still_With_The_Approver_Should_Close_It_And_Log_It()
    {
        var meId = CreateCurrentUserEmployee("UNI120");
        var approverId = EnableLeaveWorkflowWithApprover("UNI120APP");
        var typeId = AllDaysCountType();
        CreateQuota(meId, typeId, accrued: 30);

        var leaveId = await CreateApprovedLeaveAsync(typeId, Soon, days: 5, approverId);

        var requestId = await _myLeaveAppService.AmendMyLeave(new AmendMyLeaveInput
        {
            EmployeeAbsenceId = leaveId,
            AbsenceTypeId = typeId,
            StartDate = Soon,
            EndDate = Soon.AddDays(6),
            Reason = "One more day"
        });

        await _myLeaveAppService.WithdrawMyLeaveChange(new WithdrawMyLeaveChangeInput { Id = leaveId });

        RequestRow(requestId).RequestStatus.ShouldBe(RequestStatus.Closed);

        UsingDbContext(context => context.Set<RequestLog>()
            .Any(l => l.WorkflowRequestId == requestId && l.ActionType == RequestActionType.Closed && l.ActionById == meId))
            .ShouldBeTrue();

        // Nobody can decide a withdrawn request.
        await Assert.ThrowsAsync<UserFriendlyException>(() => _approvalService.ApproveAsync(requestId, approverId, "ok"));
    }

    [Fact]
    public async Task A_Sent_Back_New_Leave_Should_Be_Resendable_With_Corrected_Dates()
    {
        var meId = CreateCurrentUserEmployee("UNI130");
        var approverId = EnableLeaveWorkflowWithApprover("UNI130APP");
        var typeId = AllDaysCountType();
        CreateQuota(meId, typeId, accrued: 30);

        var applied = await _myLeaveAppService.ApplyForLeave(new ApplyForLeaveInput
        {
            AbsenceTypeId = typeId,
            StartDate = Soon,
            EndDate = Soon.AddDays(9)
        });

        await _approvalService.SendBackAsync(applied.WorkflowRequestId.Value, approverId, "Take a week instead");

        (await MyLeaveAsync(applied.EmployeeAbsenceId)).IsSentBack.ShouldBeTrue();

        var resent = await _myLeaveAppService.ResubmitMyLeave(new ResubmitMyLeaveInput
        {
            EmployeeAbsenceId = applied.EmployeeAbsenceId,
            AbsenceTypeId = typeId,
            StartDate = Soon,
            EndDate = Soon.AddDays(6)
        });

        resent.WorkflowRequestId.ShouldBe(applied.WorkflowRequestId);

        var leave = LeaveRow(applied.EmployeeAbsenceId);
        leave.EndDate.ShouldBe(Soon.AddDays(6));
        leave.TotalLeaveDays.ShouldBe(7);
        leave.WorkflowStatus.ShouldBe(RequestStatus.Open);

        await _approvalService.ApproveAsync(applied.WorkflowRequestId.Value, approverId, "ok");
        QuotaRow(meId, typeId).QuotaUsed.ShouldBe(7);
    }

    [Fact]
    public async Task Only_A_Sent_Back_Change_Should_Be_Resendable()
    {
        var meId = CreateCurrentUserEmployee("UNI140");
        var approverId = EnableLeaveWorkflowWithApprover("UNI140APP");
        var typeId = AllDaysCountType();
        CreateQuota(meId, typeId, accrued: 30);

        var leaveId = await CreateApprovedLeaveAsync(typeId, Soon, days: 5, approverId);

        await _myLeaveAppService.AmendMyLeave(new AmendMyLeaveInput
        {
            EmployeeAbsenceId = leaveId,
            AbsenceTypeId = typeId,
            StartDate = Soon,
            EndDate = Soon.AddDays(6),
            Reason = "One more day"
        });

        // Still with the approver: there is nothing to correct yet.
        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _myLeaveAppService.ResubmitMyLeaveChange(new ResubmitMyLeaveChangeInput { EmployeeAbsenceId = leaveId }));
    }

    // ------------------------------------------------------------------ Save (draft), then Submit

    [Fact]
    public async Task A_Saved_Leave_Should_Be_A_Draft_Nobody_Else_Sees_Until_It_Is_Submitted()
    {
        var meId = CreateCurrentUserEmployee("UNI150");
        var approverId = EnableLeaveWorkflowWithApprover("UNI150APP");
        var typeId = AllDaysCountType();
        CreateQuota(meId, typeId, accrued: 30);

        var saved = await _myLeaveAppService.SaveMyLeaveDraft(new SaveMyLeaveDraftInput
        {
            AbsenceTypeId = typeId,
            StartDate = Soon,
            EndDate = Soon.AddDays(4),
            Comments = "Thinking about it"
        });

        saved.SubmittedForApproval.ShouldBeFalse();
        saved.WorkflowRequestId.ShouldBeNull();

        var dto = await MyLeaveAsync(saved.EmployeeAbsenceId);
        dto.IsDraft.ShouldBeTrue();
        dto.IsSentBack.ShouldBeFalse();
        dto.CanCancel.ShouldBeTrue();

        // A draft is not a claim on the balance, and no request exists for anybody to approve.
        (await _myLeaveAppService.GetMyBalances()).Single(b => b.AbsenceTypeId == typeId).Bookable.ShouldBe(30);
        UsingDbContext(context => context.Set<WorkflowRequest>().Any(r => r.TargetEntityId == saved.EmployeeAbsenceId)).ShouldBeFalse();

        // Saved again with other dates: the same row, still a draft.
        await _myLeaveAppService.SaveMyLeaveDraft(new SaveMyLeaveDraftInput
        {
            Id = saved.EmployeeAbsenceId,
            AbsenceTypeId = typeId,
            StartDate = Soon,
            EndDate = Soon.AddDays(2)
        });

        var edit = await _myLeaveAppService.GetMyLeaveForEdit(new EntityDto(saved.EmployeeAbsenceId));
        edit.EndDate.ShouldBe(Soon.AddDays(2));
        edit.TotalLeaveDays.ShouldBe(3);

        var submitted = await _myLeaveAppService.SubmitMyLeaveDraft(new EntityDto(saved.EmployeeAbsenceId));
        submitted.SubmittedForApproval.ShouldBeTrue();

        LeaveRow(saved.EmployeeAbsenceId).WorkflowStatus.ShouldBe(RequestStatus.Open);

        await _approvalService.ApproveAsync(submitted.WorkflowRequestId.Value, approverId, "ok");
        QuotaRow(meId, typeId).QuotaUsed.ShouldBe(3);
    }

    [Fact]
    public async Task A_Saved_Amendment_Should_Wait_As_A_Draft_And_Then_Be_Submitted_Or_Discarded()
    {
        var meId = CreateCurrentUserEmployee("UNI160");
        var approverId = EnableLeaveWorkflowWithApprover("UNI160APP");
        var typeId = AllDaysCountType();
        CreateQuota(meId, typeId, accrued: 30);

        var leaveId = await CreateApprovedLeaveAsync(typeId, Soon, days: 5, approverId);

        var draftId = await _myLeaveAppService.SaveMyLeaveChangeDraft(new SaveMyLeaveChangeDraftInput
        {
            EmployeeAbsenceId = leaveId,
            Action = WorkflowRequestAction.Amendment,
            EndDate = Soon.AddDays(8),
            Reason = "Maybe longer"
        });

        // Nobody has it to approve, and the leave is untouched.
        RequestRow(draftId).RequestStatus.ShouldBe(RequestStatus.Draft);
        UsingDbContext(context => context.Set<WorkFlowApprover>().Any(a => a.WorkflowRequestId == draftId)).ShouldBeFalse();

        var dto = await MyLeaveAsync(leaveId);
        dto.HasPendingAmendment.ShouldBeTrue();
        dto.PendingIsDraft.ShouldBeTrue();
        dto.PendingIsSentBack.ShouldBeFalse();
        dto.ProposedEndDate.ShouldBe(Soon.AddDays(8));
        dto.PendingReason.ShouldBe("Maybe longer");

        // Saved again: the same draft, new dates.
        var again = await _myLeaveAppService.SaveMyLeaveChangeDraft(new SaveMyLeaveChangeDraftInput
        {
            EmployeeAbsenceId = leaveId,
            EndDate = Soon.AddDays(6)
        });
        again.ShouldBe(draftId);

        await _myLeaveAppService.ResubmitMyLeaveChange(new ResubmitMyLeaveChangeInput { EmployeeAbsenceId = leaveId });
        RequestRow(draftId).RequestStatus.ShouldBe(RequestStatus.Open);

        await _approvalService.ApproveAsync(draftId, approverId, "ok");
        LeaveRow(leaveId).EndDate.ShouldBe(Soon.AddDays(6));

        // A second draft, this time a cancellation, discarded: the leave stays and can be acted on again.
        await _myLeaveAppService.SaveMyLeaveChangeDraft(new SaveMyLeaveChangeDraftInput
        {
            EmployeeAbsenceId = leaveId,
            Action = WorkflowRequestAction.Cancellation,
            Reason = "Not sure"
        });

        (await MyLeaveAsync(leaveId)).PendingAction.ShouldBe(WorkflowRequestAction.Cancellation);

        await _myLeaveAppService.WithdrawMyLeaveChange(new WithdrawMyLeaveChangeInput { Id = leaveId });

        var after = await MyLeaveAsync(leaveId);
        after.HasPendingAmendment.ShouldBeFalse();
        after.WorkflowStatus.ShouldBe(RequestStatus.Approved);
        after.CanAmend.ShouldBeTrue();
    }

    // ------------------------------------------------------------------ what the list and the history say

    [Fact]
    public async Task A_Rejected_Amendment_Should_Leave_The_Leave_Approved_And_Say_It_Was_Rejected()
    {
        var meId = CreateCurrentUserEmployee("UNI200");
        var approverId = EnableLeaveWorkflowWithApprover("UNI200APP");
        var typeId = AllDaysCountType();
        CreateQuota(meId, typeId, accrued: 30);

        var leaveId = await CreateApprovedLeaveAsync(typeId, Soon, days: 5, approverId);

        var requestId = await _myLeaveAppService.AmendMyLeave(new AmendMyLeaveInput
        {
            EmployeeAbsenceId = leaveId,
            AbsenceTypeId = typeId,
            StartDate = Soon,
            EndDate = Soon.AddDays(9),
            Reason = "Longer trip"
        });

        var pending = await MyLeaveAsync(leaveId);
        pending.EndDate.ShouldBe(Soon.AddDays(4));
        pending.ProposedEndDate.ShouldBe(Soon.AddDays(9));

        await _approvalService.RejectAsync(requestId, approverId, "No cover");

        var dto = await MyLeaveAsync(leaveId);
        dto.WorkflowStatus.ShouldBe(RequestStatus.Approved);
        dto.EndDate.ShouldBe(Soon.AddDays(4));
        dto.HasPendingAmendment.ShouldBeFalse();
        dto.LastRejectedAction.ShouldBe(WorkflowRequestAction.Amendment);

        // And the employee may ask again.
        dto.CanAmend.ShouldBeTrue();
        await _myLeaveAppService.AmendMyLeave(new AmendMyLeaveInput
        {
            EmployeeAbsenceId = leaveId,
            AbsenceTypeId = typeId,
            StartDate = Soon,
            EndDate = Soon.AddDays(6),
            Reason = "Shorter than before"
        });
    }

    [Fact]
    public async Task History_Should_List_Every_Request_Against_The_Leave_With_Its_Outcome()
    {
        var meId = CreateCurrentUserEmployee("UNI210");
        var approverId = EnableLeaveWorkflowWithApprover("UNI210APP");
        var typeId = AllDaysCountType();
        CreateQuota(meId, typeId, accrued: 30);

        var leaveId = await CreateApprovedLeaveAsync(typeId, Soon, days: 5, approverId);

        var rejectedId = await _myLeaveAppService.AmendMyLeave(new AmendMyLeaveInput
        {
            EmployeeAbsenceId = leaveId, AbsenceTypeId = typeId, StartDate = Soon, EndDate = Soon.AddDays(11), Reason = "Long"
        });
        await _approvalService.RejectAsync(rejectedId, approverId, "No cover");

        var approvedId = await _myLeaveAppService.AmendMyLeave(new AmendMyLeaveInput
        {
            EmployeeAbsenceId = leaveId, AbsenceTypeId = typeId, StartDate = Soon, EndDate = Soon.AddDays(6), Reason = "Short"
        });
        await _approvalService.ApproveAsync(approvedId, approverId, "ok");

        await _myLeaveAppService.RequestCancellationOfMyLeave(new RequestLeaveCancellationInput { Id = leaveId, Reason = "Off" });
        await _myLeaveAppService.WithdrawMyLeaveChange(new WithdrawMyLeaveChangeInput { Id = leaveId });

        var history = await _myLeaveAppService.GetMyLeaveHistory(new EntityDto(leaveId));

        history.Select(h => h.Action).ShouldBe(new[]
        {
            WorkflowRequestAction.New,
            WorkflowRequestAction.Amendment,
            WorkflowRequestAction.Amendment,
            WorkflowRequestAction.Cancellation
        });

        history.Select(h => h.Outcome).ShouldBe(new[]
        {
            MyLeaveHistoryOutcome.Approved,
            MyLeaveHistoryOutcome.Rejected,
            MyLeaveHistoryOutcome.Approved,
            MyLeaveHistoryOutcome.Withdrawn
        });

        // The application shows what was first asked for, not what the leave has since become.
        history[0].EndDate.ShouldBe(Soon.AddDays(4));

        history[1].BeforeEndDate.ShouldBe(Soon.AddDays(4));
        history[1].EndDate.ShouldBe(Soon.AddDays(11));
        history[1].DecisionRemark.ShouldBe("No cover");

        history[2].BeforeEndDate.ShouldBe(Soon.AddDays(4));
        history[2].EndDate.ShouldBe(Soon.AddDays(6));
    }

    [Fact]
    public async Task A_Returned_Leave_Should_Not_Be_Amendable_Or_Cancellable()
    {
        var meId = CreateCurrentUserEmployee("UNI220");
        var approverId = EnableLeaveWorkflowWithApprover("UNI220APP");
        var typeId = AllDaysCountType();
        CreateQuota(meId, typeId, accrued: 30);

        var leaveId = await CreateApprovedLeaveAsync(typeId, Soon, days: 5, approverId);

        var returned = await _myLeaveAppService.ReturnFromMyLeave(new ReturnFromLeaveInput
        {
            EmployeeAbsenceId = leaveId,
            RejoiningDate = Soon.AddDays(5)
        });
        await _approvalService.ApproveAsync(returned.WorkflowRequestId, approverId, "ok");

        RequestRow(returned.WorkflowRequestId).RequestAction.ShouldBe(WorkflowRequestAction.Return);

        var dto = await MyLeaveAsync(leaveId);
        dto.IsReturned.ShouldBeTrue();
        dto.CanAmend.ShouldBeFalse();
        dto.CanRequestCancellation.ShouldBeFalse();
        dto.CanReturn.ShouldBeFalse();

        await Assert.ThrowsAsync<UserFriendlyException>(() => _myLeaveAppService.AmendMyLeave(new AmendMyLeaveInput
        {
            EmployeeAbsenceId = leaveId, AbsenceTypeId = typeId, StartDate = Soon, EndDate = Soon.AddDays(6), Reason = "Late idea"
        }));

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _myLeaveAppService.RequestCancellationOfMyLeave(new RequestLeaveCancellationInput { Id = leaveId }));
    }

    [Fact]
    public async Task An_Amendment_That_Changes_Nothing_Should_Be_Refused()
    {
        var meId = CreateCurrentUserEmployee("UNI230");
        var approverId = EnableLeaveWorkflowWithApprover("UNI230APP");
        var typeId = AllDaysCountType();
        CreateQuota(meId, typeId, accrued: 30);

        var leaveId = await CreateApprovedLeaveAsync(typeId, Soon, days: 5, approverId);

        await Assert.ThrowsAsync<UserFriendlyException>(() => _myLeaveAppService.AmendMyLeave(new AmendMyLeaveInput
        {
            EmployeeAbsenceId = leaveId, AbsenceTypeId = typeId, StartDate = Soon, EndDate = Soon.AddDays(4), Reason = "Same"
        }));
    }

    // ------------------------------------------------------------------ balances

    [Fact]
    public async Task A_Pending_Amendment_Should_Reserve_The_Extra_Days_It_Asks_For()
    {
        var meId = CreateCurrentUserEmployee("UNI300");
        var approverId = EnableLeaveWorkflowWithApprover("UNI300APP");
        var typeId = AllDaysCountType();
        CreateQuota(meId, typeId, accrued: 30);

        var leaveId = await CreateApprovedLeaveAsync(typeId, Soon, days: 5, approverId);

        (await _myLeaveAppService.GetMyBalances()).Single(b => b.AbsenceTypeId == typeId).Bookable.ShouldBe(25);

        var requestId = await _myLeaveAppService.AmendMyLeave(new AmendMyLeaveInput
        {
            EmployeeAbsenceId = leaveId, AbsenceTypeId = typeId, StartDate = Soon, EndDate = Soon.AddDays(9), Reason = "Longer"
        });

        var balance = (await _myLeaveAppService.GetMyBalances()).Single(b => b.AbsenceTypeId == typeId);
        balance.Remaining.ShouldBe(25);
        balance.Pending.ShouldBe(5);
        balance.Bookable.ShouldBe(20);

        // The change must not count against itself when it is approved.
        await _approvalService.ApproveAsync(requestId, approverId, "ok");
        (await _myLeaveAppService.GetMyBalances()).Single(b => b.AbsenceTypeId == typeId).Bookable.ShouldBe(20);
    }

    [Fact]
    public async Task An_Amendment_Using_The_Whole_Balance_Should_Still_Be_Approvable()
    {
        var meId = CreateCurrentUserEmployee("UNI310");
        var approverId = EnableLeaveWorkflowWithApprover("UNI310APP");
        var typeId = AllDaysCountType();
        CreateQuota(meId, typeId, accrued: 10);

        var leaveId = await CreateApprovedLeaveAsync(typeId, Soon, days: 5, approverId);

        var requestId = await _myLeaveAppService.AmendMyLeave(new AmendMyLeaveInput
        {
            EmployeeAbsenceId = leaveId, AbsenceTypeId = typeId, StartDate = Soon, EndDate = Soon.AddDays(9), Reason = "All of it"
        });

        await _approvalService.ApproveAsync(requestId, approverId, "ok");

        QuotaRow(meId, typeId).QuotaUsed.ShouldBe(10);
    }

    [Fact]
    public async Task The_Balance_Preview_Should_Show_Current_Bookable_And_After()
    {
        var meId = CreateCurrentUserEmployee("UNI320");
        var approverId = EnableLeaveWorkflowWithApprover("UNI320APP");
        var typeId = AllDaysCountType();
        CreateQuota(meId, typeId, accrued: 30);

        await CreateApprovedLeaveAsync(typeId, Soon, days: 5, approverId);

        await _myLeaveAppService.ApplyForLeave(new ApplyForLeaveInput
        {
            AbsenceTypeId = typeId, StartDate = Soon.AddDays(20), EndDate = Soon.AddDays(22)
        });

        var preview = await _myLeaveAppService.PreviewMyLeaveBalance(new PreviewLeaveBalanceInput
        {
            AbsenceTypeId = typeId, StartDate = Soon.AddDays(40), EndDate = Soon.AddDays(43)
        });

        preview.HasQuota.ShouldBeTrue();
        preview.CurrentBalance.ShouldBe(25);
        preview.Bookable.ShouldBe(22);
        preview.DaysRequested.ShouldBe(4);
        preview.BalanceAfter.ShouldBe(18);
    }

    // ------------------------------------------------------------------ a chain per action

    [Fact]
    public async Task A_Cancellation_Should_Follow_Its_Own_Chain_When_One_Is_Configured()
    {
        var meId = CreateCurrentUserEmployee("UNI400");
        var approverId = EnableLeaveWorkflowWithApprover("UNI400APP");
        var hrId = CreateEmployee("UNI400HR");
        var typeId = AllDaysCountType();
        CreateQuota(meId, typeId, accrued: 30);

        UsingDbContext(context =>
        {
            var role = context.Set<WorkflowRole>().Add(new WorkflowRole
            {
                TenantId = TenantId, Code = "HRCAN", NameEn = "HR", NameAr = "HR"
            }).Entity;

            var master = context.Set<RequestMaster>().Add(new RequestMaster
            {
                TenantId = TenantId, Code = "LEAVE-CAN", NameEn = "Leave Cancellation", NameAr = "Leave Cancellation", WorkflowSteps = 1
            }).Entity;

            context.SaveChanges();

            context.Set<RequestDetail>().Add(new RequestDetail
            {
                TenantId = TenantId,
                RequestMasterId = master.Id,
                Step = 1,
                WorkflowRoleId = role.Id,
                ApproverPerson = WorkflowApproverPersonType.Person,
                ApprovalRequire = ApprovalRequire.Single,
                CanReject = true,
                CanSendBack = true
            });

            context.Set<EmployeeWorkflowRole>().Add(new EmployeeWorkflowRole
            {
                TenantId = TenantId,
                EmployeeId = hrId,
                WorkflowRoleId = role.Id,
                StartDate = DateTime.Today.AddYears(-1),
                EndDate = DateTime.Today.AddYears(2)
            });

            context.Set<WorkflowTransactionType>().First(t => t.Code == "EmployeeAbsence").CancellationRequestMasterId = master.Id;
        });

        var leaveId = await CreateApprovedLeaveAsync(typeId, Soon, days: 5, approverId);

        // An amendment has no chain of its own, so it follows the leave's.
        var amendmentId = await _myLeaveAppService.AmendMyLeave(new AmendMyLeaveInput
        {
            EmployeeAbsenceId = leaveId, AbsenceTypeId = typeId, StartDate = Soon, EndDate = Soon.AddDays(6), Reason = "More"
        });
        await _approvalService.ApproveAsync(amendmentId, approverId, "ok");

        var cancellationId = await _myLeaveAppService.RequestCancellationOfMyLeave(
            new RequestLeaveCancellationInput { Id = leaveId, Reason = "Off" });

        RequestRow(cancellationId).RequestAction.ShouldBe(WorkflowRequestAction.Cancellation);

        // The leave's own approver is not on the cancellation chain; HR is.
        await Assert.ThrowsAsync<UserFriendlyException>(() => _approvalService.ApproveAsync(cancellationId, approverId, "ok"));
        await _approvalService.ApproveAsync(cancellationId, hrId, "ok");

        LeaveRow(leaveId).WorkflowStatus.ShouldBe(RequestStatus.Cancelled);
    }

    // ------------------------------------------------------------------ who may apply for which type

    [Fact]
    public async Task Leave_Types_Should_Be_Offered_By_Gender_And_Religion()
    {
        var meId = CreateCurrentUserEmployee("UNI500");

        var religionId = UsingDbContext(context =>
        {
            var religion = context.Set<Religion>().Add(new Religion
            {
                TenantId = TenantId, Code = "M", NameEn = "Muslim", NameAr = "مسلم"
            }).Entity;
            context.SaveChanges();
            return religion.Id;
        });

        SetEmployee(meId, e =>
        {
            e.Gender = Gender.Female;
            e.ReligionId = religionId;
        });

        var everyone = CreateAbsenceType(t => { }, "Annual");
        var maternity = CreateAbsenceType(t => t.LeaveGender = LeaveGender.Female, "Maternity");
        var paternity = CreateAbsenceType(t => t.LeaveGender = LeaveGender.Male, "Paternity");
        var iddahMuslim = CreateAbsenceType(t => { t.LeaveGender = LeaveGender.Female; t.LeaveReligion = LeaveReligion.Muslim; }, "Iddah (Muslim)");
        var iddahOther = CreateAbsenceType(t => { t.LeaveGender = LeaveGender.Female; t.LeaveReligion = LeaveReligion.NonMuslim; }, "Iddah (Non-Muslim)");
        var inactive = CreateAbsenceType(t => { }, "Retired type");

        UsingDbContext(context =>
        {
            foreach (var type in context.Set<AbsenceType>().Where(t => t.Id != inactive))
            {
                type.IsActive = true;
            }
        });

        var offered = (await _myLeaveAppService.GetMyLeaveTypes()).Select(t => t.Id).ToList();

        offered.ShouldContain(everyone);
        offered.ShouldContain(maternity);
        offered.ShouldContain(iddahMuslim);
        offered.ShouldNotContain(paternity);
        offered.ShouldNotContain(iddahOther);
        offered.ShouldNotContain(inactive);
    }

    [Fact]
    public async Task A_Leave_Type_For_The_Other_Gender_Should_Be_Refused_On_Submit()
    {
        // Test employees are male by default.
        var meId = CreateCurrentUserEmployee("UNI510");
        EnableLeaveWorkflowWithApprover("UNI510APP");
        var maternity = AllDaysCountType(t => t.LeaveGender = LeaveGender.Female);
        CreateQuota(meId, maternity, accrued: 70);

        await Assert.ThrowsAsync<UserFriendlyException>(() => _myLeaveAppService.ApplyForLeave(new ApplyForLeaveInput
        {
            AbsenceTypeId = maternity, StartDate = Soon, EndDate = Soon.AddDays(4)
        }));
    }

    [Fact]
    public async Task A_Leave_Too_Far_Ahead_Should_Be_Refused_When_The_Type_Limits_It()
    {
        var meId = CreateCurrentUserEmployee("UNI520");
        EnableLeaveWorkflowWithApprover("UNI520APP");
        var typeId = AllDaysCountType(t => t.MaxFutureDays = 60);
        CreateQuota(meId, typeId, accrued: 30);

        await Assert.ThrowsAsync<UserFriendlyException>(() => _myLeaveAppService.ApplyForLeave(new ApplyForLeaveInput
        {
            AbsenceTypeId = typeId, StartDate = DateTime.Today.AddDays(61), EndDate = DateTime.Today.AddDays(63)
        }));

        // Inside the window it goes through.
        await _myLeaveAppService.ApplyForLeave(new ApplyForLeaveInput
        {
            AbsenceTypeId = typeId, StartDate = DateTime.Today.AddDays(59), EndDate = DateTime.Today.AddDays(60)
        });
    }

    [Fact]
    public async Task A_Type_That_Requires_Policy_Acceptance_Should_Refuse_Without_It_And_Record_It_With_It()
    {
        var meId = CreateCurrentUserEmployee("UNI530");
        EnableLeaveWorkflowWithApprover("UNI530APP");
        var typeId = AllDaysCountType(t => t.RequirePolicyAcceptance = true);
        CreateQuota(meId, typeId, accrued: 30);

        await Assert.ThrowsAsync<UserFriendlyException>(() => _myLeaveAppService.ApplyForLeave(new ApplyForLeaveInput
        {
            AbsenceTypeId = typeId, StartDate = Soon, EndDate = Soon.AddDays(2)
        }));

        var applied = await _myLeaveAppService.ApplyForLeave(new ApplyForLeaveInput
        {
            AbsenceTypeId = typeId, StartDate = Soon, EndDate = Soon.AddDays(2), PolicyAccepted = true
        });

        LeaveRow(applied.EmployeeAbsenceId).PolicyAcceptedOn.ShouldNotBeNull();
    }

    [Fact]
    public async Task A_Partial_Only_Type_Should_Refuse_A_Full_Day_Leave()
    {
        var meId = CreateCurrentUserEmployee("UNI540");
        EnableLeaveWorkflowWithApprover("UNI540APP");
        var typeId = AllDaysCountType(t =>
        {
            t.AllowPartialLeave = true;
            t.LeaveDurationType = LeaveDurationType.Partial;
        });
        CreateQuota(meId, typeId, accrued: 30);

        await Assert.ThrowsAsync<UserFriendlyException>(() => _myLeaveAppService.ApplyForLeave(new ApplyForLeaveInput
        {
            AbsenceTypeId = typeId, StartDate = Soon, EndDate = Soon
        }));
    }

    [Fact]
    public async Task An_Hourly_Leave_Longer_Than_The_Type_Allows_Should_Be_Refused()
    {
        var meId = CreateCurrentUserEmployee("UNI550");
        EnableLeaveWorkflowWithApprover("UNI550APP");
        var typeId = AllDaysCountType(t =>
        {
            t.AllowPartialLeave = true;
            t.ResetFrequency = ResetFrequency.Monthly;
            t.MaxMinutes = 120;
            t.MaxCount = 4;
            t.TotalMinutes = 480;
        });
        CreateQuota(meId, typeId, accrued: 30);

        await Assert.ThrowsAsync<UserFriendlyException>(() => _myLeaveAppService.ApplyForLeave(new ApplyForLeaveInput
        {
            AbsenceTypeId = typeId, StartDate = Soon, EndDate = Soon, StartTime = "08:00", EndTime = "11:00"
        }));
    }

    // ------------------------------------------------------------------ delegates

    [Fact]
    public async Task Delegates_Should_Be_Checked_Before_The_Leave_Goes_For_Approval()
    {
        var meId = CreateCurrentUserEmployee("UNI600");
        var approverId = EnableLeaveWorkflowWithApprover("UNI600APP");
        var colleagueId = CreateEmployee("UNI600COL");
        var awayId = CreateEmployee("UNI600AWAY");
        SetEmployee(colleagueId, e => e.Status = EmployeeStatus.Active);
        SetEmployee(awayId, e => e.Status = EmployeeStatus.Active);

        var typeId = AllDaysCountType();
        CreateQuota(meId, typeId, accrued: 30);

        // The colleague who is away over the same dates.
        UsingDbContext(context => context.Set<EmployeeAbsence>().Add(new EmployeeAbsence
        {
            TenantId = TenantId,
            EmployeeId = awayId,
            AbsenceTypeId = typeId,
            StartDate = Soon.AddDays(1),
            EndDate = Soon.AddDays(3),
            TotalLeaveDays = 3,
            WorkflowStatus = RequestStatus.Approved
        }));

        ApplyForLeaveInput Leave(Action<ApplyForLeaveInput> with)
        {
            var input = new ApplyForLeaveInput { AbsenceTypeId = typeId, StartDate = Soon, EndDate = Soon.AddDays(4) };
            with(input);
            return input;
        }

        // Yourself.
        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _myLeaveAppService.ApplyForLeave(Leave(i => i.PrimaryDelegateId = meId)));

        // A secondary with no primary.
        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _myLeaveAppService.ApplyForLeave(Leave(i => i.SecondaryDelegateId = colleagueId)));

        // Somebody who is on leave on those days.
        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _myLeaveAppService.ApplyForLeave(Leave(i => i.PrimaryDelegateId = awayId)));

        // Somebody who is not active (the approver in this harness is left inactive).
        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _myLeaveAppService.ApplyForLeave(Leave(i => i.PrimaryDelegateId = approverId)));

        var applied = await _myLeaveAppService.ApplyForLeave(Leave(i => i.PrimaryDelegateId = colleagueId));
        LeaveRow(applied.EmployeeAbsenceId).PrimaryDelegateId.ShouldBe(colleagueId);
    }

    [Fact]
    public async Task An_Approver_Delegate_Should_Become_A_Delegation_That_Follows_The_Leave()
    {
        var meId = CreateCurrentUserEmployee("UNI610");
        var approverId = EnableLeaveWorkflowWithApprover("UNI610APP");
        var delegateId = CreateEmployee("UNI610DEL");
        SetEmployee(delegateId, e => e.Status = EmployeeStatus.Active);

        var typeId = AllDaysCountType();
        CreateQuota(meId, typeId, accrued: 30);

        var applied = await _myLeaveAppService.ApplyForLeave(new ApplyForLeaveInput
        {
            AbsenceTypeId = typeId, StartDate = Soon, EndDate = Soon.AddDays(4), ApproverDelegateId = delegateId
        });

        EmployeeDelegation Delegation() => UsingDbContext(context => context.Set<EmployeeDelegation>()
            .IgnoreQueryFilters()
            .SingleOrDefault(d => d.EmployeeId == meId && !d.IsDeleted));

        // Asking for cover is not having it: nothing exists until the leave is approved.
        Delegation().ShouldBeNull();

        await _approvalService.ApproveAsync(applied.WorkflowRequestId.Value, approverId, "ok");

        var delegation = Delegation();
        delegation.ShouldNotBeNull();
        delegation.DelegateToId.ShouldBe(delegateId);
        delegation.StartDate.ShouldBe(Soon);
        delegation.EndDate.ShouldBe(Soon.AddDays(4));
        delegation.WorkflowStatus.ShouldBe(RequestStatus.Approved);

        // New dates move it.
        var amendmentId = await _myLeaveAppService.AmendMyLeave(new AmendMyLeaveInput
        {
            EmployeeAbsenceId = applied.EmployeeAbsenceId, AbsenceTypeId = typeId, StartDate = Soon, EndDate = Soon.AddDays(7), Reason = "Longer"
        });
        await _approvalService.ApproveAsync(amendmentId, approverId, "ok");
        Delegation().EndDate.ShouldBe(Soon.AddDays(7));

        // A cancelled leave takes it away.
        var cancellationId = await _myLeaveAppService.RequestCancellationOfMyLeave(
            new RequestLeaveCancellationInput { Id = applied.EmployeeAbsenceId, Reason = "Off" });
        await _approvalService.ApproveAsync(cancellationId, approverId, "ok");
        Delegation().ShouldBeNull();
    }
}
