using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Timing;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using HCMv3.TimeManagement.Services;
using HCMv3.Storage;
using HCMv3.SystemSettings.Services;
using HCMv3.TravelManagement;
using HCMv3.Workflow;

namespace HCMv3.PersonnelAdministration.Services;

/// <inheritdoc cref="ILeaveRuleValidator"/>
/// <remarks>
/// Ported from legacy <c>WorkFlowServices.SaveWorkflowRequest</c> (the AbsenceRequest block, lines 142-1268),
/// <c>ValidationService</c> (lines 24-128) and <c>EmployeeAbsence.PerformValidations</c> /
/// <c>UpdateQuotaInformation</c>, which between them applied the same rules on the request and on the row.
/// HCMv3 has one leave table, so there is one rule set, called from the ESS/workflow handler and from the HR
/// screen alike.
///
/// <para><b>Order matters.</b> Legacy reports the first rule that fails, and testers know the messages by the
/// order they appear in; the order here is legacy's.</para>
///
/// <para>The attachment rule (<c>IsAttachmentRequired</c>) reads the generic <c>AppAttachment</c> store that
/// replaced legacy's per-entity <c>DocAttachment</c>. <c>ValidateLeavePolicy</c> is ported as a live rule even
/// though legacy's copy did nothing for the leave its own ESS created -- it only ever branched on the retired
/// <c>LeaveRequest</c> types -- because the flags it reads are on every absence type.</para>
/// </remarks>
[UnitOfWork]
public class LeaveRuleValidator : HCMv3DomainServiceBase, ILeaveRuleValidator
{
    /// <summary>How leave records are keyed in the attachment store; matches the owner policy.</summary>
    public const string LeaveAttachmentEntityType = "EmployeeAbsence";

    private readonly IRepository<EmployeeAbsence> _absenceRepository;
    private readonly IRepository<AbsenceType> _absenceTypeRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeAbsenceQuota> _quotaRepository;
    private readonly IRepository<EmployeeSeparation> _separationRepository;
    private readonly IRepository<EmployeeRejoin> _rejoinRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IRepository<EmployeeSubGroupAbsence> _subGroupAbsenceRepository;
    private readonly IRepository<EmployeeSubGroupAbsenceType> _subGroupAbsenceTypeRepository;
    private readonly IRepository<Travel> _travelRepository;
    private readonly ISysSettingsAccessor _settings;
    private readonly ITimeDataService _timeDataService;
    private readonly IEmployeeAbsenceCalculationService _calculationService;
    private readonly IAccrualService _accrualService;
    private readonly ILeaveBalanceService _leaveBalanceService;
    private readonly IAttachmentManager _attachmentManager;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IRepository<Religion> _religionRepository;

