using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using HCMv3.PersonnelAdministration;
using HCMv3.TimeManagement;

namespace HCMv3.PayrollManagement.Services;

/// <inheritdoc cref="IEmployeeLeavePayCalculationService"/>
/// <remarks>
/// Ported from <c>HCM10.PayrollEngine/EmployeeLeavePayService.cs</c>: <c>Main</c> (line 17),
/// <c>GetLeavePay</c> (line 361), <c>GetLeaveDaysMonthWise</c> (line 832) and
/// <c>GetActualSickLeaveDays</c> (line 887). <c>Main_Simu</c> and its private clones (lines 959-1770) are
/// deliberately NOT ported -- wave P7 collapses them via <see cref="PayrollRegisterRef.Target"/>.
///
/// <para><b>Deviations and preserved quirks -- all deliberate:</b></para>
/// <list type="number">
/// <item><description><b>The <c>IsNotReturnFromLeave</c> arm is an EMPTY block in legacy</b> (lines 30-37:
/// the only two statements are commented out), with the entire body in its <c>else</c>. Net effect: when
/// <c>IsNotReturnFromLeave</c> is true, leave pay does nothing at all. Reproduced as an early
/// <c>return</c>.</description></item>
/// <item><description><b>Legacy mutates its own <c>offCyclePaymentDate</c>/<c>offCycleEndDate</c>/
/// <c>payEndDate</c> parameters mid-loop</b> (lines 94, 127, 136) and those mutations LEAK into subsequent
/// leave-type iterations and into <c>GetLeavePay</c>. <see cref="PayrollCalculationContext"/> is read-only, so
/// they are held in method-scoped locals seeded from the context -- same leakage,
/// preserved.</description></item>
/// <item><description><b>Legacy's <c>offCyclePaymentDate != DateTimeExtension.DBDtNull</c> sentinel</b>
/// (legacy line 485) is <c>1800-01-01</c> (legacy <c>HCM10.Module/BusinessObjects/Util/DateTimeExtension.cs</c>
/// line 14), assigned on Regular runs at legacy <c>Program.cs</c> line 207. This port's engine uses
/// <c>default(DateTime)</c> for the same "unset" state (see
/// <see cref="PayrollCalculationContext.OffCyclePaymentDate"/>), so BOTH sentinels are treated as unset --
/// see <see cref="IsPaymentDateSet"/>.</description></item>
/// <item><description><b>Legacy's raw SQL <c>Update EmployeeAbsence SET AdvanceLeavePaid = 1</c></b>
/// (legacy line 235) becomes a normal repository update. Legacy used raw SQL specifically to avoid firing
/// XAF's <c>NextTimeEvaluationFrom</c>/<c>EarliestMasterDataChange</c> side effects (see its 2024.10.05
/// comment); this port's <see cref="EmployeeAbsence"/> entity has no such side effects, so a plain update is
/// equivalent.</description></item>
/// <item><description><b><c>Program.AdvancePaymentDed</c> (wage code 2005) is NOT ported and THROWS</b> --
/// identical gap and reason as <see cref="EmployeeBasicPayCalculationService"/>; see
/// <see cref="AdvancePaymentDedAsync"/>.</description></item>
/// <item><description><b><c>GetActualSickLeaveDays</c> is fully ported but its employee-&gt;sub-group lookup
/// cannot resolve</b> -- see <see cref="ResolveEmployeeSubGroupId"/>. It degrades onto legacy's own
/// <c>empSubGroup == null</c> no-op arm rather than guessing.</description></item>
/// <item><description><b>No rounding.</b> Legacy's leave-pay body contains no <c>Math.Round</c>; rounding is
/// applied once later by <c>Program.SetRoundoff</c> (<see cref="IPayrollRoundingService"/>). The one
/// exception is inside <c>AdvancePaymentDed</c>, which is not ported.</description></item>
/// <item><description><b>Legacy swallows every exception inside <c>GetActualSickLeaveDays</c></b> (lines
/// 950-953, returning the uncorrected day count). Preserved verbatim, because several of the lookups it
/// performs legitimately return null on sparsely-configured tenants.</description></item>
/// </list>
/// </remarks>
public class EmployeeLeavePayCalculationService : HCMv3DomainServiceBase, IEmployeeLeavePayCalculationService, ITransientDependency
{
    private const string BasicPayWageCode = "1000";
    private const string SickLeaveUnpaidWageCode = "2003";
    private const string LeaveEncashmentWageCode = "1052";
    private const string AnnualLeaveAbsenceCode = "1";
    private const string SickLeaveAbsenceCode = "3";

    /// <summary>Legacy <c>DateTimeExtension.DBDtNull</c> (legacy Util file line 14).</summary>
    private static readonly DateTime LegacyDbDateNull = new DateTime(1800, 1, 1, 0, 0, 0);

    private readonly IRepository<AbsenceType> _absenceTypeRepository;
    private readonly IRepository<WageType> _wageTypeRepository;
    private readonly IRepository<WageTypeLeave> _wageTypeLeaveRepository;
    private readonly IRepository<EmployeeAbsence> _employeeAbsenceRepository;
    private readonly IRepository<EmployeeAbsAdjustment> _employeeAbsAdjustmentRepository;
    private readonly IRepository<EmployeeSubGroupAbsence> _employeeSubGroupAbsenceRepository;
    private readonly IRepository<AbsenceDedRuleDetail> _absenceDedRuleDetailRepository;
    private readonly IRepository<EmployeeDutyPeriod> _employeeDutyPeriodRepository;
    private readonly IRepository<TimeRegister> _timeRegisterRepository;
    private readonly IPayrollRegisterStore _registerStore;
    private readonly IPayrollDayCountService _dayCountService;
    private readonly IAccrualService _accrualService;
    private readonly TimeManagement.Services.IRosterService _rosterService;

