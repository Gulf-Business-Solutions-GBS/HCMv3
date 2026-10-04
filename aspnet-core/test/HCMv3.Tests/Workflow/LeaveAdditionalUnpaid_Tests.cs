using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.Ess.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// ADDITIONAL UNPAID LEAVE: an employee asking for more days than their balance covers, and taking the excess
/// unpaid rather than being refused (legacy's <c>ExtAbsenceType</c> fields on the request, booked as their own
/// leave row by <c>SaveAdditionalUnpaidLeave</c> when the request is approved).
///
/// <para>
/// One application, one approval, two leave rows -- which is what makes the return rules interesting: coming
/// back early can land before the unpaid part starts, or in the middle of it.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class LeaveAdditionalUnpaid_Tests : EssLeaveWorkflowTestBase
{
    private int _meId;
    private int _approverId;
    private int _annualId;
    private int _unpaidId;

    private void GivenAnEmployeeWithBalance(string code, double accrued)
    {
        _meId = CreateCurrentUserEmployee(code);
        _approverId = EnableLeaveWorkflowWithApprover(code + "APP");
        _annualId = CreateAbsenceType(t => t.Code = "1", nameEn: "Annual Paid");
        _unpaidId = EnsureUnpaidLeaveType();
        CreateQuota(_meId, _annualId, accrued: accrued);
    }

    private static DateTime Soon => DateTime.Today.AddDays(30);

    private Task<ApplyForLeaveResultDto> ApplyWithSplitAsync(DateTime start, int days) =>
        _myLeaveAppService.ApplyForLeave(new ApplyForLeaveInput
        {
            AbsenceTypeId = _annualId,
            StartDate = start,
            EndDate = start.AddDays(days - 1),
            SplitExcessAsUnpaidLeave = true
        });

    [Fact]
    public async Task The_Preview_Should_Show_Where_The_Paid_Days_Run_Out()
    {
        GivenAnEmployeeWithBalance("EXT100", accrued: 4);

        var preview = await _myLeaveAppService.PreviewMyLeaveSplit(new ApplyForLeaveInput
        {
            AbsenceTypeId = _annualId,
            StartDate = Soon,
            EndDate = Soon.AddDays(9)
        });

        preview.FitsWithinBalance.ShouldBeFalse();
        preview.Bookable.ShouldBe(4);
        preview.PaidEndDate.ShouldBe(Soon.AddDays(3));
        preview.PaidLeaveDays.ShouldBe(4);
        preview.UnpaidStartDate.ShouldBe(Soon.AddDays(4));
        preview.UnpaidEndDate.ShouldBe(Soon.AddDays(9));
        preview.UnpaidLeaveDays.ShouldBe(6);
    }

    [Fact]
    public async Task A_Leave_Within_The_Balance_Needs_No_Split()
    {
        GivenAnEmployeeWithBalance("EXT110", accrued: 30);

        var preview = await _myLeaveAppService.PreviewMyLeaveSplit(new ApplyForLeaveInput
        {
            AbsenceTypeId = _annualId,
            StartDate = Soon,
            EndDate = Soon.AddDays(4)
        });

        preview.FitsWithinBalance.ShouldBeTrue();
        preview.UnpaidStartDate.ShouldBeNull();
    }

    [Fact]
    public async Task Applying_With_A_Split_Should_Shorten_The_Paid_Leave_And_Carry_The_Unpaid_Part()
    {
        GivenAnEmployeeWithBalance("EXT120", accrued: 4);

        var applied = await ApplyWithSplitAsync(Soon, days: 10);

        var leave = LeaveRow(applied.EmployeeAbsenceId);
        leave.EndDate.ShouldBe(Soon.AddDays(3));
        leave.ExtAbsenceTypeId.ShouldBe(_unpaidId);
        leave.ExtStartDate.ShouldBe(Soon.AddDays(4));
        leave.ExtEndDate.ShouldBe(Soon.AddDays(9));

        // Still one row until it is approved: the unpaid leave is part of the same application.
        LeaveRowsOf(_meId).Count.ShouldBe(1);
    }

    [Fact]
    public async Task Approval_Should_Book_The_Unpaid_Remainder_As_Its_Own_Leave()
    {
        GivenAnEmployeeWithBalance("EXT130", accrued: 4);

        var applied = await ApplyWithSplitAsync(Soon, days: 10);
        await _approvalService.ApproveAsync(applied.WorkflowRequestId!.Value, _approverId, "ok");

        var rows = LeaveRowsOf(_meId);
        rows.Count.ShouldBe(2);

        var paid = rows.Single(r => r.AbsenceTypeId == _annualId);
        paid.EndDate.ShouldBe(Soon.AddDays(3));
        paid.TotalLeaveDays.ShouldBe(4);

        var unpaid = rows.Single(r => r.AbsenceTypeId == _unpaidId);
        unpaid.StartDate.ShouldBe(Soon.AddDays(4));
        unpaid.EndDate.ShouldBe(Soon.AddDays(9));
        unpaid.TotalLeaveDays.ShouldBe(6);
        unpaid.WorkflowStatus.ShouldBe(HCMv3.Workflow.RequestStatus.Approved);
        unpaid.WorkflowRequestId.ShouldBe(applied.WorkflowRequestId);

        // Only the paid days touch the balance.
        QuotaRow(_meId, _annualId).QuotaUsed.ShouldBe(4);
    }

    [Fact]
    public async Task Without_The_Split_The_Same_Leave_Is_Refused()
    {
        GivenAnEmployeeWithBalance("EXT140", accrued: 4);

        await Assert.ThrowsAsync<UserFriendlyException>(() => _myLeaveAppService.ApplyForLeave(new ApplyForLeaveInput
        {
            AbsenceTypeId = _annualId,
            StartDate = Soon,
            EndDate = Soon.AddDays(9)
        }));
    }

    [Fact]
    public async Task Cancelling_The_Leave_Should_Take_The_Unpaid_Part_With_It()
    {
        GivenAnEmployeeWithBalance("EXT150", accrued: 4);

        var applied = await ApplyWithSplitAsync(Soon, days: 10);
        await _approvalService.ApproveAsync(applied.WorkflowRequestId!.Value, _approverId, "ok");

        var cancellationId = await _myLeaveAppService.RequestCancellationOfMyLeave(new RequestLeaveCancellationInput { Id = applied.EmployeeAbsenceId });
        await _approvalService.ApproveAsync(cancellationId, _approverId, "ok");

        // Both rows are kept as history and both are out of payroll's sight.
        LeaveRowsOf(_meId).ShouldAllBe(a => a.WorkflowStatus == RequestStatus.Cancelled);
        QuotaRow(_meId, _annualId).QuotaUsed.ShouldBe(0);
    }

    [Fact]
    public async Task Returning_Before_The_Unpaid_Part_Starts_Should_Remove_It()
    {
        // Legacy EarlyJoinType.UpdateMainLeaveDeleteAdditional.
        GivenAnEmployeeWithBalance("EXT160", accrued: 4);

        var applied = await ApplyWithSplitAsync(Soon, days: 10);
        await _approvalService.ApproveAsync(applied.WorkflowRequestId!.Value, _approverId, "ok");

        // Back on the third day: before the unpaid part (which starts on Soon+4) begins.
        var result = await _myLeaveAppService.ReturnFromMyLeave(new ReturnFromLeaveInput
        {
            EmployeeAbsenceId = applied.EmployeeAbsenceId,
            RejoiningDate = Soon.AddDays(2)
        });

        result.Kind.ShouldBe(LeaveReturnKind.Early);
        await _approvalService.ApproveAsync(result.WorkflowRequestId, _approverId, "ok");

        var rows = LeaveRowsOf(_meId);
        rows.Count.ShouldBe(1);
        rows.Single().AbsenceTypeId.ShouldBe(_annualId);
        rows.Single().EndDate.ShouldBe(Soon.AddDays(1));

        QuotaRow(_meId, _annualId).QuotaUsed.ShouldBe(2);
    }

    [Fact]
    public async Task Returning_During_The_Unpaid_Part_Should_Cut_It_Short_And_Leave_The_Paid_Days_Alone()
    {
        // Legacy EarlyJoinType.UpdateAdditional.
        GivenAnEmployeeWithBalance("EXT170", accrued: 4);

        var applied = await ApplyWithSplitAsync(Soon, days: 10);
        await _approvalService.ApproveAsync(applied.WorkflowRequestId!.Value, _approverId, "ok");

        // Back on Soon+7, in the middle of the unpaid part (Soon+4 to Soon+9).
        var result = await _myLeaveAppService.ReturnFromMyLeave(new ReturnFromLeaveInput
        {
            EmployeeAbsenceId = applied.EmployeeAbsenceId,
            RejoiningDate = Soon.AddDays(7)
        });

        await _approvalService.ApproveAsync(result.WorkflowRequestId, _approverId, "ok");

        var rows = LeaveRowsOf(_meId);
        rows.Count.ShouldBe(2);

        var paid = rows.Single(r => r.AbsenceTypeId == _annualId);
        paid.EndDate.ShouldBe(Soon.AddDays(3));

        var unpaid = rows.Single(r => r.AbsenceTypeId == _unpaidId);
        unpaid.EndDate.ShouldBe(Soon.AddDays(6));
        unpaid.ReturnDate.ShouldBe(Soon.AddDays(7));
        unpaid.IsReturned.ShouldBeTrue();

        // The paid days are untouched, so the balance is unchanged.
        QuotaRow(_meId, _annualId).QuotaUsed.ShouldBe(4);
    }

    [Fact]
    public async Task A_Late_Return_Should_Be_Measured_From_The_End_Of_The_Unpaid_Part()
    {
        // Legacy DaysDifference uses ExtEndDate when there is additional leave.
        GivenAnEmployeeWithBalance("EXT180", accrued: 4);

        var applied = await ApplyWithSplitAsync(Soon, days: 10);
        await _approvalService.ApproveAsync(applied.WorkflowRequestId!.Value, _approverId, "ok");

        // The unpaid part ends on Soon+9, so coming back on Soon+12 is two days late, not eight.
        var preview = await _myLeaveAppService.PreviewReturnFromMyLeave(new ReturnFromLeaveInput
        {
            EmployeeAbsenceId = applied.EmployeeAbsenceId,
            RejoiningDate = Soon.AddDays(12)
        });

        preview.Kind.ShouldBe(LeaveReturnKind.Late);
        preview.DaysDifference.ShouldBe(2);
    }
}