    public LeaveRuleValidator(
        IRepository<EmployeeAbsence> absenceRepository,
        IRepository<AbsenceType> absenceTypeRepository,
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeAbsenceQuota> quotaRepository,
        IRepository<EmployeeSeparation> separationRepository,
        IRepository<EmployeeRejoin> rejoinRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        IRepository<EmployeeSubGroupAbsence> subGroupAbsenceRepository,
        IRepository<EmployeeSubGroupAbsenceType> subGroupAbsenceTypeRepository,
        IRepository<Travel> travelRepository,
        ISysSettingsAccessor settings,
        ITimeDataService timeDataService,
        IEmployeeAbsenceCalculationService calculationService,
        IAccrualService accrualService,
        ILeaveBalanceService leaveBalanceService,
        IAttachmentManager attachmentManager,
        IUnitOfWorkManager unitOfWorkManager,
        IRepository<Religion> religionRepository)
    {
        _religionRepository = religionRepository;
        _absenceRepository = absenceRepository;
        _absenceTypeRepository = absenceTypeRepository;
        _employeeRepository = employeeRepository;
        _quotaRepository = quotaRepository;
        _separationRepository = separationRepository;
        _rejoinRepository = rejoinRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _subGroupAbsenceRepository = subGroupAbsenceRepository;
        _subGroupAbsenceTypeRepository = subGroupAbsenceTypeRepository;
        _travelRepository = travelRepository;
        _settings = settings;
        _timeDataService = timeDataService;
        _calculationService = calculationService;
        _accrualService = accrualService;
        _leaveBalanceService = leaveBalanceService;
        _attachmentManager = attachmentManager;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public async Task ValidateAsync(LeaveRuleContext context)
    {
        var employee = _employeeRepository.GetAll().FirstOrDefault(e => e.Id == context.EmployeeId)
                       ?? throw new UserFriendlyException(L("EmployeeCannotBeBlank"));

        var absenceType = _absenceTypeRepository.GetAll().FirstOrDefault(t => t.Id == context.AbsenceTypeId)
                          ?? throw new UserFriendlyException(L("AbsenceTypeNotFound"));

        var start = context.StartDate.Date;
        var end = context.EndDate.Date;

        if (start == default || end == default)
        {
            throw new UserFriendlyException(L("LeaveDatesCannotBeEmpty"));
        }

        if (end < start)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        EnsureLeaveTypeSuitsTheEmployee(employee, absenceType);

        var timeSettings = await _settings.GetTimeSettingsAsync();

        // Legacy EmployeeAbsenceController: the half-day editor is only enabled when
        // ManageTimeSettings.EnableAutoHalfDayLeave is on, so a half-day leave cannot be entered without it.
        if (context.HalfdayType != HalfdayType.None && (timeSettings == null || !timeSettings.EnableAutoHalfDayLeave))
        {
            throw new UserFriendlyException(L("HalfDayLeaveNotEnabled"));
        }

        // Legacy WorkFlowServices.LeavePaymentTypeList: "Pay Full With Payroll" is only offered when
        // ManageTimeSettings.EnablePayFullWithPayroll is on.
        if (context.LeavePaymentType == LeavePaymentType.PayFullWithPayroll
            && (timeSettings == null || !timeSettings.EnablePayFullWithPayroll))
        {
            throw new UserFriendlyException(L("PayFullWithPayrollNotEnabled"));
        }

        if (context.AbsenceHours != 0 || context.HalfdayType != HalfdayType.None)
        {
            if (!absenceType.AllowPartialLeave)
            {
                throw new UserFriendlyException(L("PartialLeaveNotAllowedForLeaveType", absenceType.NameEn));
            }

            if (start != end)
            {
                throw new UserFriendlyException(L("HourlyLeaveMustStartAndEndOnTheSameDay"));
            }

            if (context.EndTime < context.StartTime)
            {
                throw new UserFriendlyException(L("EndTimeCannotBeEarlierThanStartTime"));
            }
        }

        EnsureDurationTypeIsRespected(context, absenceType);
        await EnsureHourlyLimitsAreRespectedAsync(context, absenceType, start);

        EnsurePaymentDateIsUsable(context, start);

        var days = context.TotalLeaveDays > 0
            ? context.TotalLeaveDays
            : await _calculationService.CalculateTotalLeaveDaysAsync(
                context.EmployeeId, context.AbsenceTypeId, start, end, context.AbsenceHours);

        await EnsureQuotaRowExistsAsync(context, absenceType);
        EnsureNotTooFarBackDated(context, absenceType, start);
        EnsureNotTooFarAhead(context, absenceType, start);
        EnsureNotInProbation(employee, absenceType, start);
        await EnsureStartAndEndAreRegularDaysAsync(context, employee, absenceType, start, end);
        EnsureAtLeastOneDay(days);
        EnsureConfiguredInSalaryProfile(context, absenceType, start);
        EnsureMinimumServiceDays(employee, absenceType, start);
        EnsureWithinMinAndMaxLength(absenceType, days);
        EnsureRepeatRulesAllowIt(context, absenceType, start);
        EnsureNoticePeriodGiven(absenceType, start);
        EnsureStartDayIsAllowed(absenceType, start);
        await EnsureQuotaIsAvailableAsync(context, absenceType, start, end, days);
        await EnsureUnpaidIsAllowedAsync(context, absenceType, start);
        EnsureNoTravelInTheSameDays(context, start, end);
        EnsureSeparationRulesAllowIt(employee, absenceType, start, end);
        await EnsureCompanyLeavePolicyAllowsItAsync(context, absenceType, start, days);
        EnsureDelegatesAreUsable(context, start, end);
        EnsurePolicyIsAccepted(context, absenceType);
        await EnsureRequiredDocumentIsAttachedAsync(context, absenceType);
    }

    /// <summary>
    /// Whether a leave type is open to this employee by gender and religion -- maternity for women, paternity
    /// for men, a Muslim and a non-Muslim Iddah leave with different lengths.
    ///
    /// <para>
    /// Legacy only filtered the picklist (<c>AbsenceRequest.EmpAbsenceType</c>, 63-116) and never refused on
    /// save, so a request that reached the server by any other route went through. The same test is applied
    /// here as a refusal, and <see cref="LeaveTypeSuits"/> is what the ESS list is filtered with, so the two
    /// cannot disagree. The Muslim test is legacy's: the religion's English name is "muslim" or "islam".
    /// An employee with no religion recorded is offered every type, as legacy did.
    /// </para>
    /// </summary>
    private void EnsureLeaveTypeSuitsTheEmployee(Employee employee, AbsenceType absenceType)
    {
        var religionName = employee.ReligionId.HasValue
            ? _religionRepository.GetAll().Where(r => r.Id == employee.ReligionId.Value).Select(r => r.NameEn).FirstOrDefault()
            : null;

        if (!LeaveTypeSuits(absenceType.LeaveGender, absenceType.LeaveReligion, employee.Gender, employee.ReligionId.HasValue, religionName))
        {
            throw new UserFriendlyException(L("LeaveTypeNotAvailableForThisEmployee", absenceType.NameEn));
        }
    }

    /// <summary>The gender and religion test, shared by the refusal above and by the ESS leave type list.</summary>
    public static bool LeaveTypeSuits(
        LeaveGender leaveGender, LeaveReligion leaveReligion, Gender employeeGender, bool hasReligion, string religionNameEn)
    {
        if (employeeGender == Gender.Male && leaveGender == LeaveGender.Female)
        {
            return false;
        }

        if (employeeGender == Gender.Female && leaveGender == LeaveGender.Male)
        {
            return false;
        }

        if (!hasReligion || leaveReligion == LeaveReligion.All)
        {
            return true;
        }

        var name = religionNameEn?.Trim().ToLowerInvariant();
        var isMuslim = name == "muslim" || name == "islam";

        return isMuslim ? leaveReligion == LeaveReligion.Muslim : leaveReligion == LeaveReligion.NonMuslim;
    }

    /// <summary>
    /// Legacy <c>LeaveDurationType</c> (<c>AbsenceRequest</c> 1985-1991): a type that is Partial-only needs a
    /// start and end time. The mirror -- a FullDay-only type refusing them -- is not in legacy, which relied on
    /// <c>AllowPartialLeave</c> being off for such a type; it is refused here so the two settings cannot
    /// contradict each other.
    /// </summary>
    private void EnsureDurationTypeIsRespected(LeaveRuleContext context, AbsenceType absenceType)
    {
        var isPartial = context.AbsenceHours != 0 || context.HalfdayType != HalfdayType.None;

        if (absenceType.LeaveDurationType == LeaveDurationType.Partial && !isPartial)
        {
            throw new UserFriendlyException(L("LeaveTypeCanOnlyBeAppliedAsPartialLeave", absenceType.NameEn));
        }

        if (absenceType.LeaveDurationType == LeaveDurationType.FullDay && isPartial)
        {
            throw new UserFriendlyException(L("PartialLeaveNotAllowedForLeaveType", absenceType.NameEn));
        }
    }

    /// <summary>
    /// Legacy "Validation For Hourly Basis Leave" (<c>AbsenceRequest</c> 2093-2180) and
    /// <c>EmployeeAbsence.ValidateHourlyLeaveAgainstDutyHours</c> (4004-4080): the minutes one hourly leave may
    /// take, how many such leaves and how many minutes in total the type allows within its reset period, and
    /// that the day's hourly leave does not exceed the day's duty.
    ///
    /// <para>
    /// Kept as legacy has them, including two things that look wrong and are not corrected here: the limits are
    /// compared even when they are zero (a type that allows partial leave must have them filled in), and a
    /// night shift's length is taken as start minus end. A half-day leave skips all of this, as it did in
    /// legacy, where this block was the <c>else</c> of the half-day one. Earlier leave is counted from the
    /// approved rows only, which is what legacy's leave table held.
    /// </para>
    /// </summary>
    private async Task EnsureHourlyLimitsAreRespectedAsync(LeaveRuleContext context, AbsenceType absenceType, DateTime start)
    {
        if (context.AbsenceHours == 0 || context.HalfdayType != HalfdayType.None)
        {
            return;
        }

        DateTime from;
        DateTime to;

        switch (absenceType.ResetFrequency)
        {
            case ResetFrequency.Weekly:
                from = start.AddDays(-(int)start.DayOfWeek);
                to = start.AddDays(6 - (int)start.DayOfWeek);
                break;

            case ResetFrequency.Monthly:
                from = new DateTime(start.Year, start.Month, 1);
                to = new DateTime(start.Year, start.Month, DateTime.DaysInMonth(start.Year, start.Month));
                break;

            case ResetFrequency.Quarterly:
                var quarter = (start.Month - 1) / 3 + 1;
                from = new DateTime(start.Year, 3 * quarter - 2, 1);
                to = new DateTime(start.Year, 3 * quarter, DateTime.DaysInMonth(start.Year, 3 * quarter));
                break;

            case ResetFrequency.Annually:
                from = new DateTime(start.Year, 1, 1);
                to = new DateTime(start.Year, 12, 31);
                break;

            default:
                from = start;
                to = start;
                break;
        }

        var appliedMinutes = context.EndTime.Subtract(context.StartTime).TotalMinutes;

        if (appliedMinutes > absenceType.MaxMinutes)
        {
            throw new UserFriendlyException(L(
                "HourlyLeaveExceedsMaximumMinutes", absenceType.NameEn, $"{absenceType.MaxMinutes}", $"{appliedMinutes:0.##}"));
        }

        var toExclusive = to.AddDays(1);

        var earlier = _absenceRepository.GetAll()
            .Where(a => a.EmployeeId == context.EmployeeId
                        && a.AbsenceTypeId == context.AbsenceTypeId
                        && a.Id != context.ExcludeEmployeeAbsenceId
                        && a.StartDate >= from
                        && a.EndDate < toExclusive
                        && a.AbsenceHours != 0)
            .Select(a => new { a.StartTime, a.EndTime })
            .ToList();

        var count = earlier.Count + 1;

        if (count > absenceType.MaxCount)
        {
            throw new UserFriendlyException(L(
                "HourlyLeaveExceedsMaximumCount", absenceType.NameEn, $"{absenceType.MaxCount}", $"{count}"));
        }

        var totalMinutes = earlier.Sum(a => a.EndTime.Subtract(a.StartTime).TotalMinutes) + appliedMinutes;

        if (totalMinutes > absenceType.TotalMinutes)
        {
            throw new UserFriendlyException(L(
                "HourlyLeaveExceedsTotalMinutes", absenceType.NameEn, $"{absenceType.TotalMinutes}", $"{totalMinutes:0.##}"));
        }

        TimeManagement.Duty duty;
        try
        {
            duty = await _calculationService.GetEmployeeDutyAsync(context.EmployeeId, start);
        }
        catch (InvalidOperationException)
        {
            throw new UserFriendlyException(L("DutyNotFoundForTheDay", start.ToString("dd/MM/yyyy")));
        }

        var dutyHours = (duty.IsNightShift ? duty.StartTime - duty.EndTime : duty.EndTime - duty.StartTime).TotalHours;

        if (dutyHours <= 0)
        {
            throw new UserFriendlyException(L("InvalidDutyHoursForTheDay", start.ToString("dd/MM/yyyy"), duty.NameEn));
        }

        var dayEnd = start.AddDays(1);

        var alreadyApplied = _absenceRepository.GetAll()
            .Where(a => a.EmployeeId == context.EmployeeId
                        && a.Id != context.ExcludeEmployeeAbsenceId
                        && a.StartDate >= start
                        && a.StartDate < dayEnd
                        && a.AbsenceHours != 0)
            .Select(a => a.AbsenceHours)
            .ToList()
            .Sum();

        if (alreadyApplied + context.AbsenceHours > dutyHours)
        {
            throw new UserFriendlyException(L(
                "HourlyLeaveExceedsDutyHours",
                $"{alreadyApplied + context.AbsenceHours:0.##}",
                $"{dutyHours:0.##}",
                start.ToString("dd/MM/yyyy"),
                $"{Math.Max(0, dutyHours - alreadyApplied):0.##}"));
        }
    }

    /// <summary>
    /// How far ahead a leave may start (<c>AbsenceType.MaxFutureDays</c>; zero is no limit). Not in legacy.
    /// Applied to a new leave only, like back-dating, so a request does not go stale while it waits.
    /// </summary>
    private void EnsureNotTooFarAhead(LeaveRuleContext context, AbsenceType absenceType, DateTime start)
    {
        if (!context.IsNew || absenceType.MaxFutureDays <= 0)
        {
            return;
        }

        var latest = (context.CreatedOn?.Date ?? Clock.Now.Date).AddDays(absenceType.MaxFutureDays);

        if (start > latest)
        {
            throw new UserFriendlyException(L(
                "LeaveStartDateCannotBeAfterTheMaximumDate",
                latest.ToString("dd/MM/yyyy"),
                $"{absenceType.MaxFutureDays}"));
        }
    }

    /// <summary>
    /// The three delegates on a leave. Legacy checked these in the browser only (<c>LeaveRequest.js</c>
    /// 127-146, 267-312; <c>UnifiedLeaveRequestController</c> 2995-3062): a secondary delegate cannot be chosen
    /// before a primary one, nobody delegates to themselves, the pick list holds active employees only, and a
    /// delegate who is on leave over the same dates is refused ("Delegated is on Leave").
    ///
    /// <para>
    /// Legacy judged "on leave" from the delegate's most recent request, whatever its status. Here it is
    /// their leave rows that overlap the dates, approved or awaiting approval -- the same question, asked of
    /// the leave table rather than of the last thing they happened to request.
    /// </para>
    /// </summary>
    private void EnsureDelegatesAreUsable(LeaveRuleContext context, DateTime start, DateTime end)
    {
        if (context.SecondaryDelegateId.HasValue && !context.PrimaryDelegateId.HasValue)
        {
            throw new UserFriendlyException(L("SelectPrimaryDelegateBeforeSecondary"));
        }

        if (context.SecondaryDelegateId.HasValue && context.SecondaryDelegateId == context.PrimaryDelegateId)
        {
            throw new UserFriendlyException(L("PrimaryAndSecondaryDelegateMustDiffer"));
        }

        foreach (var delegateId in new[] { context.PrimaryDelegateId, context.SecondaryDelegateId, context.ApproverDelegateId }
                     .Where(id => id.HasValue)
                     .Select(id => id.Value)
                     .Distinct())
        {
            if (delegateId == context.EmployeeId)
            {
                throw new UserFriendlyException(L("YouCannotDelegateToYourself"));
            }

            var delegateEmployee = _employeeRepository.GetAll()
                .Where(e => e.Id == delegateId)
                .Select(e => new { e.Status, e.FullNameEn })
                .FirstOrDefault();

            if (delegateEmployee == null || delegateEmployee.Status != EmployeeStatus.Active)
            {
                throw new UserFriendlyException(L("DelegateMustBeAnActiveEmployee"));
            }

            bool onLeave;
            using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
            {
                onLeave = _absenceRepository.GetAll().Any(a => a.EmployeeId == delegateId
                                                               && a.StartDate <= end
                                                               && a.EndDate >= start
                                                               && (a.WorkflowStatus == null
                                                                   || a.WorkflowStatus == RequestStatus.Approved
                                                                   || a.WorkflowStatus == RequestStatus.Closed
                                                                   || a.WorkflowStatus == RequestStatus.Open));
            }

            if (onLeave)
            {
                throw new UserFriendlyException(L("DelegateIsOnLeave", delegateEmployee.FullNameEn));
            }
        }
    }

    /// <summary>
    /// A type flagged <c>RequirePolicyAcceptance</c> cannot be submitted until the employee has accepted the
    /// leave policy. Legacy had one company-wide tick box checked in the browser only (<c>LeaveRequest.js</c>
    /// 1394-1410); this is per leave type and enforced here. Skipped when the question was not asked at all.
    /// </summary>
    private void EnsurePolicyIsAccepted(LeaveRuleContext context, AbsenceType absenceType)
    {
        if (absenceType.RequirePolicyAcceptance && context.PolicyAccepted == false)
        {
            throw new UserFriendlyException(L("PleaseAcceptTheLeavePolicy"));
        }
    }

    /// <summary>
    /// Legacy "Please Attach Document." (<c>WorkFlowServices</c> 592-602): an absence type flagged
    /// <c>IsAttachmentRequired</c> -- a sick note, a bereavement certificate -- cannot be submitted without one.
    ///
    /// <para>
    /// Checked against the leave row, through the generic attachment store that replaced legacy's per-entity
    /// <c>DocAttachment</c>. A row with no id yet has nowhere to attach to, so the rule waits until there is one:
    /// the ESS attaches the employee's file to the new leave before submitting it for approval.
    /// </para>
    /// </summary>
    private async Task EnsureRequiredDocumentIsAttachedAsync(LeaveRuleContext context, AbsenceType absenceType)
    {
        if (!absenceType.IsAttachmentRequired || context.EmployeeAbsenceId <= 0)
        {
            return;
        }

        var attachments = await _attachmentManager.GetListAsync(
            LeaveAttachmentEntityType, context.EmployeeAbsenceId.ToString(CultureInfo.InvariantCulture));

        if (attachments.Count == 0)
        {
            throw new UserFriendlyException(L("PleaseAttachDocument", absenceType.NameEn));
        }
    }

    /// <summary>
    /// The company leave policy, ported from legacy <c>ValidationService.ValidateLeavePolicy</c> (132-309): a
    /// type may require leave to be asked for a number of days in advance (<c>ApplyLeaveInAdvance</c> /
    /// <c>ApplyLeaveInAdvanceDays</c>), and may cap the days taken in the quota period
    /// (<c>ApplyMaxLeave</c> / <c>MaxLimitInDays</c>).
    ///
    /// <para>
    /// <c>PolicyRestrictType</c> decides what a breach costs. <b>Error</b> refuses the leave. <b>Warning</b>
    /// lets it through but makes the reason mandatory -- legacy's " Please Add your Comments.", which is the
    /// only consequence a warning ever had.
    /// </para>
    /// <para>
    /// Legacy called this for its <c>AbsenceRequest</c> too, but the routine only ever branched on the retired
    /// <c>LeaveRequest</c> types, so it did nothing for the leave the ESS actually creates. Ported here as a
    /// live rule for every leave (user decision 2026-09-22), which is what the flags on the absence type have
    /// always implied.
    /// </para>
    /// </summary>
    private async Task EnsureCompanyLeavePolicyAllowsItAsync(
        LeaveRuleContext context, AbsenceType absenceType, DateTime start, double days)
    {
        var breaksAdvanceNotice = absenceType.ApplyLeaveInAdvance
                                  && absenceType.ApplyLeaveInAdvanceDays > 0
                                  && (start - (context.CreatedOn?.Date ?? Clock.Now.Date)).TotalDays + 1 < absenceType.ApplyLeaveInAdvanceDays;

        var breaksMaximum = false;
        if (absenceType.ApplyMaxLeave && absenceType.MaxLimitInDays > 0)
        {
            var balances = await _leaveBalanceService.GetBalancesAsync(context.EmployeeId, start, context.ExcludeWorkflowRequestId);
            var used = balances.FirstOrDefault(b => b.AbsenceTypeId == context.AbsenceTypeId)?.Used ?? 0;

            // Legacy compares the quota's USED days (what has been taken this period) plus what is being asked
            // for now against the policy cap.
            breaksMaximum = used > 0 && used + days > absenceType.MaxLimitInDays;
        }

        if (!breaksAdvanceNotice && !breaksMaximum)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(context.Comments))
        {
            throw new UserFriendlyException(L("PleaseAddYourComments"));
        }

        if (absenceType.PolicyRestrictType != RestrictType.Error)
        {
            return;
        }

        // Advance notice wins when both are broken, as legacy ordered them.
        if (breaksAdvanceNotice)
        {
            var earliest = Clock.Now.Date.AddDays(absenceType.ApplyLeaveInAdvanceDays);

            throw new UserFriendlyException(L("CannotApplyLeaveBeforeCompanyPolicyDate", earliest.ToString("dd/MM/yyyy")));
        }

        throw new UserFriendlyException(L("CannotExceedMaximumLeavePolicy", $"{absenceType.MaxLimitInDays}"));
    }

