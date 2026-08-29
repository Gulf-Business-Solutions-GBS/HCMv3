using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;

namespace HCMv3.PersonnelAdministration.Workflow;

/// <summary>
/// Workflow handler for leave (<see cref="EmployeeAbsence"/>) -- the reference implementation the other
/// transaction types are modelled on.
///
/// <para>
/// Note where this file lives: in the PersonnelAdministration module, not the Workflow module. That is the
/// point of the handler contract. Leave rules belong to the team that owns leave, and the approval engine
/// stays type-agnostic. Legacy had the inverse arrangement -- <c>RequestProcessApproval</c> carried a branch
/// for every one of ~30 request types, so any new transaction meant editing the engine.
/// </para>
///
/// <para>
/// The status bookkeeping (moving <c>EmployeeAbsence.WorkflowStatus</c> through Open, Approved, Rejected,
/// Draft) is inherited from <see cref="WorkflowTransactionHandlerBase{TEntity}"/>. What is specific to leave
/// is the overlap check and the inbox label.
/// </para>
///
/// <para>
/// <b>Pending leave does not reserve quota -- it reserves BOOKABLE.</b> <c>QuotaUsed</c> is touched only on
/// approval, so payroll, accrual and every report keep seeing exactly what they saw before workflow existed.
/// What a pending request does consume is the employee's ability to book those days again, and that is a
/// computed number (<see cref="ILeaveBalanceService"/>), not a stored one. The consequence is that there is
/// no reservation to leak when a request is abandoned and nothing to reconcile: the pending row IS the
/// reservation, which only works because it lives in <c>EmployeeAbsences</c> next to the approved leave.
/// </para>
/// </summary>
public class LeaveWorkflowTransactionHandler : WorkflowTransactionHandlerBase<EmployeeAbsence>
{
    /// <summary>Must match the <c>WorkflowTransactionType.Code</c> seeded for leave.</summary>
    public const string LeaveRequestType = "EmployeeAbsence";

    private readonly IRepository<AbsenceType> _absenceTypeRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly ILeaveBalanceService _leaveBalanceService;

    public LeaveWorkflowTransactionHandler(
        IRepository<EmployeeAbsence> repository,
        IRepository<AbsenceType> absenceTypeRepository,
        IUnitOfWorkManager unitOfWorkManager,
        ILeaveBalanceService leaveBalanceService)
        : base(repository, unitOfWorkManager)
    {
        _absenceTypeRepository = absenceTypeRepository;
        _unitOfWorkManager = unitOfWorkManager;
        _leaveBalanceService = leaveBalanceService;
    }

    public override string RequestType => LeaveRequestType;

    /// <summary>
    /// Refuses a leave that overlaps one the employee already has.
    ///
    /// <para>
    /// Pending leave counts as blocking, not just approved leave. That is the deliberate reading: if only
    /// approved rows blocked, an employee could submit the same week three times over and every one would
    /// pass validation, because the earlier submissions are invisible under the <c>ApprovedOnly</c> filter.
    /// The filter is therefore disabled for this check -- one of the few places that genuinely needs to see
    /// in-flight rows. Rejected and sent-back rows do not block, since neither represents a claim on the
    /// employee's time.
    /// </para>
    /// </summary>
    public override async Task ValidateBeforeSubmitAsync(WorkflowTransactionContext context)
    {
        var leave = await GetTargetAsync(context);
        if (leave == null)
        {
            return;
        }

        if (leave.EndDate.Date < leave.StartDate.Date)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var overlapping = Repository.GetAll()
                .Where(a => a.Id != leave.Id
                            && a.EmployeeId == leave.EmployeeId
                            && a.StartDate <= leave.EndDate
                            && a.EndDate >= leave.StartDate
                            && (a.WorkflowStatus == null
                                || a.WorkflowStatus == RequestStatus.Approved
                                || a.WorkflowStatus == RequestStatus.Closed
                                || a.WorkflowStatus == RequestStatus.Open))
                .Select(a => (int?)a.Id)
                .FirstOrDefault();

            if (overlapping.HasValue)
            {
                throw new UserFriendlyException(L("OverlappingLeaveRequestExists"));
            }
        }

        await EnsureWithinBookableBalanceAsync(leave);
    }

    /// <summary>
    /// Refuses a request for more days than the employee can still book.
    ///
    /// <para>
    /// Checked against BOOKABLE, not against the quota balance. The quota balance ignores leave already
    /// awaiting approval, so an employee with ten days left could submit three separate week-long requests
    /// and each would pass on its own. Bookable subtracts what is already in flight, so the second
    /// application sees the first.
    /// </para>
    /// <para>
    /// Skipped when the employee holds no quota row for the type at all: some absence types (unpaid,
    /// compassionate) are deliberately not quota-controlled, and refusing those would block legitimate
    /// requests rather than catch a mistake.
    /// </para>
    /// </summary>
    private async Task EnsureWithinBookableBalanceAsync(EmployeeAbsence leave)
    {
        var balances = await _leaveBalanceService.GetBalancesAsync(leave.EmployeeId, leave.StartDate);

        var balance = balances.FirstOrDefault(b => b.AbsenceTypeId == leave.AbsenceTypeId);
        if (balance == null)
        {
            return;
        }

        // The row being submitted is itself still a Draft, so it is not in Pending yet and does not need
        // excluding from the comparison.
        var requestedDays = leave.TotalLeaveDays > 0
            ? leave.TotalLeaveDays
            : (leave.EndDate.Date - leave.StartDate.Date).TotalDays + 1;

        if (requestedDays > balance.Bookable)
        {
            throw new UserFriendlyException(
                L("NotEnoughBookableLeaveBalance", $"{balance.Bookable:0.##}", $"{requestedDays:0.##}"));
        }
    }

    /// <summary>
    /// The one-line inbox label, e.g. "Annual Leave, 5 day(s), 01 Jan 2026 - 05 Jan 2026". Written once at
    /// submit so the approver inbox never has to reach into this table to render a list.
    /// </summary>
    public override async Task<WorkflowSummary> BuildSummaryAsync(WorkflowTransactionContext context)
    {
        var leave = await GetTargetAsync(context);
        if (leave == null)
        {
            return null;
        }

        var absenceType = _absenceTypeRepository.GetAll()
            .FirstOrDefault(t => t.Id == leave.AbsenceTypeId);

        var days = leave.TotalLeaveDays > 0
            ? leave.TotalLeaveDays
            : (leave.EndDate.Date - leave.StartDate.Date).TotalDays + 1;

        var range = leave.StartDate.ToString("dd MMM yyyy") + " - " + leave.EndDate.ToString("dd MMM yyyy");

        return new WorkflowSummary(
            $"{absenceType?.NameEn ?? "Leave"}, {days:0.##} day(s), {range}",
            $"{absenceType?.NameAr ?? "إجازة"}، {days:0.##} يوم، {range}");
    }
}
