using System;
using System.Linq;
using System.Text.Json;
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
/// APPROVAL (see <c>OnApprovedAsync</c>), so payroll, accrual and every report keep seeing exactly what they
/// saw before workflow existed.
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
    private readonly IRepository<EmployeeAbsenceQuota> _quotaRepository;

    public LeaveWorkflowTransactionHandler(
        IRepository<EmployeeAbsence> repository,
        IRepository<AbsenceType> absenceTypeRepository,
        IUnitOfWorkManager unitOfWorkManager,
        ILeaveBalanceService leaveBalanceService,
        IRepository<EmployeeAbsenceQuota> quotaRepository)
        : base(repository, unitOfWorkManager)
    {
        _absenceTypeRepository = absenceTypeRepository;
        _unitOfWorkManager = unitOfWorkManager;
        _leaveBalanceService = leaveBalanceService;
        _quotaRepository = quotaRepository;
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

        // For an amendment the row still holds the OLD values -- the proposed ones live on the request until
        // approval. Validating the row would check dates nobody is asking for and pass an amendment that is
        // in fact impossible.
        var proposed = context.IsAmendment ? ReadAmendment(context) : null;

        var absenceTypeId = proposed?.AbsenceTypeId ?? leave.AbsenceTypeId;
        var startDate = proposed?.StartDate ?? leave.StartDate;
        var endDate = proposed?.EndDate ?? leave.EndDate;
        var totalDays = proposed?.TotalLeaveDays ?? leave.TotalLeaveDays;

        // An approved leave being amended already sits in the quota's Used, so its own days have to be
        // credited back before comparing -- otherwise shortening a leave from 5 days to 3 could be refused
        // for insufficient balance.
        var daysAlreadyCounted = context.IsAmendment && leave.AbsenceTypeId == absenceTypeId
            ? DaysOf(leave.TotalLeaveDays, leave.StartDate, leave.EndDate)
            : 0;

        await ValidateLeaveValuesAsync(
            leave.EmployeeId,
            absenceTypeId,
            startDate,
            endDate,
            totalDays,
            excludeLeaveId: leave.Id,
            daysAlreadyCounted: daysAlreadyCounted);
    }

    /// <inheritdoc/>
    public override bool SupportsAmendment => true;

    /// <inheritdoc/>
    public override async Task<string> BuildSnapshotAsync(WorkflowTransactionContext context)
    {
        var leave = await GetTargetAsync(context);
        if (leave == null)
        {
            return null;
        }

        return JsonSerializer.Serialize(new LeaveAmendment
        {
            AbsenceTypeId = leave.AbsenceTypeId,
            StartDate = leave.StartDate,
            EndDate = leave.EndDate,
            TotalLeaveDays = leave.TotalLeaveDays,
            Comments = leave.Comments
        });
    }

    /// <summary>
    /// Writes an approved amendment onto the live leave row.
    ///
    /// <para>
    /// Re-validated here, not only at submit. Between raising the change and approving it the employee may
    /// have booked other leave, or their quota may have been adjusted, so the approver could otherwise be
    /// granting something that is no longer possible. Throwing rolls the approval back with it.
    /// </para>
    /// </summary>
    public override async Task ApplyAmendmentAsync(WorkflowTransactionContext context)
    {
        var leave = await GetRequiredTargetAsync(context);
        var proposed = ReadAmendment(context);

        var previousDays = DaysOf(leave.TotalLeaveDays, leave.StartDate, leave.EndDate);
        var previousAbsenceTypeId = leave.AbsenceTypeId;

        var daysAlreadyCounted = leave.AbsenceTypeId == proposed.AbsenceTypeId
            ? previousDays
            : 0;

        await ValidateLeaveValuesAsync(
            leave.EmployeeId,
            proposed.AbsenceTypeId,
            proposed.StartDate,
            proposed.EndDate,
            proposed.TotalLeaveDays,
            excludeLeaveId: leave.Id,
            daysAlreadyCounted: daysAlreadyCounted);

        leave.AbsenceTypeId = proposed.AbsenceTypeId;
        leave.StartDate = proposed.StartDate;
        leave.EndDate = proposed.EndDate;
        leave.TotalLeaveDays = proposed.TotalLeaveDays;
        leave.Comments = proposed.Comments;

        // Status is untouched on purpose: the leave was approved before the amendment and stays approved.
        // The filter must not so much as blink, or the leave would vanish from payroll mid-change.
        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            await Repository.UpdateAsync(leave);
        }

        // The quota already carries the ORIGINAL days, because this leave was approved before it was
        // amended. Give those back and take the new figure, rather than adding the difference -- a change of
        // absence type moves days between two different quota rows, and a single delta cannot express that.
        await ReleaseQuotaAsync(leave.EmployeeId, previousAbsenceTypeId, previousDays, leave.StartDate);
        await ConsumeQuotaAsync(leave.EmployeeId, leave.AbsenceTypeId, proposed.TotalLeaveDays, leave.StartDate);
    }

    /// <summary>
    /// Takes the approved days out of the employee's quota. This is the moment leave stops being a claim on
    /// the balance and becomes a deduction from it.
    ///
    /// <para>
    /// Runs ONLY on final approval, inside the approving unit of work. If anything downstream throws, the
    /// deduction rolls back with the approval -- leave recorded as approved with the balance untouched, or
    /// the other way round, would be a discrepancy nobody could explain from the screens.
    /// </para>
    /// <para>
    /// Silent when the employee holds no quota row for the type on that date. Some absence types (unpaid,
    /// compassionate) are deliberately not quota-controlled, and inventing a row to deduct from would create
    /// an entitlement that HR never granted.
    /// </para>
    /// </summary>
    private async Task ConsumeQuotaAsync(int employeeId, int absenceTypeId, double days, DateTime asOf)
    {
        if (days <= 0)
        {
            return;
        }

        var quota = FindQuota(employeeId, absenceTypeId, asOf);
        if (quota == null)
        {
            return;
        }

        quota.QuotaUsed = Math.Round(quota.QuotaUsed + days, 6);

        await _quotaRepository.UpdateAsync(quota);
    }

    /// <summary>
    /// Puts days back, for when an amendment replaces what was previously approved.
    ///
    /// <para>
    /// Floored at zero. A negative QuotaUsed would read as extra entitlement the employee never had, and if
    /// the stored figure has drifted for any reason the safe direction to fail is "no free days".
    /// </para>
    /// </summary>
    private async Task ReleaseQuotaAsync(int employeeId, int absenceTypeId, double days, DateTime asOf)
    {
        if (days <= 0)
        {
            return;
        }

        var quota = FindQuota(employeeId, absenceTypeId, asOf);
        if (quota == null)
        {
            return;
        }

        quota.QuotaUsed = Math.Round(Math.Max(0, quota.QuotaUsed - days), 6);

        await _quotaRepository.UpdateAsync(quota);
    }

    /// <summary>
    /// The quota row whose entitlement period covers the leave's start date -- the same row
    /// <see cref="ILeaveBalanceService"/> reads, so the balance shown and the balance deducted cannot
    /// disagree about which year they mean.
    /// </summary>
    private EmployeeAbsenceQuota FindQuota(int employeeId, int absenceTypeId, DateTime asOf)
    {
        var effectiveDate = asOf.Date;

        return _quotaRepository.GetAll()
            .Where(q => q.EmployeeId == employeeId
                        && q.AbsenceTypeId == absenceTypeId
                        && q.EntitlementFrom <= effectiveDate
                        && q.EntitlementTo >= effectiveDate)
            .OrderBy(q => q.EntitlementFrom)
            .FirstOrDefault();
    }

    private static LeaveAmendment ReadAmendment(WorkflowTransactionContext context)
    {
        if (string.IsNullOrWhiteSpace(context.AmendmentJson))
        {
            throw new UserFriendlyException("This amendment carries no proposed values.");
        }

        return JsonSerializer.Deserialize<LeaveAmendment>(context.AmendmentJson);
    }

    private static double DaysOf(double totalDays, DateTime start, DateTime end)
    {
        return totalDays > 0 ? totalDays : (end.Date - start.Date).TotalDays + 1;
    }

    /// <summary>
    /// The leave rules, applied to a set of values rather than to a row -- so the same checks serve a new
    /// request (values from the row) and an amendment (values from the request).
    /// </summary>
    private async Task ValidateLeaveValuesAsync(
        int employeeId,
        int absenceTypeId,
        DateTime startDate,
        DateTime endDate,
        double totalDays,
        int excludeLeaveId,
        double daysAlreadyCounted)
    {
        if (endDate.Date < startDate.Date)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var overlapping = Repository.GetAll()
                .Where(a => a.Id != excludeLeaveId
                            && a.EmployeeId == employeeId
                            && a.StartDate <= endDate
                            && a.EndDate >= startDate
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

        await EnsureWithinBookableBalanceAsync(
            employeeId, absenceTypeId, startDate, endDate, totalDays, daysAlreadyCounted);
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
    private async Task EnsureWithinBookableBalanceAsync(
        int employeeId,
        int absenceTypeId,
        DateTime startDate,
        DateTime endDate,
        double totalDays,
        double daysAlreadyCounted)
    {
        var balances = await _leaveBalanceService.GetBalancesAsync(employeeId, startDate);

        var balance = balances.FirstOrDefault(b => b.AbsenceTypeId == absenceTypeId);
        if (balance == null)
        {
            return;
        }

        // The row being submitted is itself still a Draft, so it is not in Pending yet and does not need
        // excluding from the comparison. An amendment is the exception: its row is already approved and
        // therefore already in Used, so those days are credited back through daysAlreadyCounted.
        var requestedDays = DaysOf(totalDays, startDate, endDate);
        var available = balance.Bookable + daysAlreadyCounted;

        if (requestedDays > available)
        {
            throw new UserFriendlyException(
                L("NotEnoughBookableLeaveBalance", $"{available:0.##}", $"{requestedDays:0.##}"));
        }
    }

    /// <summary>
    /// Applies the approval, then takes the days out of the quota.
    ///
    /// <para>
    /// The base class stamps the row Approved, which is what makes it visible to payroll again; this adds the
    /// deduction. Both happen in the approving unit of work, so the row and the balance can never disagree.
    /// </para>
    /// <para>
    /// Returns early for an AMENDMENT. The base routes those into <see cref="ApplyAmendmentAsync"/>, which
    /// already does its own release-and-reconsume -- deducting again here would take the days twice, so an
    /// employee extending 3 days to 7 would lose 14. Caught by the tests rather than by review, because the
    /// dispatch happens in the base class and is invisible from this method.
    /// </para>
    /// </summary>
    public override async Task OnApprovedAsync(WorkflowTransactionContext context)
    {
        await base.OnApprovedAsync(context);

        if (context.IsAmendment)
        {
            return;
        }

        var leave = await GetTargetAsync(context);
        if (leave == null)
        {
            return;
        }

        var days = DaysOf(leave.TotalLeaveDays, leave.StartDate, leave.EndDate);

        await ConsumeQuotaAsync(leave.EmployeeId, leave.AbsenceTypeId, days, leave.StartDate);
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

        if (context.IsAmendment)
        {
            // An amendment reads as a change, not as a request. "5 days becomes 3" is a different decision
            // from "3 days", and an approver shown only the latter is deciding on the wrong question.
            var proposed = ReadAmendment(context);

            return new WorkflowSummary(
                "Change: " + DescribeLeave(leave.AbsenceTypeId, leave.StartDate, leave.EndDate, leave.TotalLeaveDays, arabic: false) +
                " to " + DescribeLeave(proposed.AbsenceTypeId, proposed.StartDate, proposed.EndDate, proposed.TotalLeaveDays, arabic: false),
                "تعديل: " + DescribeLeave(leave.AbsenceTypeId, leave.StartDate, leave.EndDate, leave.TotalLeaveDays, arabic: true) +
                " إلى " + DescribeLeave(proposed.AbsenceTypeId, proposed.StartDate, proposed.EndDate, proposed.TotalLeaveDays, arabic: true));
        }

        return new WorkflowSummary(
            DescribeLeave(leave.AbsenceTypeId, leave.StartDate, leave.EndDate, leave.TotalLeaveDays, arabic: false),
            DescribeLeave(leave.AbsenceTypeId, leave.StartDate, leave.EndDate, leave.TotalLeaveDays, arabic: true));
    }

    private string DescribeLeave(int absenceTypeId, DateTime startDate, DateTime endDate, double totalDays, bool arabic)
    {
        var absenceType = _absenceTypeRepository.GetAll().FirstOrDefault(t => t.Id == absenceTypeId);
        var days = DaysOf(totalDays, startDate, endDate);
        var range = startDate.ToString("dd MMM yyyy") + " - " + endDate.ToString("dd MMM yyyy");

        return arabic
            ? $"{absenceType?.NameAr ?? "إجازة"}، {days:0.##} يوم، {range}"
            : $"{absenceType?.NameEn ?? "Leave"}, {days:0.##} day(s), {range}";
    }
}