    /// <summary>
    /// Legacy <c>EmployeeAbsence.PerformValidations</c> (1504-1516): an advance needs a date to be paid on, and
    /// any payment date must fall inside the month the leave starts in -- a later one belongs to a month the
    /// run has not reached.
    /// </summary>
    private void EnsurePaymentDateIsUsable(LeaveRuleContext context, DateTime start)
    {
        if (context.LeavePaymentType == LeavePaymentType.PayInAdvance && context.PaymentDate == default)
        {
            throw new UserFriendlyException(L("PaymentDateIsRequiredForPayInAdvance"));
        }

        if (context.PaymentDate == default)
        {
            return;
        }

        var lastDayOfStartMonth = new DateTime(start.Year, start.Month, DateTime.DaysInMonth(start.Year, start.Month));

        if (context.PaymentDate.Date > lastDayOfStartMonth)
        {
            throw new UserFriendlyException(L("PaymentDateShouldBeOnOrBefore", lastDayOfStartMonth.ToString("dd/MM/yyyy")));
        }
    }

    /// <summary>
    /// Legacy: an accrual-based type must have a quota row whose deduction window covers the leave --
    /// "Leave Quota not found within the Deduction From - to Dates" (WFS 183-188).
    /// </summary>
    private Task EnsureQuotaRowExistsAsync(LeaveRuleContext context, AbsenceType absenceType)
    {
        if (!absenceType.HaveQuotaAccrual)
        {
            return Task.CompletedTask;
        }

        var start = context.StartDate.Date;
        var end = context.EndDate.Date;

        var covered = _quotaRepository.GetAll().Any(q => q.EmployeeId == context.EmployeeId
                                                         && q.AbsenceTypeId == context.AbsenceTypeId
                                                         && q.DeductionFrom <= start
                                                         && q.DeductionTo >= end);

        if (!covered)
        {
            throw new UserFriendlyException(L("LeaveQuotaNotFoundForTheDeductionPeriod", absenceType.NameEn));
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Legacy <c>AllowBackDatedDays</c> (WFS 192-211): how far back a leave may start. Measured from today for a
    /// new leave and from the day the request was raised for one being edited, so a request does not go stale
    /// while it waits in an inbox.
    /// </summary>
    private void EnsureNotTooFarBackDated(LeaveRuleContext context, AbsenceType absenceType, DateTime start)
    {
        if (!context.IsNew || absenceType.AllowBackDatedDays < 0)
        {
            return;
        }

        var from = (context.CreatedOn?.Date ?? Clock.Now.Date).AddDays(-absenceType.AllowBackDatedDays);

        if (start < from)
        {
            throw new UserFriendlyException(L(
                "LeaveStartDateCannotBeBeforeTheMinimumDate",
                from.ToString("dd/MM/yyyy"),
                $"{absenceType.AllowBackDatedDays}"));
        }
    }

    /// <summary>Legacy WFS 589-590: a type flagged not allowed during probation.</summary>
    private void EnsureNotInProbation(Employee employee, AbsenceType absenceType, DateTime start)
    {
        if (!absenceType.AllowDuringProbation
            && employee.ProbationEndDate != default
            && employee.ProbationEndDate.Date > start)
        {
            throw new UserFriendlyException(L("LeaveTypeNotAllowedWithinProbationPeriod", absenceType.NameEn));
        }
    }

    /// <summary>
    /// Legacy <c>ValidationService.ValidateDate</c> (24-48): a leave may not start or end on a holiday, week-off
    /// or rest day unless the day's duty allows a transaction to start on one. Legacy used
    /// <c>AllowTransStart</c> for both ends, and that is kept.
    /// </summary>
    private async Task EnsureStartAndEndAreRegularDaysAsync(
        LeaveRuleContext context, Employee employee, AbsenceType absenceType, DateTime start, DateTime end)
    {
        if (context.AbsenceHours != 0)
        {
            return;
        }

        await EnsureRegularDayAsync(employee, absenceType, start, "LeaveStartDateShouldBeARegularDay");
        await EnsureRegularDayAsync(employee, absenceType, end, "LeaveEndDateShouldBeARegularDay");
    }

    private async Task EnsureRegularDayAsync(Employee employee, AbsenceType absenceType, DateTime day, string messageKey)
    {
        TimeManagement.Duty duty;
        try
        {
            duty = await _calculationService.GetEmployeeDutyAsync(employee.Id, day);
        }
        catch (InvalidOperationException)
        {
            // Legacy "Duty is not found" -- the employee has no working week covering that day.
            throw new UserFriendlyException(L("DutyNotFoundForTheDay", day.ToString("dd/MM/yyyy")));
        }

        if (duty.AllowTransStart)
        {
            return;
        }

        if (await _timeDataService.IsHolidayAsync(day, absenceType, employee)
            || await _timeDataService.IsWeekEndAsync(day, absenceType, employee)
            || await _timeDataService.IsRestDayAsync(day, absenceType, employee))
        {
            throw new UserFriendlyException(L(messageKey, day.ToString("dd/MM/yyyy")));
        }
    }

    /// <summary>Legacy <c>MinDaysValidation</c> (VS 98-128): a leave made up entirely of excluded days is no leave.</summary>
    private void EnsureAtLeastOneDay(double days)
    {
        if (days <= 0)
        {
            throw new UserFriendlyException(L("LeaveDaysShouldBeAtLeastASingleDay"));
        }
    }

    /// <summary>Legacy WFS 640-653: a type flagged <c>IncludeInSalaryProfile</c> must exist in the employee's salary profile.</summary>
    private void EnsureConfiguredInSalaryProfile(LeaveRuleContext context, AbsenceType absenceType, DateTime start)
    {
        if (!absenceType.IncludeInSalaryProfile)
        {
            return;
        }

        var subGroupId = CurrentSubGroupId(context.EmployeeId, start);

        var configured = subGroupId.HasValue
                         && _subGroupAbsenceTypeRepository.GetAll().Any(a => a.EmployeeSubGroupId == subGroupId.Value
                                                                             && a.AbsenceTypeId == context.AbsenceTypeId);

        if (!configured)
        {
            throw new UserFriendlyException(L("LeaveTypeShouldBeConfiguredInSalaryProfile", absenceType.NameEn));
        }
    }

    /// <summary>Legacy WFS 657-670: the employee must have served long enough to take this type at all.</summary>
    private void EnsureMinimumServiceDays(Employee employee, AbsenceType absenceType, DateTime start)
    {
        if (absenceType.MinActiveDaysInService <= 0)
        {
            return;
        }

        var activeDays = (start - employee.JoiningDate.Date).TotalDays;

        if (activeDays < absenceType.MinActiveDaysInService)
        {
            throw new UserFriendlyException(L(
                "CurrentActiveDaysLessThanMinActiveDaysInService",
                $"{absenceType.MinActiveDaysInService:0.##}",
                $"{activeDays:0.##}"));
        }
    }

    /// <summary>
    /// Legacy WFS 674-702. The maximum is not applied when the type is allowed to turn the excess into unpaid
    /// leave (<c>CreateUnpaidAboveMaxLimitDays</c>), which is legacy's additional-unpaid-leave path.
    /// </summary>
    private void EnsureWithinMinAndMaxLength(AbsenceType absenceType, double days)
    {
        if (absenceType.ApplyMaxLeave && days > absenceType.MaxLimitInDays && !absenceType.CreateUnpaidAboveMaxLimitDays)
        {
            throw new UserFriendlyException(L(
                "TotalLeaveDaysMoreThanMaxLimitInDays", $"{absenceType.MaxLimitInDays}", $"{days:0.##}"));
        }

        if (absenceType.ApplyMinLeave && days < absenceType.MinLimitInDays)
        {
            throw new UserFriendlyException(L(
                "TotalLeaveDaysLessThanMinLimitInDays", $"{absenceType.MinLimitInDays}", $"{days:0.##}"));
        }
    }

    /// <summary>Legacy WFS 730-769: once only (<c>CanApplyAgain</c>), or not again within <c>CanApplyAfterDays</c>.</summary>
    private void EnsureRepeatRulesAllowIt(LeaveRuleContext context, AbsenceType absenceType, DateTime start)
    {
        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var sameType = _absenceRepository.GetAll()
                .Where(a => a.Id != context.ExcludeEmployeeAbsenceId
                            && a.EmployeeId == context.EmployeeId
                            && a.AbsenceTypeId == context.AbsenceTypeId
                            && (a.WorkflowStatus == null
                                || a.WorkflowStatus == RequestStatus.Approved
                                || a.WorkflowStatus == RequestStatus.Closed
                                || a.WorkflowStatus == RequestStatus.Open));

            if (!absenceType.CanApplyAgain)
            {
                var existing = sameType.Select(a => new { a.StartDate, a.EndDate }).FirstOrDefault();
                if (existing != null)
                {
                    throw new UserFriendlyException(L(
                        "LeaveTypeCanBeTakenOnlyOnce",
                        absenceType.NameEn,
                        existing.StartDate.ToString("dd/MM/yyyy"),
                        existing.EndDate.ToString("dd/MM/yyyy")));
                }

                return;
            }

            if (absenceType.CanApplyAfterDays <= 0)
            {
                return;
            }

            var previousEnd = sameType
                .Where(a => a.EndDate < start)
                .OrderByDescending(a => a.EndDate)
                .Select(a => (DateTime?)a.EndDate)
                .FirstOrDefault();

            if (previousEnd == null)
            {
                return;
            }

            var canApplyFrom = previousEnd.Value.Date.AddDays(absenceType.CanApplyAfterDays);

            if (start < canApplyFrom)
            {
                throw new UserFriendlyException(L(
                    "LeaveCannotBeAppliedAgainYet",
                    absenceType.NameEn,
                    $"{absenceType.CanApplyAfterDays:0.##}",
                    canApplyFrom.ToString("dd/MM/yyyy")));
            }
        }
    }

    /// <summary>Legacy WFS 773-786: a type that must be asked for a number of days in advance.</summary>
    private void EnsureNoticePeriodGiven(AbsenceType absenceType, DateTime start)
    {
        if (absenceType.CanApplyBeforeDays <= 0)
        {
            return;
        }

        var gap = (start - Clock.Now.Date).TotalDays;

        if (gap < absenceType.CanApplyBeforeDays)
        {
            throw new UserFriendlyException(L(
                "DaysGapLessThanCanApplyBeforeDays", $"{absenceType.CanApplyBeforeDays}", $"{gap:0.##}"));
        }
    }

    /// <summary>Legacy WFS 790-805: days of the week a type may not start on.</summary>
    private void EnsureStartDayIsAllowed(AbsenceType absenceType, DateTime start)
    {
        var blocked = start.DayOfWeek switch
        {
            DayOfWeek.Sunday => absenceType.DontAllowToStartOnSunday,
            DayOfWeek.Monday => absenceType.DontAllowToStartOnMonday,
            DayOfWeek.Tuesday => absenceType.DontAllowToStartOnTuesday,
            DayOfWeek.Wednesday => absenceType.DontAllowToStartOnWednesday,
            DayOfWeek.Thursday => absenceType.DontAllowToStartOnThursday,
            DayOfWeek.Friday => absenceType.DontAllowToStartOnFriday,
            _ => absenceType.DontAllowToStartOnSaturday,
        };

        if (blocked)
        {
            throw new UserFriendlyException(L("NotAllowedToStartLeaveOn", start.DayOfWeek.ToString()));
        }
    }

    /// <summary>
    /// The balance. For an accrual type it is the quota row, plus whatever negative balance the type allows,
    /// less what is already booked (legacy WFS 842-895). For a type with no quota row it is the pro-rata
    /// entitlement of each accrual period the leave touches, less the days already used in it (WFS 899-1103).
    /// </summary>
    private async Task EnsureQuotaIsAvailableAsync(
        LeaveRuleContext context, AbsenceType absenceType, DateTime start, DateTime end, double days)
    {
        var required = days + context.DeductFromQuota;

        var balances = await _leaveBalanceService.GetBalancesAsync(context.EmployeeId, start, context.ExcludeWorkflowRequestId);
        var balance = balances.FirstOrDefault(b => b.AbsenceTypeId == context.AbsenceTypeId);

        // A quota row is what makes a type quota-controlled here, rather than the type's HaveQuotaAccrual flag:
        // HCMv3 measures against BOOKABLE (balance less what is awaiting approval), which only exists where
        // there is a row. Types with no row fall through to legacy's non-accrual allowance below.
        if (balance != null)
        {
            var available = balance.Bookable + context.DaysAlreadyCounted;

            if (absenceType.EnableNegativeBalance)
            {
                available += await _accrualService.GetAvailableNegativeBalanceAsync(
                    context.EmployeeId,
                    absenceType.EnableNegativeBalance,
                    absenceType.NegativeBalanceLimit,
                    await YearlyQuotaAsync(context.EmployeeId, absenceType, start),
                    start);
            }

            if (required > available)
            {
                throw new UserFriendlyException(
                    L("NotEnoughBookableLeaveBalance", $"{available:0.##}", $"{required:0.##}"));
            }

            return;
        }

        await EnsureNonAccrualQuotaAsync(context, absenceType, start, end, days);
    }

    /// <summary>
    /// Legacy's non-accrual branch: the type carries an allowance per accrual period (the salary profile's
    /// <c>TotalDays</c> when there is one, otherwise the type's <c>TotalDaysAllowed</c>), pro-rated for a
    /// joiner, and what has already been taken in that period is subtracted. An allowance of zero means the
    /// type is unlimited and nothing is checked.
    /// </summary>
    private async Task EnsureNonAccrualQuotaAsync(
        LeaveRuleContext context, AbsenceType absenceType, DateTime start, DateTime end, double days)
    {
        var subGroupId = CurrentSubGroupId(context.EmployeeId, start);

        var allowed = subGroupId.HasValue
            ? _subGroupAbsenceRepository.GetAll()
                  .Where(a => a.EmployeeSubGroupId == subGroupId.Value
                              && a.AbsenceTypeId == context.AbsenceTypeId
                              && a.StartDate <= start
                              && a.EndDate >= start)
                  .Select(a => (double?)a.TotalDays)
                  .FirstOrDefault() ?? absenceType.TotalDaysAllowed
            : absenceType.TotalDaysAllowed;

        if (allowed <= 0)
        {
            return;
        }

        var employee = _employeeRepository.GetAll().First(e => e.Id == context.EmployeeId);

        var (periodStart, periodEnd) = await _accrualService.GetLeaveAccrualStartEndAsync(
            context.EmployeeId, context.AbsenceTypeId, end);

        var entitlement = await _accrualService.GetLeaveAccrualStartEndAsync(context.EmployeeId, context.AbsenceTypeId, start);
        var deductionStart = entitlement.DeductionStartDate < periodStart ? periodStart : entitlement.DeductionStartDate;

        var balance = _accrualService.GetProRataBasedBalanceQuota(
            employee.JoiningDate.Date,
            absenceType.AccrualPeriod,
            absenceType.LeaveEntitlement,
            allowed,
            deductionStart,
            periodEnd);

        double used;
        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            used = _absenceRepository.GetAll()
                .Where(a => a.Id != context.ExcludeEmployeeAbsenceId
                            && a.EmployeeId == context.EmployeeId
                            && a.AbsenceTypeId == context.AbsenceTypeId
                            && a.StartDate >= deductionStart
                            && a.EndDate <= periodEnd
                            && (a.WorkflowStatus == null
                                || a.WorkflowStatus == RequestStatus.Approved
                                || a.WorkflowStatus == RequestStatus.Closed
                                || a.WorkflowStatus == RequestStatus.Open))
                .Sum(a => (double?)a.TotalLeaveDays) ?? 0;
        }

        var available = balance - used;

        if (absenceType.EnableNegativeBalance)
        {
            available += absenceType.NegativeBalanceLimit;
        }

        if (days > available)
        {
            throw new UserFriendlyException(L(
                "NotEnoughQuotaForTheDeductionPeriod",
                absenceType.NameEn,
                deductionStart.ToString("dd/MM/yyyy"),
                periodEnd.ToString("dd/MM/yyyy"),
                $"{used:0.##}",
                $"{available:0.##}",
                $"{days:0.##}"));
        }
    }