    public EmployeeLeavePayCalculationService(
        IRepository<AbsenceType> absenceTypeRepository,
        IRepository<WageType> wageTypeRepository,
        IRepository<WageTypeLeave> wageTypeLeaveRepository,
        IRepository<EmployeeAbsence> employeeAbsenceRepository,
        IRepository<EmployeeAbsAdjustment> employeeAbsAdjustmentRepository,
        IRepository<EmployeeSubGroupAbsence> employeeSubGroupAbsenceRepository,
        IRepository<AbsenceDedRuleDetail> absenceDedRuleDetailRepository,
        IRepository<EmployeeDutyPeriod> employeeDutyPeriodRepository,
        IRepository<TimeRegister> timeRegisterRepository,
        IPayrollRegisterStore registerStore,
        IPayrollDayCountService dayCountService,
        IAccrualService accrualService,
        TimeManagement.Services.IRosterService rosterService)
    {
        _absenceTypeRepository = absenceTypeRepository;
        _wageTypeRepository = wageTypeRepository;
        _wageTypeLeaveRepository = wageTypeLeaveRepository;
        _employeeAbsenceRepository = employeeAbsenceRepository;
        _employeeAbsAdjustmentRepository = employeeAbsAdjustmentRepository;
        _employeeSubGroupAbsenceRepository = employeeSubGroupAbsenceRepository;
        _absenceDedRuleDetailRepository = absenceDedRuleDetailRepository;
        _employeeDutyPeriodRepository = employeeDutyPeriodRepository;
        _timeRegisterRepository = timeRegisterRepository;
        _registerStore = registerStore;
        _dayCountService = dayCountService;
        _accrualService = accrualService;
        _rosterService = rosterService;
    }

    #region << Legacy Program.cs lines 2666/2675-2676 (AbsenceTypeList + wtLeavePayWageList) >>

    /// <inheritdoc/>
    public Task<List<int>> GetLeavePayWageTypeIdsAsync()
    {
        var ids = GetLeaveAbsenceTypes().Select(r => r.WageTypeId.Value).ToList();

        ids.AddRange(_wageTypeRepository.GetAll()
            .Where(r => r.Code == LeaveEncashmentWageCode)
            .Select(r => r.Id)
            .ToList());

        return Task.FromResult(ids);
    }

    /// <summary>Legacy <c>Program.AbsenceTypeList</c> (legacy <c>Program.cs</c> line 2666): every
    /// <see cref="AbsenceType"/> that has a wage type.</summary>
    private List<AbsenceType> GetLeaveAbsenceTypes()
    {
        return _absenceTypeRepository.GetAll().Where(r => r.WageTypeId != null).ToList();
    }

    #endregion

    #region << Main (legacy EmployeeLeavePayService.cs line 17) >>

