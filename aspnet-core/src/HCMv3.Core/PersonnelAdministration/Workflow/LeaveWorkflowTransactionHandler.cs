using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.SystemSettings.Services;
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
    private readonly IEmployeeAbsenceCalculationService _absenceCalculationService;
    private readonly ILeaveRuleValidator _leaveRuleValidator;
    private readonly ILeaveQuotaLedger _quotaLedger;
    private readonly ILeaveReturnProcessor _returnProcessor;
    private readonly IProbationEndDateService _probation;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;
    private readonly IRepository<EmployeeDelegation> _delegationRepository;

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
        IEmployeeAbsenceCalculationService absenceCalculationService,
        ILeaveRuleValidator leaveRuleValidator,
        ILeaveQuotaLedger quotaLedger,
        ILeaveReturnProcessor returnProcessor,
        IProbationEndDateService probation,
        ISysSettingsAccessor sysSettingsAccessor,
        IRepository<EmployeeDelegation> delegationRepository)
        : base(repository, unitOfWorkManager)
    {
        _delegationRepository = delegationRepository;
        _absenceTypeRepository = absenceTypeRepository;
        _unitOfWorkManager = unitOfWorkManager;
        _leaveBalanceService = leaveBalanceService;
        _absenceCalculationService = absenceCalculationService;
        _leaveRuleValidator = leaveRuleValidator;
        _quotaLedger = quotaLedger;
        _returnProcessor = returnProcessor;
        _probation = probation;
        _sysSettingsAccessor = sysSettingsAccessor;
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
            daysAlreadyCounted: daysAlreadyCounted,
            isNewRequest: !context.IsAmendment,
            leavePaymentType: leave.LeavePaymentType,
            paymentDate: leave.PaymentDate,
            comments: proposed?.Comments ?? leave.Comments,
            row: leave,

            // Asked only of the employee applying for their own leave. HR recording a leave on somebody's behalf
            // is not the person the policy is addressed to, and a change does not ask again.
            policyAccepted: !context.IsAmendment && context.ActingEmployeeId == leave.EmployeeId
                ? leave.PolicyAcceptedOn.HasValue
                : (bool?)null,
            requestId: context.Request?.Id);

        if (proposed?.IsLateReturn == true)
        {
            await _returnProcessor.ValidateLateReturnAsync(ToReturnInstruction(context, leave, proposed));

            // Legacy compared the balance against the leave PLUS the paid late days (WorkFlowServices 842-895).
            await EnsureLateReturnPaidDaysFitTheBalanceAsync(leave, proposed);
        }
    }


    /// <summary>The return, as the shared processor takes it -- the same shape HR's screen builds.</summary>
    private static LeaveReturnInstruction ToReturnInstruction(
        WorkflowTransactionContext context, EmployeeAbsence leave, LeaveAmendment proposed)
    {
        return new LeaveReturnInstruction
        {
            Leave = leave,
            ReturnDate = proposed.ReturnDate ?? default,
            PaidStartDate = proposed.PaidStartDate,
            PaidEndDate = proposed.PaidEndDate,
            UnpaidStartDate = proposed.UnpaidStartDate,
            UnpaidEndDate = proposed.UnpaidEndDate,
            AdditionalLeaveAction = proposed.AdditionalLeaveAction,
            AdditionalLeaveEndDate = proposed.AdditionalLeaveEndDate,
            WorkflowRequestId = context.Request?.Id,
        };
    }

    /// <summary>
    /// Legacy's balance test for a return (<c>WorkFlowServices</c> 842-895): the employee must still be able to
    /// book the leave they already hold PLUS the paid days of the late gap.
    /// </summary>
    private async Task EnsureLateReturnPaidDaysFitTheBalanceAsync(EmployeeAbsence leave, LeaveAmendment proposed)
    {
        if (proposed.PaidStartDate == null || proposed.PaidEndDate == null)
        {
            return;
        }

        var annualTypeId = _absenceTypeRepository.GetAll()
            .Where(t => t.Code == AnnualLeaveTypeCode)
            .Select(t => (int?)t.Id)
            .FirstOrDefault() ?? leave.AbsenceTypeId;

        var excluded = (double)await _absenceCalculationService.ActualLeaveDaysAsync(
            leave.EmployeeId, proposed.PaidStartDate.Value.Date, proposed.PaidEndDate.Value.Date, annualTypeId);

        var deductFromQuota = (proposed.PaidEndDate.Value.Date - proposed.PaidStartDate.Value.Date).TotalDays + 1 - excluded;

        if (deductFromQuota <= 0)
        {
            return;
        }

        var heldDays = DaysOf(leave.TotalLeaveDays, leave.StartDate, leave.EndDate);

        await _leaveRuleValidator.ValidateAsync(new LeaveRuleContext
        {
            EmployeeId = leave.EmployeeId,
            AbsenceTypeId = leave.AbsenceTypeId,
            StartDate = leave.StartDate,
            EndDate = leave.EndDate,
            TotalLeaveDays = heldDays,
            ExcludeEmployeeAbsenceId = leave.Id,
            IsNew = false,
            DaysAlreadyCounted = heldDays,
            DeductFromQuota = deductFromQuota,
        });
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
            await _returnProcessor.ApplyAsync(ToReturnInstruction(context, leave, proposed));
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
            daysAlreadyCounted: daysAlreadyCounted,
            isNewRequest: false,
            row: leave,
            requestId: context.Request?.Id);

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

        // An early or on-time return may also change the additional unpaid leave booked with this one.
        if (proposed.IsReturned)
        {
            await _returnProcessor.ApplyAsync(ToReturnInstruction(context, leave, proposed));
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

        await SyncApproverDelegationAsync(leave);
    }

    /// <summary>
    /// Legacy <c>AbsenceRequest</c> approval, <c>ActionType.Cancellation</c> (lines 1822-1884) plus
    /// <c>EmployeeAbsence.OnDeleting</c> (lines 1321-1419): the leave stops counting and its days go back
    /// to the quota. Legacy deleted the row; here it is marked <see cref="RequestStatus.Cancelled"/>, which the
    /// <c>ApprovedOnly</c> filter hides from payroll just the same while keeping it in the employee's history.
    ///
    /// <para>
    /// Rows a late return generated earlier are NOT removed with it -- legacy left them too, and they are
    /// separate leave in their own right that HR can remove on their own.
    /// </para>
    /// <para>
    /// Payroll needs no call from here: changing the row goes through the payroll lock guard and the retro
    /// trigger like any other change to a payroll-sensitive entity, which is what legacy's
    /// <c>ValidateRetroCalculation</c> / <c>EarliestMasterDataChange</c> did in <c>OnDeleting</c>.
    /// </para>
    /// </summary>
    private async Task ApplyCancellationAsync(EmployeeAbsence leave)
    {
        var days = DaysOf(leave.TotalLeaveDays, leave.StartDate, leave.EndDate);

        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            // Marked, not deleted. The ApprovedOnly filter hides a Cancelled row from payroll, quota and every
            // report exactly as a delete would, while the employee and HR can still see that the leave existed
            // and that its cancellation was approved.
            leave.WorkflowStatus = RequestStatus.Cancelled;
            await Repository.UpdateAsync(leave);

            // The additional unpaid leave goes with it: legacy deleted that row too (AbsenceRequest 1846-1870),
            // because the two were one application.
            if (leave.ExtAbsenceTypeId != null && leave.ExtStartDate != null && leave.ExtEndDate != null)
            {
                var extStart = leave.ExtStartDate.Value.Date;
                var extEnd = leave.ExtEndDate.Value.Date;

                var unpaid = Repository.GetAll().FirstOrDefault(a => a.EmployeeId == leave.EmployeeId
                                                                     && a.AbsenceTypeId == leave.ExtAbsenceTypeId.Value
                                                                     && a.StartDate == extStart
                                                                     && a.EndDate == extEnd);

                if (unpaid != null)
                {
                    unpaid.WorkflowStatus = RequestStatus.Cancelled;
                    await Repository.UpdateAsync(unpaid);
                    await ReleaseQuotaAsync(unpaid.EmployeeId, unpaid.AbsenceTypeId, unpaid.TotalLeaveDays, unpaid.StartDate);
                }
            }
        }

        await ReleaseQuotaAsync(leave.EmployeeId, leave.AbsenceTypeId, days, leave.StartDate);

        await _unitOfWorkManager.Current.SaveChangesAsync();

        await _probation.RecalculateAfterLeaveChangeAsync(leave.EmployeeId, leave.AbsenceTypeId);

        await SyncApproverDelegationAsync(leave);
    }

    /// <summary>
    /// Keeps the approver delegation in step with the leave it was raised with.
    ///
    /// <para>
    /// Legacy stored <c>ApproverDelegation</c> on the request and did nothing with it. Here an approved leave
    /// that names one gets a real <see cref="EmployeeDelegation"/> for the leave's dates, born approved and
    /// linked to the leave's own request, so the delegate receives the employee's approvals while they are
    /// away. It follows the leave afterwards: new dates move it, an early return shortens it, and a cancelled
    /// leave removes it. The match is on the leave's request id, so a delegation the employee raised
    /// separately is never touched.
    /// </para>
    /// </summary>
    private async Task SyncApproverDelegationAsync(EmployeeAbsence leave)
    {
        if (leave.WorkflowRequestId == null)
        {
            return;
        }

        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var existing = _delegationRepository.GetAll()
                .FirstOrDefault(d => d.EmployeeId == leave.EmployeeId && d.WorkflowRequestId == leave.WorkflowRequestId);

            var wanted = leave.ApproverDelegateId.HasValue
                         && leave.WorkflowStatus == RequestStatus.Approved
                         && !leave.IsDeleted;

            if (!wanted)
            {
                if (existing != null)
                {
                    await _delegationRepository.DeleteAsync(existing);
                }

                return;
            }

            if (existing == null)
            {
                await _delegationRepository.InsertAsync(new EmployeeDelegation
                {
                    TenantId = leave.TenantId,
                    EmployeeId = leave.EmployeeId,
                    DelegateToId = leave.ApproverDelegateId.Value,
                    StartDate = leave.StartDate.Date,
                    EndDate = leave.EndDate.Date,
                    Comments = leave.Comments?.Length > 1000 ? leave.Comments.Substring(0, 1000) : leave.Comments,
                    WorkflowStatus = RequestStatus.Approved,
                    WorkflowRequestId = leave.WorkflowRequestId
                });

                return;
            }

            existing.DelegateToId = leave.ApproverDelegateId.Value;
            existing.StartDate = leave.StartDate.Date;
            existing.EndDate = leave.EndDate.Date;
            await _delegationRepository.UpdateAsync(existing);
        }
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

        await _quotaLedger.ConsumeAsync(employeeId, absenceTypeId, days, asOf);
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

        await _quotaLedger.ReleaseAsync(employeeId, absenceTypeId, days, asOf);
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
        double daysAlreadyCounted,
        bool isNewRequest,
        LeavePaymentType leavePaymentType = LeavePaymentType.None,
        DateTime paymentDate = default,
        string comments = null,
        EmployeeAbsence row = null,
        bool? policyAccepted = null,
        int? requestId = null)
    {
        if (endDate.Date < startDate.Date)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        // Legacy LeaveRequest.PerformValidation "Check If Leave Request is Pending", governed by
        // ManageTimeSettings.EnableBookingQuotaInLeaveRequest:
        //   on  -- a pending request of ANY type overlapping the dates blocks the new one (and pending days come
        //          off the bookable balance, see LeaveBalanceService);
        //   off -- a pending request of the SAME type blocks the new one whatever its dates, and pending
        //          requests of other types do not block by dates at all.
        // Approved leave overlapping the dates blocks in both cases.
        var timeSettings = await _sysSettingsAccessor.GetTimeSettingsAsync();
        var bookingQuotaOn = timeSettings != null && timeSettings.EnableBookingQuotaInLeaveRequest;

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
                                || (bookingQuotaOn && a.WorkflowStatus == RequestStatus.Open)))
                .Select(a => (int?)a.Id)
                .FirstOrDefault();

            if (overlapping.HasValue)
            {
                throw new UserFriendlyException(L("OverlappingLeaveRequestExists"));
            }

            if (!bookingQuotaOn)
            {
                var pendingSameType = Repository.GetAll()
                    .Where(a => a.Id != excludeLeaveId
                                && a.EmployeeId == employeeId
                                && a.AbsenceTypeId == absenceTypeId
                                && a.WorkflowStatus == RequestStatus.Open)
                    .Select(a => (int?)a.Id)
                    .FirstOrDefault();

                if (pendingSameType.HasValue)
                {
                    throw new UserFriendlyException(L("PendingLeaveRequestOfSameTypeExists"));
                }
            }
        }

        // The leave-policy rules legacy applied to every request and every saved row: probation, notice period,
        // minimum and maximum length, repeat rules, blocked start days, salary profile, back-dating, travel,
        // separation, and the quota rules for a type with no quota row.
        await _leaveRuleValidator.ValidateAsync(new LeaveRuleContext
        {
            EmployeeId = employeeId,
            AbsenceTypeId = absenceTypeId,
            StartDate = startDate,
            EndDate = endDate,
            TotalLeaveDays = totalDays,
            ExcludeEmployeeAbsenceId = excludeLeaveId,
            EmployeeAbsenceId = excludeLeaveId,
            IsNew = isNewRequest,
            DaysAlreadyCounted = daysAlreadyCounted,
            LeavePaymentType = leavePaymentType,
            PaymentDate = paymentDate,
            Comments = comments,

            // The hourly fields always come from the row: an amendment changes type and dates, never the times.
            AbsenceHours = row?.AbsenceHours ?? 0,
            StartTime = row?.StartTime ?? default,
            EndTime = row?.EndTime ?? default,
            HalfdayType = row?.HalfdayType ?? HalfdayType.None,
            CreatedOn = isNewRequest ? row?.CreationTime : null,

            // Delegates and the policy tick belong to the application itself. A change to an approved leave
            // does not ask for them again, so they are judged once, when the leave is first raised.
            PrimaryDelegateId = isNewRequest ? row?.PrimaryDelegateId : null,
            SecondaryDelegateId = isNewRequest ? row?.SecondaryDelegateId : null,
            ApproverDelegateId = isNewRequest ? row?.ApproverDelegateId : null,
            PolicyAccepted = policyAccepted,
            ExcludeWorkflowRequestId = requestId,
        });
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

        await SetAdvancePaymentDateAsync(leave);

        var days = DaysOf(leave.TotalLeaveDays, leave.StartDate, leave.EndDate);

        await ConsumeQuotaAsync(leave.EmployeeId, leave.AbsenceTypeId, days, leave.StartDate);

        await SaveAdditionalUnpaidLeaveAsync(context, leave);

        await SyncApproverDelegationAsync(leave);

        // Unpaid leave during probation pushes its end date out (legacy Employee.GetProbationEndDate).
        await _probation.RecalculateAfterLeaveChangeAsync(leave.EmployeeId, leave.AbsenceTypeId);
        if (leave.ExtAbsenceTypeId != null)
        {
            await _probation.RecalculateAfterLeaveChangeAsync(leave.EmployeeId, leave.ExtAbsenceTypeId.Value);
        }
    }


    /// <summary>
    /// Legacy <c>AbsenceRequest.SaveAdditionalUnpaidLeave</c> (2342-2394): the unpaid remainder the employee
    /// asked for alongside the paid days becomes its own leave row once the application is approved.
    ///
    /// <para>
    /// Its days are counted with the MAIN type's week-off and holiday rules, which is what legacy did (it
    /// passed <c>this.AbsenceType</c>, not the unpaid one). The row is born approved and linked to the same
    /// request, so the history shows where it came from.
    /// </para>
    /// </summary>
    private async Task SaveAdditionalUnpaidLeaveAsync(WorkflowTransactionContext context, EmployeeAbsence leave)
    {
        if (leave.ExtAbsenceTypeId == null || leave.ExtStartDate == null || leave.ExtEndDate == null)
        {
            return;
        }

        var start = leave.ExtStartDate.Value.Date;
        var end = leave.ExtEndDate.Value.Date;

        var totalLeaveDays = leave.ExtTotalLeaveDays
                             ?? await _absenceCalculationService.CalculateTotalLeaveDaysAsync(
                                 leave.EmployeeId, leave.AbsenceTypeId, start, end, 0);

        var unpaid = new EmployeeAbsence
        {
            TenantId = leave.TenantId,
            EmployeeId = leave.EmployeeId,
            AbsenceTypeId = leave.ExtAbsenceTypeId.Value,
            StartDate = start,
            EndDate = end,
            InitialEndDate = end,
            CalendarDays = (end - start).TotalDays + 1,
            TotalLeaveDays = totalLeaveDays,
            Comments = leave.Comments,
            LeavePaymentType = LeavePaymentType.PayWithPayroll,
            WorkflowStatus = RequestStatus.Approved,
            WorkflowRequestId = context.Request?.Id
        };

        await Repository.InsertAsync(unpaid);
        await _unitOfWorkManager.Current.SaveChangesAsync();

        // Unpaid leave is normally not quota-controlled, so this is usually a no-op -- but if the tenant does
        // keep a quota for it, it is charged like any other approved leave.
        await ConsumeQuotaAsync(unpaid.EmployeeId, unpaid.AbsenceTypeId, totalLeaveDays, unpaid.StartDate);
    }

    /// <summary>
    /// Legacy <c>AbsenceRequest</c> approval (lines 1305-1328): for a leave to be PAID IN ADVANCE, the payment
    /// date on the leave is not the one the employee asked for -- it is recomputed to the first REGULAR day
    /// (not a week-off, holiday or rest day) from the 1st of the month when the leave starts on or before the
    /// 15th, and from the 15th when it starts later. If no regular day is found the leave's own start date is
    /// used, as legacy did.
    ///
    /// <para>
    /// Note the asymmetry kept from legacy: the DEFAULT offered before approval is the 1st or the 16th, while
    /// this recompute searches from the 1st or the 15th.
    /// </para>
    /// </summary>
    private async Task SetAdvancePaymentDateAsync(EmployeeAbsence leave)
    {
        if (leave.LeavePaymentType != LeavePaymentType.PayInAdvance)
        {
            return;
        }

        var start = leave.StartDate.Date;
        var searchFrom = start.Day <= 15
            ? new DateTime(start.Year, start.Month, 1)
            : new DateTime(start.Year, start.Month, 15);

        var regularDay = await _absenceCalculationService.GetFirstRegularDayAsync(
            leave.EmployeeId, leave.AbsenceTypeId, searchFrom, leave.EndDate.Date);

        leave.PaymentDate = regularDay ?? start;

        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            await Repository.UpdateAsync(leave);
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