    private async Task<double> YearlyQuotaAsync(int employeeId, AbsenceType absenceType, DateTime asOf)
    {
        var subGroupId = CurrentSubGroupId(employeeId, asOf);

        if (!subGroupId.HasValue)
        {
            return absenceType.TotalDaysAllowed;
        }

        var configured = _subGroupAbsenceRepository.GetAll()
            .Where(a => a.EmployeeSubGroupId == subGroupId.Value
                        && a.AbsenceTypeId == absenceType.Id
                        && a.StartDate <= asOf
                        && a.EndDate >= asOf)
            .Select(a => (double?)a.TotalDays)
            .FirstOrDefault();

        return await Task.FromResult(configured ?? absenceType.TotalDaysAllowed);
    }

    /// <summary>
    /// Legacy WFS 1112-1139: unpaid leave (type code "2") is refused while the employee still has annual leave
    /// (code "1") to take, when the setting says so.
    /// </summary>
    private async Task EnsureUnpaidIsAllowedAsync(LeaveRuleContext context, AbsenceType absenceType, DateTime start)
    {
        if (absenceType.Code != "2")
        {
            return;
        }

        var timeSettings = await _settings.GetTimeSettingsAsync();
        if (timeSettings?.RestrictUnpaidleaveIfAnnualleaveAvailable != true)
        {
            return;
        }

        var annualTypeId = _absenceTypeRepository.GetAll().Where(t => t.Code == "1").Select(t => (int?)t.Id).FirstOrDefault();
        if (annualTypeId == null)
        {
            return;
        }

        var balances = await _leaveBalanceService.GetBalancesAsync(context.EmployeeId, start, context.ExcludeWorkflowRequestId);
        var annual = balances.FirstOrDefault(b => b.AbsenceTypeId == annualTypeId.Value);

        if (annual != null && annual.Remaining > 0)
        {
            throw new UserFriendlyException(L("AnnualLeaveBalanceAvailableSoUnpaidLeaveIsNotAllowed", $"{annual.Remaining:0.##}"));
        }
    }