    /// <inheritdoc/>
    public async Task CalculateAsync(PayrollCalculationContext context)
    {
        if (context?.WageType == null)
        {
            throw new ArgumentException("PayrollCalculationContext.WageType must be set.", nameof(context));
        }

        // Legacy lines 30-39: the IsNotReturnFromLeave arm is an EMPTY block; everything lives in its else.
        if (context.IsNotReturnFromLeave)
        {
            return;
        }

        var payStartDate = new DateTime(context.PayDate.Year, context.PayDate.Month, 1);

        // Legacy mutates these three "parameters" mid-loop (lines 94, 127, 136) and the mutations leak.
        var payEndDate = context.PayDate;
        var offCyclePaymentDate = context.OffCyclePaymentDate;
        var offCycleEndDate = context.OffCycleEndDate;

        var leaveTypes = GetLeaveAbsenceTypes().Where(r => r.WageTypeId == context.WageType.Id).ToList();

        if (leaveTypes.Count > 0)
        {
            foreach (var leaveType in leaveTypes)
            {
                double leaveDays = 0;

                if (context.PayrollRunType == PayrollRunType.Regular)
                {
                    // Legacy lines 50-129.
                    var regularRows = RegularRows(context, payStartDate, payEndDate)
                        .Where(r => r.LeaveTypeId != null && r.LeaveTypeId == leaveType.Id)
                        .ToList();

                    leaveDays = regularRows.Sum(r => r.LeaveDay);

                    double includeInLeavePay = regularRows.Sum(r => r.IncludeInLeavePay);
                    if (includeInLeavePay != 0)
                    {
                        leaveDays += includeInLeavePay;
                    }

                    double fullMonthLeaveAdjDay = regularRows.Sum(r => r.LeaveAdjDay);
                    if (fullMonthLeaveAdjDay != 0)
                    {
                        leaveDays += fullMonthLeaveAdjDay;
                    }

                    // Legacy lines 87-128: leaves that spill into NEXT months, paid with this payroll.
                    var empAbsences = EmployeeAbsences(context)
                        .Where(r => r.AbsenceTypeId == leaveType.Id
                                    && r.LeavePaymentType == LeavePaymentType.PayFullWithPayroll
                                    && r.PaymentDate == context.PayDate)
                        .Select(r => new { r.StartDate, r.EndDate })
                        .Distinct()
                        .ToList();

                    foreach (var rec in empAbsences)
                    {
                        offCyclePaymentDate = context.PayDate;

                        // NOTE legacy line 97: "DayDate > payDate" -- strictly AFTER the pay date, so the
                        // in-month portion counted above is not double-counted. This one does NOT filter by
                        // LeaveType (legacy quirk); the two below DO.
                        leaveDays += OffCycleRows(context, rec.StartDate, rec.EndDate)
                            .Where(r => r.DayDate > context.PayDate)
                            .Sum(r => r.LeaveDay);

                        var spillRows = OffCycleRows(context, rec.StartDate, rec.EndDate)
                            .Where(r => r.LeaveTypeId != null && r.LeaveTypeId == leaveType.Id)
                            .ToList();

                        double includeInLeavePay2 = spillRows.Sum(r => r.IncludeInLeavePay);
                        if (includeInLeavePay2 != 0)
                        {
                            leaveDays += includeInLeavePay2;
                        }

                        double fullMonthLeaveAdjDay2 = spillRows.Sum(r => r.LeaveAdjDay);
                        if (fullMonthLeaveAdjDay2 != 0)
                        {
                            leaveDays += fullMonthLeaveAdjDay2;
                        }

                        offCycleEndDate = rec.EndDate;
                    }
                }
                else if (context.PayrollRunType == PayrollRunType.OffCycle)
                {
                    // Legacy lines 133-244.
                    if (context.Register.InPeriod == context.Register.ForPeriod)
                    {
                        payEndDate = offCycleEndDate;
                    }

                    if (context.IsEndOfService)
                    {
                        var rows = OffCycleRows(context, payStartDate, payEndDate)
                            .Where(r => r.LeaveTypeId != null && r.LeaveTypeId == leaveType.Id)
                            .ToList();

                        leaveDays = rows.Sum(r => r.LeaveDay);

                        double includeInLeavePay = rows.Sum(r => r.IncludeInLeavePay);
                        if (includeInLeavePay != 0)
                        {
                            leaveDays += includeInLeavePay;
                        }

                        double fullMonthLeaveAdjDay = rows.Sum(r => r.LeaveAdjDay);
                        if (fullMonthLeaveAdjDay != 0)
                        {
                            leaveDays += fullMonthLeaveAdjDay;
                        }
                    }

                    // Legacy lines 176-217: "Count LeaveDays" for advance-paid leaves.
                    var advanceAbsences = EmployeeAbsences(context)
                        .Where(r => r.AbsenceTypeId == leaveType.Id
                                    && r.LeavePaymentType == LeavePaymentType.PayInAdvance
                                    && r.PaymentDate == offCyclePaymentDate)
                        .ToList();

                    foreach (var rec in advanceAbsences.Select(r => new { r.StartDate, r.EndDate }).Distinct())
                    {
                        // Legacy line 185: lower bound is max(payStartDate, rec.StartDate); no LeaveType filter.
                        leaveDays += OffCycleRows(context, rec.StartDate, rec.EndDate)
                            .Where(r => r.DayDate >= payStartDate)
                            .Sum(r => r.LeaveDay);

                        var rows = OffCycleRows(context, rec.StartDate, rec.EndDate)
                            .Where(r => r.DayDate >= payStartDate
                                        && r.LeaveTypeId != null
                                        && r.LeaveTypeId == leaveType.Id)
                            .ToList();

                        double includeInLeavePay = rows.Sum(r => r.IncludeInLeavePay);
                        if (includeInLeavePay != 0)
                        {
                            leaveDays += includeInLeavePay;
                        }

                        double fullMonthLeaveAdjDay = rows.Sum(r => r.LeaveAdjDay);
                        if (fullMonthLeaveAdjDay != 0)
                        {
                            leaveDays += fullMonthLeaveAdjDay;
                        }
                    }

                    // Legacy lines 219-243: flag them as advance-paid.
                    foreach (var empAdvanceLeave in advanceAbsences)
                    {
                        if (!empAdvanceLeave.AdvanceLeavePaid)
                        {
                            empAdvanceLeave.AdvanceLeavePaid = true;
                            await _employeeAbsenceRepository.UpdateAsync(empAdvanceLeave);
                        }
                    }
                }

                // Legacy lines 247-283.
                if (leaveDays > 0)
                {
                    var actAbsenceType = leaveType;
                    var currAbsenceType = leaveType;

                    var currAbsenceWageType = GetWageTypeOfAbsenceTypeOrThrow(currAbsenceType);

                    if (currAbsenceWageType.Code == SickLeaveUnpaidWageCode)
                    {
                        // Legacy lines 256-260: swap to the AbsenceType whose CODE is "3" (sick leave).
                        currAbsenceType = _absenceTypeRepository.GetAll()
                            .FirstOrDefault(r => r.Code == SickLeaveAbsenceCode);

                        if (currAbsenceType == null)
                        {
                            throw new InvalidOperationException(
                                "AbsenceType with Code '3' (Sick Leave) not found; legacy "
                                + "EmployeeLeavePayService.cs line 259 requires it to redirect wage code 2003.");
                        }

                        currAbsenceWageType = GetWageTypeOfAbsenceTypeOrThrow(currAbsenceType);
                    }

                    var line = await GetOrCreateLineAsync(context, currAbsenceWageType);

                    var monthWiseDays = await GetLeavePayAsync(
                        context, currAbsenceType, currAbsenceWageType, leaveDays, context.PayDate, line,
                        offCycleEndDate, offCyclePaymentDate, isLeaveEncashment: false,
                        actAbsenceType: actAbsenceType);

                    // Legacy line 281: added unconditionally, no "zero means delete".
                    var saved = await _registerStore.SaveLineAsync(context.Register, line, deleteWhenAmountIsZero: false);
                    if (saved != null && monthWiseDays.Count > 0)
                    {
                        await _registerStore.SaveDayDetailsAsync(context.Register, saved.Id, monthWiseDays);
                    }
                }
            }
        }
        else if (context.WageType.Code == LeaveEncashmentWageCode)
        {
            // Legacy lines 289-352.
            if (context.PayrollRunType == PayrollRunType.OffCycle
                && context.Register.InPeriod == context.Register.ForPeriod)
            {
                // Legacy line 295 (2021.04.01, fractional leave) -- month END of offCycleEndDate, not the date.
                payEndDate = new DateTime(offCycleEndDate.Year, offCycleEndDate.Month,
                    DateTime.DaysInMonth(offCycleEndDate.Year, offCycleEndDate.Month));
            }

            var absenceType = _absenceTypeRepository.GetAll()
                .FirstOrDefault(r => r.Code == AnnualLeaveAbsenceCode);

            if (absenceType == null)
            {
                throw new InvalidOperationException(
                    "AbsenceType with Code '1' (Annual Leave) not found; legacy "
                    + "EmployeeLeavePayService.cs line 299 requires it for wage code 1052 (Leave Encashment).");
            }

            // Legacy lines 306-320: PayInAdvance-only when this is an advance/encashment off-cycle for the
            // off-cycle period itself; otherwise every Encashment/Fractional adjustment in the window.
            var advanceOnly = context.PayrollRunType == PayrollRunType.OffCycle
                              && context.Register.InPeriod == context.Register.ForPeriod
                              && (context.IsAdvanceLeave || context.IsLeaveEncashment);

            var annualLeaveAdjustments = _employeeAbsAdjustmentRepository.GetAll()
                .Where(r => r.EmployeeId == context.EmployeeId
                            && r.EffectiveDate >= payStartDate
                            && r.EffectiveDate <= payEndDate
                            && (r.AdjustmentType == AdjustmentType.Encashment
                                || r.AdjustmentType == AdjustmentType.Fractional))
                .ToList();

            // Legacy's "(AbsenceType.Code == "1" || AbsenceType.QuotaType == QuotaType.AnnualLeave)" OR --
            // both arms kept, they are NOT assumed to coincide (see PayrollEngineSettings.AnnualLeaveAbsenceTypeId).
            var annualLeaveAbsenceTypeIds = _absenceTypeRepository.GetAll()
                .Where(r => r.Code == AnnualLeaveAbsenceCode || r.QuotaType == QuotaType.AnnualLeave)
                .Select(r => r.Id)
                .ToList();

            var leaveEncashmentDays = annualLeaveAdjustments
                .Where(r => annualLeaveAbsenceTypeIds.Contains(r.AbsenceTypeId)
                            && (!advanceOnly || r.AdjustmentPaymentType == AdjustmentPaymentType.PayInAdvance))
                .Sum(r => r.QuotaAdjusted);

            if (leaveEncashmentDays != 0)
            {
                var line = await GetOrCreateLineAsync(context, context.WageType);

                var monthWiseDays = await GetLeavePayAsync(
                    context, absenceType, context.WageType, leaveEncashmentDays, context.PayDate, line,
                    offCycleEndDate, offCyclePaymentDate, isLeaveEncashment: true,
                    actAbsenceType: absenceType);

                // Legacy lines 344-349: delete when Amount == 0.
                var saved = await _registerStore.SaveLineAsync(context.Register, line, deleteWhenAmountIsZero: true);
                if (saved != null && monthWiseDays.Count > 0)
                {
                    await _registerStore.SaveDayDetailsAsync(context.Register, saved.Id, monthWiseDays);
                }
            }
        }
    }

