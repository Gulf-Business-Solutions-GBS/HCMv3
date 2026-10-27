using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.Ess.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// Cancelling an APPROVED leave -- legacy <c>AbsenceRequest</c> <c>ActionType.Cancellation</c>.
///
/// <para>
/// In HCMv3 the cancellation is an amendment on the one leave row, not a request table of its own. Until it is
/// approved the leave stays approved and visible to payroll; on approval the row is deleted and its days go
/// back to the quota, as legacy <c>EmployeeAbsence.OnDeleting</c> did.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class LeaveCancellation_Tests : EssLeaveWorkflowTestBase
{
    [Fact]
    public async Task Requesting_Cancellation_Should_Leave_The_Leave_Approved_And_Visible_Until_Granted()
    {
        var meId = CreateCurrentUserEmployee("CAN100");
        var approverId = EnableLeaveWorkflowWithApprover("CAN100APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var leaveId = await CreateApprovedLeaveAsync(typeId, DateTime.Today.AddDays(30), days: 5, approverId);

        await _myLeaveAppService.RequestCancellationOfMyLeave(new RequestLeaveCancellationInput { Id = leaveId });

        var leave = LeaveRow(leaveId);
        leave.IsDeleted.ShouldBeFalse();
        leave.WorkflowStatus.ShouldBe(RequestStatus.Approved);
        QuotaRow(meId, typeId).QuotaUsed.ShouldBe(5);

        // Payroll's view -- the ApprovedOnly filter left ON -- still sees it.
        UsingDbContext(context => context.Set<EmployeeAbsence>().Any(a => a.Id == leaveId)).ShouldBeTrue();
    }

    [Fact]
    public async Task An_Approved_Cancellation_Should_Mark_The_Leave_Cancelled_And_Refund_The_Quota()
    {
        var meId = CreateCurrentUserEmployee("CAN110");
        var approverId = EnableLeaveWorkflowWithApprover("CAN110APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var leaveId = await CreateApprovedLeaveAsync(typeId, DateTime.Today.AddDays(30), days: 5, approverId);

        var requestId = await _myLeaveAppService.RequestCancellationOfMyLeave(new RequestLeaveCancellationInput { Id = leaveId });
        await _approvalService.ApproveAsync(requestId, approverId, "ok");

        // Marked, not deleted, so it stays in the employee history...
        var leave = LeaveRow(leaveId);
        leave.IsDeleted.ShouldBeFalse();
        leave.WorkflowStatus.ShouldBe(RequestStatus.Cancelled);

        // ...while payroll -- the ApprovedOnly filter left ON -- no longer sees it.
        // Read through a repository inside a unit of work: the filter is ABP's, and a bare DbContext has none.
        var unitOfWorkManager = Resolve<Abp.Domain.Uow.IUnitOfWorkManager>();
        using (var uow = unitOfWorkManager.Begin())
        {
            Resolve<Abp.Domain.Repositories.IRepository<EmployeeAbsence>>()
                .GetAll().Any(a => a.Id == leaveId).ShouldBeFalse();

            await uow.CompleteAsync();
        }
        QuotaRow(meId, typeId).QuotaUsed.ShouldBe(0);
    }

    [Fact]
    public async Task A_Rejected_Cancellation_Should_Leave_The_Leave_And_Quota_Untouched()
    {
        var meId = CreateCurrentUserEmployee("CAN120");
        var approverId = EnableLeaveWorkflowWithApprover("CAN120APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var leaveId = await CreateApprovedLeaveAsync(typeId, DateTime.Today.AddDays(30), days: 5, approverId);

        var requestId = await _myLeaveAppService.RequestCancellationOfMyLeave(new RequestLeaveCancellationInput { Id = leaveId });
        await _approvalService.RejectAsync(requestId, approverId, "Needed on those days");

        var leave = LeaveRow(leaveId);
        leave.IsDeleted.ShouldBeFalse();
        leave.WorkflowStatus.ShouldBe(RequestStatus.Approved);
        QuotaRow(meId, typeId).QuotaUsed.ShouldBe(5);
    }

    [Fact]
    public async Task The_List_Should_Show_A_Granted_Cancellation_As_Cancel_Leave_Approved()
    {
        var meId = CreateCurrentUserEmployee("CAN170");
        var approverId = EnableLeaveWorkflowWithApprover("CAN170APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var leaveId = await CreateApprovedLeaveAsync(typeId, DateTime.Today.AddDays(30), days: 5, approverId);

        var row = await ListRowAsync(leaveId);
        row.RequestAction.ShouldBe(WorkflowRequestAction.New);
        row.DisplayStatus.ShouldBe(MyLeaveRequestStatus.Approved);
        var leaveNumber = row.RequestNumber;
        leaveNumber.ShouldNotBeNullOrWhiteSpace();

        var requestId = await _myLeaveAppService.RequestCancellationOfMyLeave(new RequestLeaveCancellationInput { Id = leaveId });

        row = await ListRowAsync(leaveId);

        // The row keeps the number the leave was applied for under; the cancellation has its own, underneath.
        row.RequestNumber.ShouldBe(leaveNumber);
        row.Requests.Count.ShouldBe(2);
        row.Requests[0].RequestNumber.ShouldBe(leaveNumber);
        row.Requests[0].Action.ShouldBe(WorkflowRequestAction.New);
        row.Requests[1].RequestNumber.ShouldNotBe(leaveNumber);
        row.Requests[1].Action.ShouldBe(WorkflowRequestAction.Cancellation);
        row.Requests[1].Outcome.ShouldBe(MyLeaveHistoryOutcome.AwaitingApproval);
        row.RequestAction.ShouldBe(WorkflowRequestAction.Cancellation);
        row.DisplayStatus.ShouldBe(MyLeaveRequestStatus.Open);

        await _approvalService.ApproveAsync(requestId, approverId, "ok");

        // One of the standard statuses, never a status of its own: the request type says what was approved.
        row = await ListRowAsync(leaveId);
        row.RequestAction.ShouldBe(WorkflowRequestAction.Cancellation);
        row.DisplayStatus.ShouldBe(MyLeaveRequestStatus.Approved);
        row.CanAmend.ShouldBeFalse();
        row.CanRequestCancellation.ShouldBeFalse();
        row.CanReturn.ShouldBeFalse();
    }

    [Fact]
    public async Task The_List_Should_Show_A_Rejected_Cancellation_As_Cancel_Leave_Rejected()
    {
        var meId = CreateCurrentUserEmployee("CAN180");
        var approverId = EnableLeaveWorkflowWithApprover("CAN180APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var leaveId = await CreateApprovedLeaveAsync(typeId, DateTime.Today.AddDays(30), days: 5, approverId);

        var requestId = await _myLeaveAppService.RequestCancellationOfMyLeave(new RequestLeaveCancellationInput { Id = leaveId });
        await _approvalService.RejectAsync(requestId, approverId, "Needed on those days");

        var row = await ListRowAsync(leaveId);
        row.RequestAction.ShouldBe(WorkflowRequestAction.Cancellation);
        row.DisplayStatus.ShouldBe(MyLeaveRequestStatus.Rejected);

        // The leave itself stands, so it can be changed again.
        row.CanRequestCancellation.ShouldBeTrue();
    }

    [Fact]
    public async Task The_List_Should_Go_Back_To_The_Leave_When_A_Cancellation_Is_Withdrawn()
    {
        var meId = CreateCurrentUserEmployee("CAN190");
        var approverId = EnableLeaveWorkflowWithApprover("CAN190APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var leaveId = await CreateApprovedLeaveAsync(typeId, DateTime.Today.AddDays(30), days: 5, approverId);

        await _myLeaveAppService.RequestCancellationOfMyLeave(new RequestLeaveCancellationInput { Id = leaveId });
        await _myLeaveAppService.WithdrawMyLeaveChange(new WithdrawMyLeaveChangeInput { Id = leaveId });

        var row = await ListRowAsync(leaveId);
        row.RequestAction.ShouldBe(WorkflowRequestAction.New);
        row.DisplayStatus.ShouldBe(MyLeaveRequestStatus.Approved);
    }

    [Fact]
    public async Task Exporting_My_Leaves_Should_Produce_A_Workbook_With_A_Line_Per_Request()
    {
        var meId = CreateCurrentUserEmployee("CAN195");
        var approverId = EnableLeaveWorkflowWithApprover("CAN195APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var leaveId = await CreateApprovedLeaveAsync(typeId, DateTime.Today.AddDays(30), days: 5, approverId);
        await _myLeaveAppService.RequestCancellationOfMyLeave(new RequestLeaveCancellationInput { Id = leaveId });

        var file = await _myLeaveAppService.GetMyLeavesToExcel(new ExportMyLeavesInput());

        file.FileName.ShouldEndWith(".xlsx");

        var bytes = Resolve<HCMv3.Storage.ITempFileCacheManager>().GetFile(file.FileToken);
        using var book = new ClosedXML.Excel.XLWorkbook(new System.IO.MemoryStream(bytes));

        // Title, blank, headings, then the leave and its cancellation.
        book.Worksheets.Single().LastRowUsed().RowNumber().ShouldBe(5);
    }

    [Fact]
    public async Task Exporting_My_Leaves_Should_Keep_To_The_Leaves_The_Screen_Is_Showing()
    {
        var meId = CreateCurrentUserEmployee("CAN196");
        var approverId = EnableLeaveWorkflowWithApprover("CAN196APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var first = await CreateApprovedLeaveAsync(typeId, DateTime.Today.AddDays(30), days: 2, approverId);
        await CreateApprovedLeaveAsync(typeId, DateTime.Today.AddDays(40), days: 2, approverId);

        var file = await _myLeaveAppService.GetMyLeavesToExcel(new ExportMyLeavesInput { LeaveIds = new System.Collections.Generic.List<int> { first } });

        var bytes = Resolve<HCMv3.Storage.ITempFileCacheManager>().GetFile(file.FileToken);
        using var book = new ClosedXML.Excel.XLWorkbook(new System.IO.MemoryStream(bytes));

        book.Worksheets.Single().LastRowUsed().RowNumber().ShouldBe(4);
    }

    [Fact]
    public async Task A_Leave_Withdrawn_Before_Approval_Should_Stay_In_My_List_As_Withdrawn()
    {
        var meId = CreateCurrentUserEmployee("CAN200");
        EnableLeaveWorkflowWithApprover("CAN200APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var applied = await _myLeaveAppService.ApplyForLeave(new ApplyForLeaveInput
        {
            AbsenceTypeId = typeId,
            StartDate = DateTime.Today.AddDays(30),
            EndDate = DateTime.Today.AddDays(34)
        });

        await _myLeaveAppService.CancelMyLeave(new Abp.Application.Services.Dto.EntityDto(applied.EmployeeAbsenceId));

        var list = await _myLeaveAppService.GetMyLeaves(new GetMyLeavesInput { IncludeHistory = true, IncludeWithdrawn = true });
        var row = list.Items.Single(l => l.Id == applied.EmployeeAbsenceId);

        // Still there as a record of what was asked for, under its number...
        row.IsWithdrawn.ShouldBeTrue();
        row.DisplayStatus.ShouldBe(MyLeaveRequestStatus.Withdrawn);
        row.RequestNumber.ShouldNotBeNullOrWhiteSpace();
        row.Requests.Single().Outcome.ShouldBe(MyLeaveHistoryOutcome.Withdrawn);

        // ...and finished with: nothing can be done to it.
        row.CanCancel.ShouldBeFalse();
        row.CanAmend.ShouldBeFalse();
        row.CanRequestCancellation.ShouldBeFalse();
        row.CanReturn.ShouldBeFalse();
        row.IsDraft.ShouldBeFalse();
        row.IsSentBack.ShouldBeFalse();

        // It holds no days: the balance is as if it had never been applied for.
        QuotaRow(meId, typeId).QuotaUsed.ShouldBe(0);
    }

    [Fact]
    public async Task A_Withdrawn_Leave_Should_Not_Be_Listed_Unless_Asked_For()
    {
        // Everything else that reads this list -- the assistant, the mobile app -- means leave that exists.
        var meId = CreateCurrentUserEmployee("CAN201");
        EnableLeaveWorkflowWithApprover("CAN201APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var applied = await _myLeaveAppService.ApplyForLeave(new ApplyForLeaveInput
        {
            AbsenceTypeId = typeId,
            StartDate = DateTime.Today.AddDays(30),
            EndDate = DateTime.Today.AddDays(34)
        });

        await _myLeaveAppService.CancelMyLeave(new Abp.Application.Services.Dto.EntityDto(applied.EmployeeAbsenceId));

        var list = await _myLeaveAppService.GetMyLeaves(new GetMyLeavesInput { IncludeHistory = true });

        list.Items.ShouldNotContain(l => l.Id == applied.EmployeeAbsenceId);
    }

    [Fact]
    public async Task A_Draft_Discarded_Before_It_Was_Sent_Should_Not_Come_Back_As_Withdrawn()
    {
        // Nobody else ever saw it; it was never a request.
        var meId = CreateCurrentUserEmployee("CAN202");
        EnableLeaveWorkflowWithApprover("CAN202APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var draft = await _myLeaveAppService.SaveMyLeaveDraft(new SaveMyLeaveDraftInput
        {
            AbsenceTypeId = typeId,
            StartDate = DateTime.Today.AddDays(30),
            EndDate = DateTime.Today.AddDays(34)
        });

        await _myLeaveAppService.CancelMyLeave(new Abp.Application.Services.Dto.EntityDto(draft.EmployeeAbsenceId));

        var list = await _myLeaveAppService.GetMyLeaves(new GetMyLeavesInput { IncludeHistory = true, IncludeWithdrawn = true });

        list.Items.ShouldNotContain(l => l.Id == draft.EmployeeAbsenceId);
    }

    private async Task<MyLeaveDto> ListRowAsync(int leaveId)
    {
        var list = await _myLeaveAppService.GetMyLeaves(new GetMyLeavesInput { IncludeHistory = true });

        return list.Items.Single(l => l.Id == leaveId);
    }

    [Fact]
    public async Task A_Pending_Leave_Should_Be_Withdrawn_Not_Cancelled_Through_Approval()
    {
        var meId = CreateCurrentUserEmployee("CAN130");
        EnableLeaveWorkflowWithApprover("CAN130APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var applied = await _myLeaveAppService.ApplyForLeave(new ApplyForLeaveInput
        {
            AbsenceTypeId = typeId,
            StartDate = DateTime.Today.AddDays(30),
            EndDate = DateTime.Today.AddDays(34)
        });

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _myLeaveAppService.RequestCancellationOfMyLeave(new RequestLeaveCancellationInput { Id = applied.EmployeeAbsenceId }));
    }

    [Fact]
    public async Task A_Second_Change_Should_Be_Refused_While_A_Cancellation_Is_Pending()
    {
        // One change at a time on a leave: an amendment approved after a cancellation would write onto a
        // deleted row, and a cancellation approved after an amendment would remove what its approver never saw.
        var meId = CreateCurrentUserEmployee("CAN140");
        var approverId = EnableLeaveWorkflowWithApprover("CAN140APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var start = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 5, approverId);

        await _myLeaveAppService.RequestCancellationOfMyLeave(new RequestLeaveCancellationInput { Id = leaveId });

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _myLeaveAppService.ReturnFromMyLeave(new ReturnFromLeaveInput
            {
                EmployeeAbsenceId = leaveId,
                RejoiningDate = start.AddDays(3)
            }));
    }

    [Fact]
    public async Task Cancellation_Should_Be_Refused_For_Someone_Elses_Leave()
    {
        var otherId = CreateEmployee("CAN150OTHER");
        CreateCurrentUserEmployee("CAN150");
        EnableLeaveWorkflowWithApprover("CAN150APP");
        var typeId = CreateAbsenceType();

        var leaveId = UsingDbContext(context =>
        {
            var row = context.Set<EmployeeAbsence>().Add(new EmployeeAbsence
            {
                TenantId = TenantId,
                EmployeeId = otherId,
                AbsenceTypeId = typeId,
                StartDate = DateTime.Today.AddDays(30),
                EndDate = DateTime.Today.AddDays(34),
                TotalLeaveDays = 5,
                WorkflowStatus = RequestStatus.Approved
            }).Entity;
            context.SaveChanges();
            return row.Id;
        });

        await Assert.ThrowsAsync<Abp.Authorization.AbpAuthorizationException>(() =>
            _myLeaveAppService.RequestCancellationOfMyLeave(new RequestLeaveCancellationInput { Id = leaveId }));
    }

    [Fact]
    public async Task My_Leaves_Should_Offer_Cancellation_And_Return_On_An_Approved_Leave()
    {
        var meId = CreateCurrentUserEmployee("CAN160");
        var approverId = EnableLeaveWorkflowWithApprover("CAN160APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var leaveId = await CreateApprovedLeaveAsync(typeId, DateTime.Today.AddDays(30), days: 5, approverId);

        var list = await _myLeaveAppService.GetMyLeaves(new GetMyLeavesInput());
        var dto = list.Items.Single(l => l.Id == leaveId);

        dto.CanRequestCancellation.ShouldBeTrue();
        // Not yet: this leave starts a month from now, and a return is only offered once a leave has begun.
        dto.CanReturn.ShouldBeFalse();
        dto.CanCancel.ShouldBeFalse();

        await _myLeaveAppService.RequestCancellationOfMyLeave(new RequestLeaveCancellationInput { Id = leaveId });

        dto = (await _myLeaveAppService.GetMyLeaves(new GetMyLeavesInput())).Items.Single(l => l.Id == leaveId);
        dto.HasPendingAmendment.ShouldBeTrue();
        dto.CanRequestCancellation.ShouldBeFalse();
        dto.CanReturn.ShouldBeFalse();
    }
}
