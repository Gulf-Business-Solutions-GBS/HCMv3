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
        dto.CanReturn.ShouldBeTrue();
        dto.CanCancel.ShouldBeFalse();

        await _myLeaveAppService.RequestCancellationOfMyLeave(new RequestLeaveCancellationInput { Id = leaveId });

        dto = (await _myLeaveAppService.GetMyLeaves(new GetMyLeavesInput())).Items.Single(l => l.Id == leaveId);
        dto.HasPendingAmendment.ShouldBeTrue();
        dto.CanRequestCancellation.ShouldBeFalse();
        dto.CanReturn.ShouldBeFalse();
    }
}