    /// <summary>Legacy lines 262-270 / 324-332.</summary>
    private async Task<PayrollRegisterLine> GetOrCreateLineAsync(PayrollCalculationContext context, WageType wageType)
    {
        var line = await _registerStore.GetLineByWageTypeIdAsync(context.Register, wageType.Id);

        return line ?? new PayrollRegisterLine
        {
            WageTypeId = wageType.Id,
            WageCode = wageType.Code
        };
    }

    /// <summary>Legacy lines 252-253 -- <c>AbsenceType.WageType</c> is required.</summary>
    private WageType GetWageTypeOfAbsenceTypeOrThrow(AbsenceType absenceType)
    {
        if (absenceType.WageTypeId == null)
        {
            throw new InvalidOperationException(
                $"WageType not found for LeaveType : {absenceType.NameEn}");
        }

        var wageType = _wageTypeRepository.GetAll().FirstOrDefault(r => r.Id == absenceType.WageTypeId.Value);
        if (wageType == null)
        {
            throw new InvalidOperationException(
                $"WageType not found for LeaveType : {absenceType.NameEn}");
        }

        return wageType;
    }

    #endregion

    #region << GetLeavePay (legacy EmployeeLeavePayService.cs line 361) >>

    /// <summary>
    /// Legacy <c>GetLeavePay</c>. Returns the month-wise rows accumulated by legacy's
    /// <c>Program.AddSalaryRegisterDaysDetail</c> calls, for the caller to persist once the register line has
    /// an id (legacy could add them eagerly because XPO gave it an unsaved object reference).
    /// </summary>
    private async Task<List<SalaryRegisterDayCount>> GetLeavePayAsync(
        PayrollCalculationContext context,
        AbsenceType leaveType,
        WageType leaveTypeWageType,
        double leaveDays,
        DateTime payDate,
        PayrollRegisterLine line,
        DateTime offCycleEndDate,
        DateTime offCyclePaymentDate,
        bool isLeaveEncashment,
        AbsenceType actAbsenceType)
    {
        var monthWiseDays = new List<SalaryRegisterDayCount>();

        // Legacy line 367: "if (leaveDays != 0)", NOT "> 0" (the comment records the change).
        if (leaveDays == 0)
        {
            return monthWiseDays;
        }

        double totalleaveamount = 0;
        double totalleavedays = 0;
        var wtLeavePay = leaveTypeWageType;

        #region << Get LeavePay Rate (legacy lines 373-396) >>

        var includeInPaidLeaveWageTypeIds = _wageTypeLeaveRepository.GetAll()
            .Where(r => r.AbsenceTypeId == leaveType.Id)
            .Select(r => r.WageTypeId)
            .Distinct()
            .ToList();

        var registerLines = await _registerStore.GetLinesAsync(context.Register);
        var salRegWageTypes = registerLines
            .Where(r => includeInPaidLeaveWageTypeIds.Contains(r.WageTypeId))
            .ToList();

        double totalBasicPay = salRegWageTypes.Sum(r => r.Rate);

        if (isLeaveEncashment)
        {
            var settings = context.Settings;

            if (settings.RegularLeaveEncashOnBasicPayOnly
                && (context.PayrollRunType == PayrollRunType.Regular
                    || (context.PayrollRunType == PayrollRunType.OffCycle
                        && (context.IsAdvanceLeave || context.IsLeaveEncashment))))
            {
                totalBasicPay = salRegWageTypes.Where(r => r.WageCode == BasicPayWageCode).Sum(r => r.Rate);
            }
            else if (settings.FnFLeaveEncashOnBasicPayOnly
                     && context.Register.InPeriod == context.Register.ForPeriod
                     && context.PayrollRunType == PayrollRunType.OffCycle
                     && context.IsEndOfService)
            {
                totalBasicPay = salRegWageTypes.Where(r => r.WageCode == BasicPayWageCode).Sum(r => r.Rate);
            }
        }

        line.Rate = totalBasicPay;

        #endregion

        if (context.PayrollRunType == PayrollRunType.Regular)
        {
            #region << PayrollRunType = Regular (legacy lines 399-602) >>

            double regAmount = 0;

            if (!isLeaveEncashment)
            {
                // Legacy lines 404-566.
                var regStartDate = new DateTime(payDate.Year, payDate.Month, 1);
                var regEndDate = payDate;

                var rows = OffCycleRows(context, regStartDate, regEndDate)
                    .Where(r => r.LeaveTypeId != null && r.LeaveTypeId == leaveType.Id)
                    .ToList();

                leaveDays = rows.Sum(r => r.LeaveDay);

                double includeInLeavePay = rows.Sum(r => r.IncludeInLeavePay);
                if (includeInLeavePay != 0)
                {
                    leaveDays += includeInLeavePay;
                }

                double fullMonthLeaveAdjDay = rows.Sum(r => r.LeaveAdjDay);
                if (fullMonthLeaveAdjDay != 0)
                {
                    leaveDays += fullMonthLeaveAdjDay;
                }

                // Legacy lines 445-455: sick-leave-unpaid slab correction.
                if (actAbsenceType.Id != leaveType.Id
                    && GetWageTypeCodeOfAbsenceType(actAbsenceType) == SickLeaveUnpaidWageCode)
                {
                    var sickStart = regStartDate;
                    var sickEnd = MonthEnd(regStartDate);
                    leaveDays = await GetActualSickLeaveDaysAsync(
                        context, actAbsenceType, leaveDays, regStartDate, sickStart, sickEnd);
                }

                regAmount = await AmountForBillingMethodAsync(context, wtLeavePay, totalBasicPay, leaveDays, payDate, offCycleEndDate);

                // Legacy lines 484-566: leaves that are NOT in the same month (FullPayWithPayroll).
                if (IsPaymentDateSet(offCyclePaymentDate))
                {
                    AddMonthWiseInfo(context, line, monthWiseDays, PayrollRunType.Regular, payDate, leaveDays, regAmount);

                    // Legacy lines 497-498: move to the 1st of NEXT month.
                    payDate = payDate.AddMonths(1);
                    payDate = new DateTime(payDate.Year, payDate.Month, 1);

                    var recoveryPeriods = EmployeeBasicPayCalculationService.GetRecoveryPeriods(payDate, offCycleEndDate);

                    foreach (var rec in recoveryPeriods)
                    {
                        double recAmount = 0;

                        double leaveDaysInMonth = GetLeaveDaysMonthWise(
                            context, actAbsenceType, rec, offCyclePaymentDate);

                        // Legacy lines 511-516. NOTE it passes the OUTER `leaveDays`, not leaveDaysInMonth --
                        // preserved verbatim (the value is only used as the fallback return).
                        if (GetWageTypeCodeOfAbsenceType(actAbsenceType) == SickLeaveUnpaidWageCode)
                        {
                            leaveDaysInMonth = await GetActualSickLeaveDaysAsync(
                                context, actAbsenceType, leaveDays, rec, rec, MonthEnd(rec));
                        }

                        if (leaveDaysInMonth > 0)
                        {
                            recAmount = await AmountForBillingMethodAsync(
                                context, wtLeavePay, totalBasicPay, leaveDaysInMonth, rec, offCycleEndDate);

                            if (context.WageType.DontImpactNetPay == false)
                            {
                                await AdvancePaymentDedAsync(context, rec, recAmount);
                            }

                            AddMonthWiseInfo(context, line, monthWiseDays, PayrollRunType.Regular, rec, leaveDaysInMonth, recAmount);
                        }

                        totalleaveamount += recAmount;
                        totalleavedays += leaveDaysInMonth;
                    }
                }
            }
            else
            {
                // Legacy lines 571-597. NOTE: uses currWageType's BillingMethod/FixedDays, not wtLeavePay's.
                totalleaveamount = await AmountForBillingMethodAsync(
                    context, context.WageType, totalBasicPay, leaveDays, payDate, offCycleEndDate);
            }

            totalleaveamount += regAmount;
            totalleavedays += leaveDays;

            #endregion
        }
        else if (context.PayrollRunType == PayrollRunType.OffCycle)
        {
            #region << PayrollRunType = OffCycle (legacy lines 606-820) >>

            if (wtLeavePay.IncludeInAdvancePay || context.IsEndOfService)
            {
                if (!isLeaveEncashment)
                {
                    if (context.Register.InPeriod != context.Register.ForPeriod)
                    {
                        // Legacy lines 616-703 -- retro month.
                        var regStartDate = new DateTime(payDate.Year, payDate.Month, 1);
                        var regEndDate = payDate;

                        var rows = OffCycleRows(context, regStartDate, regEndDate)
                            .Where(r => r.LeaveTypeId != null && r.LeaveTypeId == leaveType.Id)
                            .ToList();

                        leaveDays = rows.Sum(r => r.LeaveDay);

                        double includeInLeavePay = rows.Sum(r => r.IncludeInLeavePay);
                        if (includeInLeavePay != 0)
                        {
                            leaveDays += includeInLeavePay;
                        }

                        double fullMonthLeaveAdjDay = rows.Sum(r => r.LeaveAdjDay);
                        if (fullMonthLeaveAdjDay != 0)
                        {
                            leaveDays += fullMonthLeaveAdjDay;
                        }

                        if (actAbsenceType.Id != leaveType.Id
                            && GetWageTypeCodeOfAbsenceType(actAbsenceType) == SickLeaveUnpaidWageCode)
                        {
                            leaveDays = await GetActualSickLeaveDaysAsync(
                                context, actAbsenceType, leaveDays, regStartDate, regStartDate, MonthEnd(regStartDate));
                        }

                        var regAmount = await AmountForBillingMethodAsync(
                            context, wtLeavePay, totalBasicPay, leaveDays, payDate, offCycleEndDate);

                        totalleaveamount += regAmount;
                        totalleavedays += leaveDays;

                        // Legacy line 697 -- guarded on InPeriod == ForPeriod, which is FALSE here by
                        // construction. Dead in legacy; reproduced dead.
                        AddMonthWiseInfo(context, line, monthWiseDays, PayrollRunType.OffCycle, payDate, leaveDays, regAmount);
                    }
                    else
                    {
                        // Legacy lines 707-775 -- off-cycle month, recovery periods.
                        var recoveryPeriods = EmployeeBasicPayCalculationService.GetRecoveryPeriods(payDate, offCycleEndDate);

                        foreach (var rec in recoveryPeriods)
                        {
                            double recAmount = 0;

                            double leaveDaysInMonth = GetLeaveDaysMonthWise(
                                context, actAbsenceType, rec, offCyclePaymentDate);

                            // Legacy lines 720-725. NOTE: NO "actAbsenceType != leaveType" guard here (unlike
                            // the Regular path), and it passes the outer leaveDays. Preserved verbatim.
                            if (GetWageTypeCodeOfAbsenceType(actAbsenceType) == SickLeaveUnpaidWageCode)
                            {
                                leaveDaysInMonth = await GetActualSickLeaveDaysAsync(
                                    context, actAbsenceType, leaveDays, rec, rec, MonthEnd(rec));
                            }

                            if (leaveDaysInMonth > 0)
                            {
                                recAmount = await AmountForBillingMethodAsync(
                                    context, wtLeavePay, totalBasicPay, leaveDaysInMonth, rec, offCycleEndDate);

                                if (context.WageType.DontImpactNetPay == false)
                                {
                                    await AdvancePaymentDedAsync(context, rec, recAmount);
                                }
                            }

                            totalleaveamount += recAmount;
                            totalleavedays += leaveDaysInMonth;

                            AddMonthWiseInfo(context, line, monthWiseDays, PayrollRunType.OffCycle, rec, leaveDaysInMonth, recAmount);
                        }
                    }
                }
                else
                {
                    // Legacy lines 781-817. Again currWageType, not wtLeavePay.
                    totalleaveamount = await AmountForBillingMethodAsync(
                        context, context.WageType, totalBasicPay, leaveDays, payDate, offCycleEndDate);

                    if (context.WageType.DontImpactNetPay == false)
                    {
                        await AdvancePaymentDedAsync(
                            context, new DateTime(payDate.Year, payDate.Month, 1, 0, 0, 0), totalleaveamount);
                    }

                    // Legacy line 816: ASSIGNMENT, not accumulation.
                    totalleavedays = leaveDays;
                }
            }

            #endregion
        }

        // Legacy lines 823-824.
        line.Number += totalleavedays;
        line.Amount += totalleaveamount;

        return monthWiseDays;
    }

