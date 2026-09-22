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
    private readonly IEmployeeAbsenceCalculationService _absenceCalculationService;

    /// <summary>Legacy hard-codes these absence type codes: "1" annual (paid late days are measured against
    /// it), "2" unpaid (late days beyond the balance are booked as it).</summary>
    public const string AnnualLeaveTypeCode = "1";

    /// <inheritdoc cref="AnnualLeaveTypeCode"/>
    public const string UnpaidLeaveTypeCode = "2";

    public LeaveWorkflowTransactionHandler(
        IRepository<EmployeeAbsence> repository,
        IRepository<AbsenceType> absenceTypeRepository,
        IUnitOfWorkManager unitOfWorkManager,
        ILeaveBalanceService leaveBalanceService,
        IRepository<EmployeeAbsenceQuota> quotaRepository,
        IEmployeeAbsenceCalculationService absenceCalculationService)
        : base(repository, unitOfWorkManager)
    {
        _absenceTypeRepository = absenceTypeRepository;
        _unitOfWorkManager = unitOfWorkManager;
        _leaveBalanceService = leaveBalanceService;
        _quotaRepository = quotaRepository;
        _absenceCalculationService = absenceCalculationService;
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

        // Removing a leave can neither overlap anything nor overdraw a balance. Legacy re-ran its generic
        // checks on the leave's CURRENT dates here, all of which the leave already passed when it was approved.
        if (proposed?.IsCancellation == true)
        {
            return;
        }

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

        if (proposed != null)
        {
            EnsureEarlyReturnIsAllowed(leave, proposed);
        }

        await ValidateLeaveValuesAsync(
            leave.EmployeeId,
            absenceTypeId,
            startDate,
            endDate,
            totalDays,
            excludeLeaveId: leave.Id,
            daysAlreadyCounted: daysAlreadyCounted);

        if (proposed?.IsLateReturn == true)
        {
            await ValidateLateReturnAsync(leave, proposed);
        }
    }

    /// <summary>
    /// The late-return rules, ported from legacy <c>WorkFlowServices.SaveWorkflowRequest</c> (the
    /// <c>ActionType.ReturnFromLeave</c> branch, lines 434-503) plus the quota test legacy ran for every action
    /// (lines 842-895, <c>balance + previous days &gt;= total + DeductFromQuota</c>) and the "leave entry
    /// exists" check legacy ran at approval (<c>AbsenceRequest</c> lines 1488-1511).
    ///
    /// <para>
    /// The two ranges must cover the late gap exactly and in order: paid first, starting the day after the
    /// leave ended; unpaid last, ending the day before the employee rejoined.
    /// </para>
    /// </summary>
    private async Task ValidateLateReturnAsync(EmployeeAbsence leave, LeaveAmendment proposed)
    {
        var returnDate = proposed.ReturnDate!.Value.Date;
        var lastDate = leave.EndDate.Date;

        var daysDifference = await _absenceCalculationService.GetReturnDaysDifferenceAsync(
            leave.EmployeeId, leave.AbsenceTypeId, lastDate, returnDate);

        if (daysDifference <= 0)
        {
            throw new UserFriendlyException(L("ThisReturnIsNotLate"));
        }

        var paidCalendarDays = CalendarDaysOf(proposed.PaidStartDate, proposed.PaidEndDate);
        var unpaidCalendarDays = CalendarDaysOf(proposed.UnpaidStartDate, proposed.UnpaidEndDate);

        if (paidCalendarDays + unpaidCalendarDays != daysDifference)
        {
            throw new UserFriendlyException(L(
                "LateReturnDaysMustEqualTheGap",
                $"{daysDifference:0.##}",
                $"{paidCalendarDays:0.##}",
                $"{unpaidCalendarDays:0.##}"));
        }

        var dayAfterLeave = lastDate.AddDays(1).ToString("dd/MM/yyyy");
        var dayBeforeReturn = returnDate.AddDays(-1).ToString("dd/MM/yyyy");

        if (paidCalendarDays > 0)
        {
            var paidStart = proposed.PaidStartDate!.Value.Date;
            var paidEnd = proposed.PaidEndDate!.Value.Date;

            if (!(lastDate < paidStart && paidStart < returnDate) || !(lastDate < paidEnd && paidEnd < returnDate))
            {
                throw new UserFriendlyException(L("LateReturnPaidDatesMustBeBetweenEndAndReturn"));
            }

            if (paidStart != lastDate.AddDays(1))
            {
                throw new UserFriendlyException(L("LateReturnPaidStartDateShouldBe", dayAfterLeave));
            }

            if (unpaidCalendarDays == 0 && paidEnd != returnDate.AddDays(-1))
            {
                throw new UserFriendlyException(L("LateReturnPaidEndDateShouldBe", dayBeforeReturn));
            }
        }

        if (unpaidCalendarDays > 0)
        {
            var unpaidStart = proposed.UnpaidStartDate!.Value.Date;
            var unpaidEnd = proposed.UnpaidEndDate!.Value.Date;

            if (!(lastDate <= unpaidStart && unpaidStart <= returnDate) || !(lastDate <= unpaidEnd && unpaidEnd <= returnDate))
            {
                throw new UserFriendlyException(L("LateReturnUnpaidDatesMustBeBetweenEndAndReturn"));
            }

            if (unpaidEnd != returnDate.AddDays(-1))
            {
                throw new UserFriendlyException(L("LateReturnUnpaidEndDateShouldBe", dayBeforeReturn));
            }

            if (paidCalendarDays == 0 && unpaidStart != lastDate.AddDays(1))
            {
                throw new UserFriendlyException(L("LateReturnUnpaidStartDateShouldBe", dayAfterLeave));
            }

            if (paidCalendarDays > 0)
            {
                var paidStart = proposed.PaidStartDate!.Value.Date;
                var paidEnd = proposed.PaidEndDate!.Value.Date;

                if ((paidStart <= unpaidStart && unpaidStart <= paidEnd) || (paidStart <= unpaidEnd && unpaidEnd <= paidEnd)
                    || (unpaidStart <= paidStart && paidStart <= unpaidEnd) || (unpaidStart <= paidEnd && paidEnd <= unpaidEnd))
                {
                    throw new UserFriendlyException(L("LateReturnPaidAndUnpaidDatesOverlap"));
                }
            }

            if (FindAbsenceTypeByCode(UnpaidLeaveTypeCode) == null)
            {
                throw new UserFriendlyException(L("UnpaidLeaveTypeNotConfigured", UnpaidLeaveTypeCode));
            }
        }

        EnsureNoLeaveStartsInLateGap(leave, returnDate);

        // The paid days come out of the quota of the leave's own type, so the employee must still be able to
        // book them. The original leave stays approved and stays in Used, so nothing is credited back.
        var deductFromQuota = await PaidLateDaysToDeductAsync(leave, proposed);
        if (deductFromQuota > 0)
        {
            await EnsureWithinBookableBalanceAsync(
                leave.EmployeeId, leave.AbsenceTypeId, proposed.PaidStartDate!.Value, proposed.PaidEndDate!.Value,
                deductFromQuota, daysAlreadyCounted: 0);
        }
    }

    /// <summary>
    /// Legacy "Leave entry exists": another leave starting after this one's end and on or before the
    /// rejoining date means the late gap is not really a gap, and booking it would double-count those days.
    /// </summary>
    private void EnsureNoLeaveStartsInLateGap(EmployeeAbsence leave, DateTime returnDate)
    {
        var endDate = leave.EndDate.Date;
        var rejoin = returnDate.Date.AddDays(1);

        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var other = Repository.GetAll()
                .Where(a => a.Id != leave.Id
                            && a.EmployeeId == leave.EmployeeId
                            && a.StartDate > endDate
                            && a.StartDate < rejoin
                            && (a.WorkflowStatus == null
                                || a.WorkflowStatus == RequestStatus.Approved
                                || a.WorkflowStatus == RequestStatus.Closed
                                || a.WorkflowStatus == RequestStatus.Open))
                .Select(a => new { a.StartDate, a.EndDate })
                .FirstOrDefault();

            if (other != null)
            {
                throw new UserFriendlyException(L(
                    "LeaveEntryExistsInLateReturnGap",
                    other.StartDate.ToString("dd/MM/yyyy"),
                    other.EndDate.ToString("dd/MM/yyyy")));
            }
        }
    }

    /// <summary>
    /// Legacy <c>AbsenceRequest.DeductFromQuota</c>: the paid range's calendar days minus the days the ANNUAL
    /// leave type (code "1") excludes -- legacy measures against annual leave whatever the leave's own type is,
    /// and that is kept. Falls back to the leave's own type when no code-"1" type exists.
    /// </summary>
    private async Task<double> PaidLateDaysToDeductAsync(EmployeeAbsence leave, LeaveAmendment proposed)
    {
        if (!proposed.PaidStartDate.HasValue || !proposed.PaidEndDate.HasValue)
        {
            return 0;
        }

        var start = proposed.PaidStartDate.Value.Date;
        var end = proposed.PaidEndDate.Value.Date;
        var measuringTypeId = FindAbsenceTypeByCode(AnnualLeaveTypeCode)?.Id ?? leave.AbsenceTypeId;

        var excluded = (double)await _absenceCalculationService.ActualLeaveDaysAsync(leave.EmployeeId, start, end, measuringTypeId);

        return (end - start).TotalDays + 1 - excluded;
    }

    /// <summary>Legacy <c>AbsenceRequest.UnpaidDays</c>: the unpaid range less what the unpaid type (code "2") excludes.</summary>
    private async Task<double> UnpaidLateDaysAsync(EmployeeAbsence leave, LeaveAmendment proposed, int unpaidTypeId)
    {
        if (!proposed.UnpaidStartDate.HasValue || !proposed.UnpaidEndDate.HasValue)
        {
            return 0;
        }

        var start = proposed.UnpaidStartDate.Value.Date;
        var end = proposed.UnpaidEndDate.Value.Date;

        var excluded = (double)await _absenceCalculationService.ActualLeaveDaysAsync(leave.EmployeeId, start, end, unpaidTypeId);

        return (end - start).TotalDays + 1 - excluded;
    }

    private static double CalendarDaysOf(DateTime? start, DateTime? end)
    {
        return start.HasValue && end.HasValue ? (end.Value.Date - start.Value.Date).TotalDays + 1 : 0;
    }

    private AbsenceType FindAbsenceTypeByCode(string code)
    {
        return _absenceTypeRepository.GetAll().FirstOrDefault(t => t.Code == code);
    }

    /// <summary>
    /// Refuses an amendment that pulls the leave's end date IN, for an absence type flagged as not returnable
    /// early.
    ///
    /// <para>
    /// Checked for ANY shortening amendment, not only for one raised through the return-from-leave screen.
    /// Some types must be taken in full -- maternity is the usual example -- and a rule enforced only on the
    /// return path would be bypassed by raising a plain amendment instead, which produces exactly the same
    /// result on the row. Extending or moving a leave is untouched by this: the rule is about cutting a leave
    /// short, not about changing when it falls.
    /// </para>
    /// <para>
    /// Judged against the type the leave was GRANTED under, not the proposed one. "This leave must be taken in
    /// full" is a property of what was approved; reading the proposed type instead would let an amendment that
    /// also switches type escape the rule it is subject to.
    /// </para>
    /// </summary>
    private void EnsureEarlyReturnIsAllowed(EmployeeAbsence leave, LeaveAmendment proposed)
    {
        if (proposed.EndDate.Date >= leave.EndDate.Date)
        {
            return;
        }

        var absenceType = _absenceTypeRepository.GetAll().FirstOrDefault(t => t.Id == leave.AbsenceTypeId);
        if (absenceType == null || absenceType.AllowToReturnEarly)
        {
            return;
        }

        throw new UserFriendlyException(L("EarlyReturnNotAllowedForLeaveType", absenceType.NameEn));
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
            Comments = leave.Comments,

            // Captured so a REJECTED return leaves no trace. These three fields are the whole of what a
            // return writes, and without them in the snapshot the record of "what this leave looked like
            // before" would be missing precisely the part the change was about.
            // default(DateTime) is how an unset DateTime column reads on this entity; it is stored as null so
            // the snapshot says "there was no return" rather than "the employee returned in year 1".
            ReturnDate = leave.ReturnDate == default ? (DateTime?)null : leave.ReturnDate,
            IsReturned = leave.IsReturned,
            InitialEndDate = leave.InitialEndDate == default ? (DateTime?)null : leave.InitialEndDate
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

        if (proposed.IsCancellation)
        {
            await ApplyCancellationAsync(leave);
            return;
        }

        if (proposed.IsLateReturn)
        {
            await ApplyLateReturnAsync(context, leave, proposed);
            return;
        }

        var previousDays = DaysOf(leave.TotalLeaveDays, leave.StartDate, leave.EndDate);
        var previousAbsenceTypeId = leave.AbsenceTypeId;

        var daysAlreadyCounted = leave.AbsenceTypeId == proposed.AbsenceTypeId
            ? previousDays
            : 0;

        // Re-checked at approval as well as at submit, for the same reason the balance is: the absence type's
        // configuration may have changed while the request sat in an inbox.
        EnsureEarlyReturnIsAllowed(leave, proposed);

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

        // The return stamp, when this amendment is a return. Written only when the amendment actually carries
        // one: an ordinary amendment leaves these null/false, and treating that as "clear the return" would
        // quietly un-return an employee who is already back, every time they moved an unrelated date.
        if (proposed.ReturnDate.HasValue)
        {
            leave.ReturnDate = proposed.ReturnDate.Value;
        }

        if (proposed.IsReturned)
        {
            leave.IsReturned = true;
        }

        if (proposed.InitialEndDate.HasValue)
        {
            leave.InitialEndDate = proposed.InitialEndDate.Value;
        }

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
    /// Legacy <c>AbsenceRequest</c> approval, <c>ActionType.Cancellation</c> (lines 1822-1884) plus
    /// <c>EmployeeAbsence.OnDeleting</c> (lines 1321-1419): the leave is deleted and its days go back to the
    /// quota. Soft delete, so the approved history is still there to audit.
    ///
    /// <para>
    /// Rows a late return generated earlier are NOT removed with it -- legacy left them too, and they are
    /// separate leave in their own right that HR can remove on their own.
    /// </para>
    /// <para>
    /// Payroll needs no call from here: deleting the row goes through the payroll lock guard and the retro
    /// trigger like any other change to a payroll-sensitive entity, which is what legacy's
    /// <c>ValidateRetroCalculation</c> / <c>EarliestMasterDataChange</c> did in <c>OnDeleting</c>.
    /// </para>
    /// </summary>
    private async Task ApplyCancellationAsync(EmployeeAbsence leave)
    {
        var days = DaysOf(leave.TotalLeaveDays, leave.StartDate, leave.EndDate);

        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            await Repository.DeleteAsync(leave);
        }

        await ReleaseQuotaAsync(leave.EmployeeId, leave.AbsenceTypeId, days, leave.StartDate);
    }

    /// <summary>
    /// Legacy <c>AbsenceRequest</c> approval, <c>ActionType.ReturnFromLeave</c>, late-joining branch (lines
    /// 1485-1643). The original leave is left exactly as approved and only marked returned; the late gap is
    /// booked as up to two NEW approved leave rows in the same table:
    /// <list type="number">
    /// <item><description><b>Paid</b> -- same absence type as the original, <c>PayWithPayroll</c>, charged to
    /// its quota. Starts on the first countable day after the leave (<c>CalcLeaveStartDate</c>) and runs for
    /// <c>DeductFromQuota</c> countable days (<c>CalcLeaveEndDate</c>).</description></item>
    /// <item><description><b>Unpaid</b> -- absence type code "2", <c>PayWithPayroll</c>. Payroll pays nothing
    /// for it because the type is unpaid and the time engine marks those days as unpaid leave.</description></item>
    /// </list>
    ///
    /// <para>
    /// <b>One deliberate difference from legacy.</b> Legacy reset its cursor to the original leave's end before
    /// placing the unpaid row (line 1572), so when BOTH ranges were present the unpaid leave was placed over
    /// the paid one and the tail of the gap was left uncovered. Here the unpaid row starts after the paid row.
    /// With only one of the two ranges -- the usual case -- the result is identical to legacy.
    /// </para>
    /// <para>
    /// The original row's <c>ReturnDate</c> stays empty, as legacy left it: the rejoining date belongs to the
    /// last generated row, the one the employee actually came back from. <c>IsReturned</c> on the original is
    /// what tells payroll the employee is not missing.
    /// </para>
    /// </summary>
    private async Task ApplyLateReturnAsync(WorkflowTransactionContext context, EmployeeAbsence leave, LeaveAmendment proposed)
    {
        // Re-checked at approval, as every other amendment is: leave booked while the request sat in an inbox
        // could otherwise be overwritten by the rows generated here.
        await ValidateLateReturnAsync(leave, proposed);

        var returnDate = proposed.ReturnDate!.Value.Date;
        var cursor = leave.EndDate.Date;

        var deductFromQuota = await PaidLateDaysToDeductAsync(leave, proposed);
        var unpaidType = FindAbsenceTypeByCode(UnpaidLeaveTypeCode);
        var unpaidDays = unpaidType == null ? 0 : await UnpaidLateDaysAsync(leave, proposed, unpaidType.Id);
        var unpaidCalendarDays = CalendarDaysOf(proposed.UnpaidStartDate, proposed.UnpaidEndDate);

        if (deductFromQuota > 0 && CalendarDaysOf(proposed.PaidStartDate, proposed.PaidEndDate) != 0)
        {
            var paid = await InsertLateReturnLeaveAsync(
                context, leave, leave.AbsenceTypeId, cursor, returnDate, deductFromQuota,
                stampReturn: unpaidDays <= 0);

            if (paid != null)
            {
                cursor = paid.EndDate.Date;
            }
        }

        if (unpaidDays > 0)
        {
            await InsertLateReturnLeaveAsync(
                context, leave, unpaidType.Id, cursor, returnDate, unpaidCalendarDays, stampReturn: true);
        }

        leave.IsReturned = true;

        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            await Repository.UpdateAsync(leave);
        }
    }

    /// <summary>
    /// Places one generated late-return leave: first countable day after <paramref name="lastDate"/>, ending
    /// on the <paramref name="dayCount"/>-th countable day. Returns null when legacy would silently skip it
    /// (no start found and fewer than one day asked for); throws when legacy did ("can't generate Leave").
    /// </summary>
    private async Task<EmployeeAbsence> InsertLateReturnLeaveAsync(
        WorkflowTransactionContext context,
        EmployeeAbsence original,
        int absenceTypeId,
        DateTime lastDate,
        DateTime returnDate,
        double dayCount,
        bool stampReturn)
    {
        var startDate = await _absenceCalculationService.CalcLeaveStartDateAsync(original.EmployeeId, absenceTypeId, lastDate, returnDate);
        var endDate = await _absenceCalculationService.CalcLeaveEndDateAsync(original.EmployeeId, absenceTypeId, lastDate, dayCount);

        if (startDate == null || endDate == null)
        {
            if (dayCount >= 1)
            {
                throw new UserFriendlyException(L("CannotGenerateLateReturnLeave"));
            }

            return null;
        }

        var totalLeaveDays = await _absenceCalculationService.CalculateTotalLeaveDaysAsync(
            original.EmployeeId, absenceTypeId, startDate.Value, endDate.Value, 0);

        var generated = new EmployeeAbsence
        {
            TenantId = original.TenantId,
            EmployeeId = original.EmployeeId,
            AbsenceTypeId = absenceTypeId,
            StartDate = startDate.Value,
            EndDate = endDate.Value,
            InitialEndDate = endDate.Value,
            CalendarDays = (endDate.Value - startDate.Value).TotalDays + 1,
            TotalLeaveDays = totalLeaveDays,
            LeavePaymentType = LeavePaymentType.PayWithPayroll,
            Comments = original.Comments,

            // Born approved -- the approval of the return IS the approval of these days -- and linked to the
            // request that approved them, so the history screen can say where they came from.
            WorkflowStatus = RequestStatus.Approved,
            WorkflowRequestId = context.Request?.Id
        };

        if (stampReturn)
        {
            generated.ReturnDate = returnDate;
            generated.IsReturned = true;
        }

        await Repository.InsertAsync(generated);

        // Flushed before the next placement: the date walker skips days on which another leave starts, and it
        // can only see this one once it is in the database.
        await _unitOfWorkManager.Current.SaveChangesAsync();

        await ConsumeQuotaAsync(original.EmployeeId, absenceTypeId, totalLeaveDays, generated.StartDate);

        return generated;
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

            if (proposed.IsCancellation)
            {
                return new WorkflowSummary(
                    "Cancel: " + DescribeLeave(leave.AbsenceTypeId, leave.StartDate, leave.EndDate, leave.TotalLeaveDays, arabic: false),
                    "إلغاء: " + DescribeLeave(leave.AbsenceTypeId, leave.StartDate, leave.EndDate, leave.TotalLeaveDays, arabic: true));
            }

            if (proposed.IsLateReturn)
            {
                var rejoin = proposed.ReturnDate!.Value.ToString("dd MMM yyyy");

                return new WorkflowSummary(
                    "Late return on " + rejoin + ": " + DescribeLeave(leave.AbsenceTypeId, leave.StartDate, leave.EndDate, leave.TotalLeaveDays, arabic: false),
                    "عودة متأخرة في " + rejoin + ": " + DescribeLeave(leave.AbsenceTypeId, leave.StartDate, leave.EndDate, leave.TotalLeaveDays, arabic: true));
            }

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