    /// <summary>Legacy WFS 1144-1163: a travel record over the same days.</summary>
    private void EnsureNoTravelInTheSameDays(LeaveRuleContext context, DateTime start, DateTime end)
    {
        var travel = _travelRepository.GetAll()
            .Where(t => t.EmployeeId == context.EmployeeId && t.StartDate <= end && t.EndDate >= start)
            .Select(t => new { t.StartDate, t.EndDate })
            .FirstOrDefault();

        if (travel != null)
        {
            throw new UserFriendlyException(L(
                "TravelEntryExistsForTheseDays",
                travel.StartDate.ToString("dd/MM/yyyy"),
                travel.EndDate.ToString("dd/MM/yyyy")));
        }
    }

    /// <summary>
    /// Legacy WFS 347-411: a type reserved for a termination notice period, and the general rule that leave
    /// cannot run past the last day in service unless the employee rejoined.
    /// </summary>
    private void EnsureSeparationRulesAllowIt(Employee employee, AbsenceType absenceType, DateTime start, DateTime end)
    {
        var separation = _separationRepository.GetAll()
            .Where(s => s.EmployeeId == employee.Id && s.RevokeDate == null)
            .OrderByDescending(s => s.SubmissionDate)
            .Select(s => new { s.SubmissionDate, s.SeparationType, s.LastDayInService })
            .FirstOrDefault();

        if (absenceType.AllowInTerminationNoticeOnly)
        {
            if (separation == null)
            {
                throw new UserFriendlyException(L("LeaveTypeOnlyDuringTerminationNoticeNoExitRecord", absenceType.NameEn));
            }

            if (separation.SeparationType == SeparationType.Resignation)
            {
                throw new UserFriendlyException(L("LeaveTypeOnlyDuringTerminationNoticeNotResignation", absenceType.NameEn));
            }

            if (!(separation.SubmissionDate.Date <= start && separation.LastDayInService.Date >= end))
            {
                throw new UserFriendlyException(L(
                    "LeaveTypeOnlyDuringNoticePeriod",
                    separation.SubmissionDate.ToString("dd/MM/yyyy"),
                    separation.LastDayInService.ToString("dd/MM/yyyy")));
            }

            return;
        }

        if (separation == null || separation.LastDayInService.Date >= end)
        {
            return;
        }

        var rejoined = _rejoinRepository.GetAll()
            .Any(r => r.EmployeeId == employee.Id
                      && r.RejoinDate > separation.LastDayInService
                      && r.RejoinDate <= end);

        if (!rejoined)
        {
            throw new UserFriendlyException(L(
                "LeaveCannotBeAppliedAfterLastDayInService",
                separation.LastDayInService.ToString("dd/MM/yyyy"),
                end.ToString("dd/MM/yyyy")));
        }
    }

    /// <summary>The employee sub group in force on the day, which carries the salary profile's leave settings.</summary>
    private int? CurrentSubGroupId(int employeeId, DateTime asOf)
    {
        return _orgAssignmentRepository.GetAll()
            .Where(a => a.EmployeeId == employeeId && a.StartDate <= asOf && a.EndDate >= asOf)
            .OrderByDescending(a => a.StartDate)
            .Select(a => a.EmployeeSubGroupId)
            .FirstOrDefault();
    }
}