    /// <summary>
    /// The three-way BillingMethod switch legacy repeats seven times verbatim (legacy lines 457-480, 520-543,
    /// 573-596, 668-691, 729-752, 783-806). <c>FixedAmountPerMonth</c> has no arm in any copy -- the amount
    /// simply stays 0. Preserved.
    /// </summary>
    private async Task<double> AmountForBillingMethodAsync(
        PayrollCalculationContext context,
        WageType wageType,
        double totalBasicPay,
        double leaveDays,
        DateTime monthSeed,
        DateTime offCycleEndDate)
    {
        if (wageType.BillingMethod == BillingMethod.FixedDaysPerMonth)
        {
            return (totalBasicPay / wageType.FixedDays) * leaveDays;
        }

        if (wageType.BillingMethod == BillingMethod.CalendarDaysPerMonth)
        {
            double calendarDays = DateTime.DaysInMonth(monthSeed.Year, monthSeed.Month);
            return (totalBasicPay / calendarDays) * leaveDays;
        }

        if (wageType.BillingMethod == BillingMethod.WorkingDaysPerMonth)
        {
            double calendarDays = DateTime.DaysInMonth(monthSeed.Year, monthSeed.Month);
            double weeklyOffDays = await GetActualWeekOffsAsync(context, monthSeed);
            double salaryBasisDays = calendarDays - weeklyOffDays;
            return (totalBasicPay / salaryBasisDays) * leaveDays;
        }

        return 0;
    }

