using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Ess.Dto;

namespace HCMv3.Ess;

/// <summary>
/// Self-service leave: an employee's own balances, their own requests, and applying for leave.
///
/// <para>
/// Deliberately NOT the admin <c>IEmployeeAbsencesAppService</c> with a filter bolted on. That service must
/// let HR pick any employee, backdate, and bypass approval; this one must do none of those. Reusing it and
/// passing a scope would mean the same endpoint enforcing two different rule sets -- which is how the
/// legacy ESS project ended up as a parallel implementation in the first place. Both sit on the same entity
/// and the same domain services; only the surface differs.
/// </para>
/// </summary>
public interface IMyLeaveAppService : IApplicationService
{
    /// <summary>Balances for every type the employee holds quota for, including what is bookable.</summary>
    Task<List<MyLeaveBalanceDto>> GetMyBalances();

    /// <summary>The tenant settings that shape the leave form; see <see cref="LeaveFormSettingsDto"/>.</summary>
    Task<LeaveFormSettingsDto> GetLeaveFormSettings();

    Task<PagedResultDto<MyLeaveDto>> GetMyLeaves(GetMyLeavesInput input);

    /// <summary>
    /// Creates the leave and, when the type is workflow-enabled, submits it for approval in one step. The
    /// employee never sees a draft they have to remember to submit.
    /// </summary>
    Task<ApplyForLeaveResultDto> ApplyForLeave(ApplyForLeaveInput input);

    /// <summary>
    /// Shows how a request longer than the balance would be split into paid and unpaid leave, before anything is
    /// created. The split itself is worked out by the server.
    /// </summary>
    Task<LeaveSplitPreviewDto> PreviewMyLeaveSplit(ApplyForLeaveInput input);

    /// <summary>How a sick leave will be paid, slab by slab. Empty for any other absence type.</summary>
    Task<List<SickLeaveDeductionSegmentDto>> PreviewMySickLeaveDeduction(ApplyForLeaveInput input);

    /// <summary>Withdraws a request the employee raised, while it is still theirs to withdraw.</summary>
    Task CancelMyLeave(EntityDto input);

    /// <summary>
    /// Asks for a change to a leave that has already been approved, routing it through the same approval
    /// chain the original went through.
    ///
    /// <para>
    /// This is the path that exists so nobody needs the other one. Without it, changing an approved leave
    /// means editing the row directly -- which takes effect immediately, with no approval and no record that
    /// anyone agreed. The approved leave stays exactly as it is until this request is granted.
    /// </para>
    /// </summary>
    /// <returns>Id of the amendment's <c>WorkflowRequest</c>.</returns>
    Task<int> AmendMyLeave(AmendMyLeaveInput input);

    /// <summary>
    /// Reports the day the employee actually came back, and raises whatever that implies.
    ///
    /// <para>
    /// Coming back ON TIME stamps the return; coming back EARLY additionally pulls the end date in and keeps
    /// the original as <c>InitialEndDate</c>. Coming back LATE (legacy parity) books the gap on approval as
    /// further paid leave of the same type and/or unpaid leave, split as the employee enters it or as
    /// <see cref="PreviewReturnFromMyLeave"/> proposes. All three go out as an amendment on the leave, through
    /// the same approval chain, and nothing changes until it is granted.
    /// </para>
    /// </summary>
    Task<ReturnFromLeaveResultDto> ReturnFromMyLeave(ReturnFromLeaveInput input);

    /// <summary>
    /// What returning on the given date would mean -- early, on time or late, and for late the proposed
    /// paid/unpaid split -- without raising anything.
    /// </summary>
    Task<ReturnFromLeavePreviewDto> PreviewReturnFromMyLeave(ReturnFromLeaveInput input);

    /// <summary>
    /// Asks for an APPROVED leave to be cancelled (legacy <c>ActionType.Cancellation</c>). The leave stays
    /// approved and visible to payroll until the cancellation is granted; then it is deleted and its days go
    /// back to the quota. For a leave still awaiting approval use <see cref="CancelMyLeave"/>, which withdraws
    /// it immediately.
    /// </summary>
    /// <returns>Id of the cancellation's <c>WorkflowRequest</c>.</returns>
    Task<int> RequestCancellationOfMyLeave(RequestLeaveCancellationInput input);

    /// <summary>
    /// The leave types this employee may apply for: active, offered through self-service, and matching their
    /// gender, religion and salary profile (legacy <c>AbsenceRequest.EmpAbsenceType</c>). Each carries the
    /// settings that decide which fields the form shows.
    /// </summary>
    Task<List<MyLeaveTypeDto>> GetMyLeaveTypes();

    /// <summary>
    /// The current balance, the bookable balance and the balance after the leave being entered, for a type
    /// with a quota. Nothing is saved.
    /// </summary>
    Task<LeaveBalancePreviewDto> PreviewMyLeaveBalance(PreviewLeaveBalanceInput input);

    /// <summary>
    /// Sends a NEW leave that an approver sent back round again, with the values the employee corrected. It
    /// keeps its request number, log and comments.
    /// </summary>
    Task<ApplyForLeaveResultDto> ResubmitMyLeave(ResubmitMyLeaveInput input);

    /// <summary>
    /// Sends a change, cancellation or return that was sent back round again, corrected. The approved leave is
    /// still untouched.
    /// </summary>
    /// <returns>Id of the request that was resubmitted.</returns>
    Task<int> ResubmitMyLeaveChange(ResubmitMyLeaveChangeInput input);

    /// <summary>
    /// Takes back a change, cancellation or return that has not been decided yet -- still with an approver, or
    /// sent back. The approved leave stands exactly as it was and can be acted on again at once.
    /// </summary>
    Task WithdrawMyLeaveChange(WithdrawMyLeaveChangeInput input);

    /// <summary>
    /// Every request raised against one leave, oldest first: the application, and each change, cancellation
    /// and return since, with what was asked and how it ended -- including the ones that were rejected or
    /// withdrawn, which leave no trace on the leave itself.
    /// </summary>
    Task<List<MyLeaveHistoryItemDto>> GetMyLeaveHistory(EntityDto input);
}