    #endregion

    #region << GetLeaveDaysMonthWise (legacy EmployeeLeavePayService.cs line 832) >>

    private double GetLeaveDaysMonthWise(
        PayrollCalculationContext context,
        AbsenceType leaveType,
        DateTime payDate,
        DateTime offCyclePaymentDate)
    {
        double leaveDaysInMonth = 0;

        var payStartDate = payDate;
        var payEndDate = MonthEnd(payDate);

        var expectedPaymentType = context.PayrollRunType == PayrollRunType.OffCycle
            ? LeavePaymentType.PayInAdvance
            : LeavePaymentType.PayFullWithPayroll;

        var empAbsences = EmployeeAbsences(context)
            .Where(r => r.AbsenceTypeId == leaveType.Id
                        && r.LeavePaymentType == expectedPaymentType
                        && r.PaymentDate == offCyclePaymentDate)
            .Select(r => new { r.StartDate, r.EndDate })
            .Distinct()
            .ToList();

        foreach (var empAbs in empAbsences)
        {
            var start = empAbs.StartDate > payStartDate ? empAbs.StartDate : payStartDate;
            var end = empAbs.EndDate < payEndDate ? empAbs.EndDate : payEndDate;

            var rows = OffCycleRows(context, start, end);

            leaveDaysInMonth += rows.Sum(r => r.LeaveDay);

            // Legacy lines 857-869 / 871-883: these two DO require a LeaveType (any), unlike the LeaveDay sum.
            double includeInLeavePay = rows.Where(r => r.LeaveTypeId != null).Sum(r => r.IncludeInLeavePay);
            if (includeInLeavePay != 0)
            {
                leaveDaysInMonth += includeInLeavePay;
            }

            double fullMonthLeaveAdjDay = rows.Where(r => r.LeaveTypeId != null).Sum(r => r.LeaveAdjDay);
            if (fullMonthLeaveAdjDay != 0)
            {
                leaveDaysInMonth += fullMonthLeaveAdjDay;
            }
        }

        return leaveDaysInMonth;
    }

    #endregion

    #region << GetActualSickLeaveDays (legacy EmployeeLeavePayService.cs line 887) >>

    /// <summary>
    /// Legacy <c>GetActualSickLeaveDays</c>: re-values sick-leave days through the employee sub-group's
    /// <c>AbsenceDeductionRule</c> slabs, so that a day falling in a 50%-deduction slab counts as 0.5 paid
    /// days.
    ///
    /// <para>Legacy wraps the whole body in <c>try { } catch { }</c> and returns the UNCORRECTED
    /// <paramref name="leaveDays"/> on any failure (legacy lines 950-953). Preserved verbatim.</para>
    /// </summary>
    private async Task<double> GetActualSickLeaveDaysAsync(
        PayrollCalculationContext context,
        AbsenceType currAbsenceType,
        double leaveDays,
        DateTime payDate,
        DateTime payStartDate,
        DateTime payEndDate)
    {
        double retLeaveDays = leaveDays;

        try
        {
            if (GetWageTypeCodeOfAbsenceType(currAbsenceType) != SickLeaveUnpaidWageCode)
            {
                return retLeaveDays;
            }

            var employeeSubGroupId = ResolveEmployeeSubGroupId(context, payDate);
            if (employeeSubGroupId == null)
            {
                // Legacy's own "empSubGroup == null" arm (line 897): returns leaveDays unchanged.
                return retLeaveDays;
            }

            var sickLeaveAbsenceTypeIds = _absenceTypeRepository.GetAll()
                .Where(r => r.Code == SickLeaveAbsenceCode || r.QuotaType == QuotaType.SickLeave)
                .Select(r => r.Id)
                .ToList();

            var empSubGroupAbsence = _employeeSubGroupAbsenceRepository.GetAll()
                .Where(r => r.StartDate <= payDate
                            && r.EndDate >= payDate
                            && r.EmployeeSubGroupId == employeeSubGroupId.Value
                            && sickLeaveAbsenceTypeIds.Contains(r.AbsenceTypeId))
                .FirstOrDefault();

            if (empSubGroupAbsence?.AbsenceDeductionRuleId == null)
            {
                return retLeaveDays;
            }

            // Legacy line 906: swap in the ACTUAL sick-leave absence type before resolving the accrual window.
            var actualSickAbsenceTypeId = empSubGroupAbsence.AbsenceTypeId;

            var absRuleDetailList = _absenceDedRuleDetailRepository.GetAll()
                .Where(r => r.AbsenceDeductionRuleId == empSubGroupAbsence.AbsenceDeductionRuleId.Value
                            && r.StartDate <= payDate
                            && r.EndDate >= payDate)
                .OrderBy(r => r.DaysCount)
                .ToList();

            var (deductionStartDate, _) = await _accrualService.GetLeaveAccrualStartEndAsync(
                context.EmployeeId, actualSickAbsenceTypeId, payDate);

            // Legacy lines 920-923: unpaid sick days already consumed BEFORE this period.
            double totalSickLeaveUnpaidDay = OffCycleRowsExclusiveEnd(context, deductionStartDate, payStartDate)
                .Sum(r => r.SickLeaveUnpaidDay);

            double sickLeavePaidDays = 0;
            double currMonthSickLeaveUnpaidDays = OffCycleRows(context, payStartDate, payEndDate)
                .Where(r => r.SickLeaveUnpaidDay > 0)
                .Sum(r => r.SickLeaveUnpaidDay);

            // Legacy lines 931-941: walk day by day up the slab table. NOTE the loop decrements by a whole 1
            // while the running total may be fractional, so a fractional day count never reaches exactly 0 --
            // it goes negative and the "!= 0" test keeps it running until `dedrule` is null and it breaks.
            // Preserved verbatim; do not "improve" to > 0.
            while (currMonthSickLeaveUnpaidDays != 0)
            {
                totalSickLeaveUnpaidDay += 1;
                var dedrule = absRuleDetailList.FirstOrDefault(r => r.DaysCount >= totalSickLeaveUnpaidDay);
                if (dedrule != null)
                {
                    sickLeavePaidDays += 1 - (dedrule.DeductionNumber / 100);
                }
                else
                {
                    break;
                }

                currMonthSickLeaveUnpaidDays -= 1;
            }

            retLeaveDays = sickLeavePaidDays;
        }
        catch (Exception)
        {
            // Legacy lines 950-953: exception swallowed, uncorrected value returned.
            return leaveDays;
        }

        return retLeaveDays;
    }

    /// <summary>
    /// Legacy <c>Program.empOrgAssignmentList.Where(StartDate &lt;= payDate &amp;&amp; EndDate &gt;= payDate)
    /// .Select(r =&gt; r.EmployeeSubGroup).FirstOrDefault()</c> (legacy
    /// <c>EmployeeLeavePayService.cs</c> lines 895-896).
    ///
    /// <para><b>GAP -- always returns <c>null</c>.</b> This port's <see cref="EmployeeOrgAssignment"/> has NO
    /// <c>EmployeeSubGroupId</c> FK: it was deferred in Wave B (see that entity's class remarks, "DEFERRED ...
    /// <c>EmployeeGroup</c>/<c>EmployeeSubGroup</c>/<c>PayrollArea</c>"). The same gap is already documented
    /// on <see cref="IAccrualService"/>, <see cref="IIndemnityCalculationService"/> and
    /// <c>TimeEngineRunInput.EmployeeSubGroupIds</c>, all of which resolved it by making the sub-group a
    /// CALLER-SUPPLIED parameter. <see cref="PayrollCalculationContext"/> carries no such member and is frozen
    /// for this wave.</para>
    ///
    /// <para>Returning <c>null</c> is not a fabricated behaviour: it lands exactly on legacy's own
    /// <c>if (empSubGroup != null)</c> guard (legacy line 897), whose false arm returns the day count
    /// uncorrected. <b>Effect while the gap is open: the sick-leave-unpaid slab correction never applies</b>,
    /// so 2003-family leave is valued at raw evaluated days.</para>
    ///
    /// <para><b>To close it:</b> add <c>EmployeeSubGroupId</c> to
    /// <see cref="PayrollCalculationContext"/> (engine-resolved, per employee) or to
    /// <see cref="EmployeeOrgAssignment"/> (+ migration), then replace this method's body with the
    /// date-bounded lookup. Reported, not made.</para>
    /// </summary>
    private static int? ResolveEmployeeSubGroupId(PayrollCalculationContext context, DateTime payDate)
    {
        _ = context;
        _ = payDate;
        return null;
    }

    #endregion

    #region << Helpers >>

    /// <summary>Legacy <c>empAbsence</c> (legacy <c>Program.cs</c> line 1856): all of the employee's absences.</summary>
    private IQueryable<EmployeeAbsence> EmployeeAbsences(PayrollCalculationContext context)
    {
        return _employeeAbsenceRepository.GetAll().Where(r => r.EmployeeId == context.EmployeeId);
    }

    /// <summary>Legacy <c>empTimeRegisterList</c> (the Regular-run cache; <c>null</c> on off-cycle runs).</summary>
    private static List<TimeRegister> RegularRows(PayrollCalculationContext context, DateTime startDate, DateTime endDate)
    {
        if (context.TimeRegisterList == null)
        {
            return new List<TimeRegister>();
        }

        return context.TimeRegisterList
            .Where(r => r.EmployeeId == context.EmployeeId && r.DayDate >= startDate && r.DayDate <= endDate)
            .ToList();
    }

    /// <summary>Legacy <c>empTimeRegister</c> (the unfiltered IQueryable; every consumer re-applies its own predicate).</summary>
    private List<TimeRegister> OffCycleRows(PayrollCalculationContext context, DateTime startDate, DateTime endDate)
    {
        var query = context.TimeRegisterQuery ?? _timeRegisterRepository.GetAll();

        return query
            .Where(r => r.EmployeeId == context.EmployeeId && r.DayDate >= startDate && r.DayDate <= endDate)
            .ToList();
    }

    /// <summary>Legacy line 922 uses a strict <c>&lt;</c> upper bound; kept separate to avoid an off-by-one day.</summary>
    private List<TimeRegister> OffCycleRowsExclusiveEnd(PayrollCalculationContext context, DateTime startDate, DateTime endDateExclusive)
    {
        var query = context.TimeRegisterQuery ?? _timeRegisterRepository.GetAll();

        return query
            .Where(r => r.EmployeeId == context.EmployeeId && r.DayDate >= startDate && r.DayDate < endDateExclusive)
            .ToList();
    }

    private string GetWageTypeCodeOfAbsenceType(AbsenceType absenceType)
    {
        if (absenceType?.WageTypeId == null)
        {
            return null;
        }

        return _wageTypeRepository.GetAll()
            .Where(r => r.Id == absenceType.WageTypeId.Value)
            .Select(r => r.Code)
            .FirstOrDefault();
    }

    /// <summary>
    /// Legacy <c>offCyclePaymentDate != DateTimeExtension.DBDtNull</c> (legacy line 485). Legacy's "unset"
    /// sentinel is 1800-01-01 (assigned on Regular runs at legacy <c>Program.cs</c> line 207); this port's
    /// engine uses <c>default(DateTime)</c>. Both are treated as unset.
    /// </summary>
    private static bool IsPaymentDateSet(DateTime offCyclePaymentDate)
    {
        return offCyclePaymentDate != default && offCyclePaymentDate != LegacyDbDateNull;
    }

    /// <summary>
    /// Legacy <c>Program.AddSalaryRegisterDaysDetail</c> (legacy <c>Program.cs</c> line 5439) plus its
    /// caller-side guard. <paramref name="expectedRunType"/> reproduces legacy's per-call-site
    /// <c>payrollRunType == X</c> literal (Regular at legacy lines 490/553, OffCycle at 697/767) -- the two
    /// blocks are never both live, so the literal is redundant in legacy but kept here for traceability.
    /// </summary>
    private static void AddMonthWiseInfo(
        PayrollCalculationContext context,
        PayrollRegisterLine line,
        List<SalaryRegisterDayCount> monthWiseDays,
        PayrollRunType expectedRunType,
        DateTime periodSeed,
        double number,
        double amount)
    {
        if (context.PayrollRunType != expectedRunType
            || context.Register.InPeriod != context.Register.ForPeriod
            || amount == 0)
        {
            return;
        }

        monthWiseDays.Add(new SalaryRegisterDayCount
        {
            Period = MonthEnd(periodSeed),
            Rate = line.Rate,
            Number = number,
            Amount = amount
        });
    }

    /// <summary>
    /// Ported from legacy <c>Program.GetActualWeekOffs</c> (legacy <c>Program.cs</c> line 2858). Identical
    /// body to <c>EmployeeBasicPayCalculationService.GetActualWeekOffsAsync</c> and to
    /// <c>PayrollDayCountService</c>'s private <c>GetPayrollActualWeekOffsAsync</c>; see the basic-pay copy's
    /// remarks for why it is duplicated rather than shared in this wave.
    /// </summary>
    private async Task<double> GetActualWeekOffsAsync(PayrollCalculationContext context, DateTime payDate)
    {
        var startDate = new DateTime(payDate.Year, payDate.Month, 1);
        var endDate = MonthEnd(payDate);
        double daysInMonth = DateTime.DaysInMonth(payDate.Year, payDate.Month);

        var query = context.TimeRegisterQuery ?? _timeRegisterRepository.GetAll();
        var daysWithTimeRegister = query
            .Where(r => r.EmployeeId == context.EmployeeId && r.DayDate >= startDate && r.DayDate <= endDate)
            .Select(r => r.DayDate)
            .ToList();

        var weeklyOffDays = await _dayCountService.GetWeeklyOffDaysAsync(
            context.EmployeeId, endDate, context.PayrollRunType, endDate, context.OffCycleReason);

        if (Math.Abs(daysInMonth - daysWithTimeRegister.Count) > double.Epsilon)
        {
            List<EmployeeDutyPeriod> employeeDutyPeriods = null;

            for (var dayDate = startDate; dayDate <= endDate; dayDate = dayDate.AddDays(1))
            {
                if (daysWithTimeRegister.Contains(dayDate))
                {
                    continue;
                }

                if (context.Settings.IsRosterActive)
                {
                    var rosterDuty = await _rosterService.GetRosterDutyAsync(context.EmployeeId, dayDate, null, false);
                    weeklyOffDays += rosterDuty != null && rosterDuty.IsWeeklyOff ? 1 : 0;
                    continue;
                }

                employeeDutyPeriods ??= _employeeDutyPeriodRepository.GetAll()
                    .Where(r => r.EmployeeId == context.EmployeeId)
                    .ToList();

                var dutyPeriod = employeeDutyPeriods
                    .Where(r => r.StartDate <= dayDate && r.EndDate >= dayDate)
                    .Select(r => r.DutyPeriod)
                    .FirstOrDefault();

                if (dutyPeriod == null)
                {
                    throw new InvalidOperationException(
                        $"DutyPeriod not Found for Employee Id '{context.EmployeeId}' For '{dayDate:dd/MM/yyyy}'");
                }

                var duty = GetDutyForDayOfWeek(dutyPeriod, dayDate);
                weeklyOffDays += duty != null && duty.IsWeeklyOff ? 1 : 0;
            }
        }

        return weeklyOffDays;
    }

    private static Duty GetDutyForDayOfWeek(DutyPeriod dutyPeriod, DateTime dayDate)
    {
        return dayDate.DayOfWeek switch
        {
            DayOfWeek.Sunday => dutyPeriod.SundayDuty,
            DayOfWeek.Monday => dutyPeriod.MondayDuty,
            DayOfWeek.Tuesday => dutyPeriod.TuesdayDuty,
            DayOfWeek.Wednesday => dutyPeriod.WednesdayDuty,
            DayOfWeek.Thursday => dutyPeriod.ThursdayDuty,
            DayOfWeek.Friday => dutyPeriod.FridayDuty,
            _ => dutyPeriod.SaturdayDuty
        };
    }

    private static DateTime MonthEnd(DateTime date)
    {
        return new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));
    }

    /// <summary>
    /// <b>NOT PORTED -- THROWS.</b> Identical gap to
    /// <c>EmployeeBasicPayCalculationService.AdvancePaymentDedAsync</c>: legacy
    /// <c>Program.AdvancePaymentDed</c> (legacy <c>Program.cs</c> line 2940) upserts an
    /// <see cref="EmployeePayment"/> row for wage code
    /// <see cref="PayrollEngineWageCodes.AdvancePaymentDeduction"/> keyed by
    /// <c>(Employee, PaymentDate, OffCyclePaymentDate, WageType)</c>, and this port's
    /// <see cref="EmployeePayment"/> dropped <c>OffCyclePaymentDate</c>. Legacy's two early-exit arms
    /// (<c>amount == 0</c>, and off-cycle End-Of-Service) ARE reproduced so no legacy no-op path throws.
    /// </summary>
    private static Task AdvancePaymentDedAsync(PayrollCalculationContext context, DateTime paymentDate, double amount)
    {
        if (amount == 0)
        {
            return Task.CompletedTask;
        }

        if (context.PayrollRunType == PayrollRunType.OffCycle && context.IsEndOfService)
        {
            return Task.CompletedTask;
        }

        throw new NotImplementedException(
            "Program.AdvancePaymentDed (HCM10.PayrollEngine/Program.cs line 2940) is not ported: "
            + "HCMv3's EmployeePayment entity dropped the OffCyclePaymentDate field that forms part of the "
            + "legacy upsert key for wage code 2005. Add EmployeePayment.OffCyclePaymentDate + "
            + "IsSystemGenerated (Core + migration) and implement the upsert. "
            + $"(employeeId={context.EmployeeId}, paymentDate={paymentDate:dd/MM/yyyy}, amount={amount})");
    }

    #endregion
}
