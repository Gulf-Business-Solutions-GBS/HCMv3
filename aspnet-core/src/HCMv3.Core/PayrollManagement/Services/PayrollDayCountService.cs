using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings.Services;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Services;

namespace HCMv3.PayrollManagement.Services;

/// <inheritdoc cref="IPayrollDayCountService"/>
/// <remarks>
/// Ported from <c>HCM10.PayrollEngine/Program.cs</c>, region <c>&lt;&lt; Time Types Balances Calculation &gt;&gt;</c>.
///
/// <para><b>Deviations from the legacy source, all deliberate:</b></para>
/// <list type="number">
/// <item><description><b>Static process-wide caches removed.</b> Legacy hoisted <c>empTimeRegister</c>,
/// <c>empTimeRegisterList</c>, <c>empContractList</c>, <c>empDutyPeriodList</c>, <c>WageTypeList</c> and
/// <c>prevSalRegList</c> into <c>static</c> fields refreshed once per employee by <c>Main()</c>. Each method
/// here re-queries via injected <c>IRepository&lt;T&gt;</c>. Collapsing the two TimeRegister caches into one
/// query is behaviour-preserving for seven of the nine methods; <see cref="GetPresentDaysAsync"/> and
/// <see cref="GetAbsentDaysAsync"/> are the exceptions -- see the interface remarks for the exact bounds and
/// for the one KNOWN, UNRESOLVED divergence (cut-off absence days reaching into the previous
/// month).</description></item>
/// <item><description><b>Static setting mirrors removed.</b> Legacy's <c>GetSysSettings()</c> copied ~15
/// settings into <c>static bool</c>/<c>static int</c> fields. Each method here reads
/// <see cref="ISysSettingsAccessor"/> per call. When the tenant has no settings row yet the accessor returns
/// <c>null</c>, and this service falls back to the legacy field initialisers (all <c>false</c>, except
/// <c>enableBasicpayMoreThenFixeddays = true</c> at line 117).</description></item>
/// <item><description><b><c>partialPeriodCutoffDayInFixedDaysBilling</c> clamp is now local.</b> Legacy line
/// 4665 <i>overwrote the static field</i> when the configured cut-off day exceeded the days in the month,
/// permanently lowering it for every subsequent employee/month in the same process run -- a latent bug. This
/// port clamps into a local variable so the configured value is honoured for every period.</description></item>
/// <item><description><b><c>calculateSalRegDaysDetail</c> gate dropped.</b> Legacy line 131 declares it
/// <c>= true</c> and never assigns it anywhere (verified by full-file search), so the line-4825 condition is a
/// constant-true conjunct and is not ported as a parameter.</description></item>
/// <item><description><b><c>TimeRegister.LeaveType</c> navigation absent.</b> This port's
/// <see cref="TimeRegister"/> carries only a bare <c>LeaveTypeId</c> (see that entity's own remarks), so
/// legacy's <c>r.LeaveType.IsPaid</c> / <c>r.LeaveType.NameEn</c> dereferences are resolved by loading the
/// referenced <see cref="AbsenceType"/> rows into a lookup dictionary.</description></item>
/// <item><description><b><c>emp.Absence</c> navigation absent.</b> Replaced by an
/// <c>IRepository&lt;EmployeeAbsence&gt;</c> query filtered by <c>EmployeeId</c> (same adaptation as
/// <see cref="AccrualService"/>).</description></item>
/// <item><description><b><c>DateTimeExtension.DBDtNull</c></b> (legacy's <c>1800-01-01</c> "null date"
/// sentinel) is preserved as the private <c>LegacyDbDateNull</c> constant, because
/// <c>GetPresentDays</c> compares against it directly (line 4660).</description></item>
/// <item><description><b>Roster duty resolution.</b> Legacy's <c>RosterService.GetRosterDuty(session, oid,
/// date, null)</c> becomes <see cref="IRosterService.GetRosterDutyAsync"/> with
/// <c>throwException: false</c> -- both legacy call sites (lines 4862 and 5150) guard with
/// <c>duty != null ? 1 : 0</c>, i.e. they treat an unresolvable duty as "not a weekly off" rather than as an
/// error.</description></item>
/// <item><description><b>The duty-fallback loop is intentionally duplicated.</b>
/// <c>AccrualService.CalculateMissingDayWeeklyOffsAsync</c> implements a near-identical loop but is
/// <c>private</c> and, more importantly, applies the <c>EmployeeSubstituteDuty</c> override that legacy's
/// <c>Program.GetActualWeekOffs</c>/<c>GetNonWorkingWeekOffs</c> do NOT apply. Delegating would silently
/// change payroll numbers for substituted days, so the payroll variant is ported as its own private helper
/// with substitute-duty resolution deliberately omitted, exactly as legacy.</description></item>
/// <item><description><b><c>throw ex;</c> wrappers dropped.</b> Every legacy method wrapped its body in
/// <c>try { ... } catch (Exception ex) { throw ex; }</c>, which only destroys the stack trace. Omitted.</description></item>
/// </list>
/// </remarks>
[UnitOfWork]
public class PayrollDayCountService : HCMv3DomainServiceBase, IPayrollDayCountService
{
    /// <summary>Legacy wage type 4014 "Present Days".</summary>
    private const string PresentDaysWageTypeCode = "4014";

    /// <summary>Legacy wage type 4008 "Weekly Off Days".</summary>
    private const string WeeklyOffWageTypeCode = "4008";

    /// <summary>Legacy wage type 4002 "Paid Leave Days" (a <see cref="GetPaidDaysAsync"/> summand).</summary>
    private const string PaidLeaveDaysWageTypeCode = "4002";

    /// <summary>Legacy wage type 4000 (a <see cref="GetPaidDaysAsync"/> summand).</summary>
    private const string FirstTimeTypeWageTypeCode = "4000";

    /// <summary>Legacy <c>DateTimeExtension.DBDtNull</c> (<c>HCM10.Module/BusinessObjects/Util/DateTimeExtension.cs</c> line 14).</summary>
    private static readonly DateTime LegacyDbDateNull = new DateTime(1800, 1, 1, 0, 0, 0);

    /// <summary>Legacy <c>Program.apply_Annualleavenoimpact_Logic</c>'s magic leave-type name (line 4757).</summary>
    private const string AnnualLeaveNoImpactIndemnityName = "Annual Leave (No impact indemnity)";

    private readonly IRepository<TimeRegister> _timeRegisterRepository;
    private readonly IRepository<WageType> _wageTypeRepository;
    private readonly IRepository<AbsenceType> _absenceTypeRepository;
    private readonly IRepository<SalaryRegister> _salaryRegisterRepository;
    private readonly IRepository<SalaryRegisterDetail> _salaryRegisterDetailRepository;
    private readonly IRepository<PayrollArea> _payrollAreaRepository;
    private readonly IRepository<EmployeeContract> _employeeContractRepository;
    private readonly IRepository<EmployeeAbsence> _employeeAbsenceRepository;
    private readonly IRepository<EmployeeDutyPeriod> _employeeDutyPeriodRepository;
    private readonly IRepository<EmployeeTimePolicy> _employeeTimePolicyRepository;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;
    private readonly IRosterService _rosterService;

    public PayrollDayCountService(
        IRepository<TimeRegister> timeRegisterRepository,
        IRepository<WageType> wageTypeRepository,
        IRepository<AbsenceType> absenceTypeRepository,
        IRepository<SalaryRegister> salaryRegisterRepository,
        IRepository<SalaryRegisterDetail> salaryRegisterDetailRepository,
        IRepository<PayrollArea> payrollAreaRepository,
        IRepository<EmployeeContract> employeeContractRepository,
        IRepository<EmployeeAbsence> employeeAbsenceRepository,
        IRepository<EmployeeDutyPeriod> employeeDutyPeriodRepository,
        IRepository<EmployeeTimePolicy> employeeTimePolicyRepository,
        ISysSettingsAccessor sysSettingsAccessor,
        IRosterService rosterService)
    {
        _timeRegisterRepository = timeRegisterRepository;
        _wageTypeRepository = wageTypeRepository;
        _absenceTypeRepository = absenceTypeRepository;
        _salaryRegisterRepository = salaryRegisterRepository;
        _salaryRegisterDetailRepository = salaryRegisterDetailRepository;
        _payrollAreaRepository = payrollAreaRepository;
        _employeeContractRepository = employeeContractRepository;
        _employeeAbsenceRepository = employeeAbsenceRepository;
        _employeeDutyPeriodRepository = employeeDutyPeriodRepository;
        _employeeTimePolicyRepository = employeeTimePolicyRepository;
        _sysSettingsAccessor = sysSettingsAccessor;
        _rosterService = rosterService;
    }

    // NOTE: HCMv3.Core has no reference to EntityFrameworkCore (architecture rule), so the EF async
    // IQueryable extensions are unavailable. All queries use the plain synchronous IQueryable from
    // IRepository<T>.GetAll(); public methods are still Task-returning for interface consistency.
    // Same pattern as AccrualService (see its note above GetAsOnDateQuotaAsync).

    #region << GetPresentDays (legacy Program.cs line 4555) >>

    public async Task<PresentDaysResult> GetPresentDaysAsync(
        int employeeId,
        DateTime payStartDate,
        DateTime payEndDate,
        int salaryRegisterId,
        PayrollRunType payrollRunType,
        DateTime offCycleEndDate,
        string offCycleReason)
    {
        var result = new PresentDaysResult();

        var wtPresentDays = GetWageTypeOrThrow(PresentDaysWageTypeCode);

        // Legacy wraps the whole body in "if (wtPresentDays.BillingMethod == FixedDaysPerMonth)"; for every
        // other BillingMethod presentdays stays 0 and is returned as-is.
        if (wtPresentDays.BillingMethod != BillingMethod.FixedDaysPerMonth)
        {
            return result;
        }

        var salaryRegister = _salaryRegisterRepository.GetAll().FirstOrDefault(r => r.Id == salaryRegisterId);
        if (salaryRegister == null)
        {
            throw new InvalidOperationException($"SalaryRegister '{salaryRegisterId}' not found.");
        }

        var settings = await GetDayCountSettingsAsync();

        #region << Get All Recovery Month List (legacy line 4565) >>

        List<DateTime> recoveryPeriods;
        if (payrollRunType == PayrollRunType.OffCycle
            && offCycleReason == PayrollDayCountOffCycleReasons.AdvanceLeave
            && salaryRegister.InPeriod == salaryRegister.ForPeriod)
        {
            recoveryPeriods = GetRecoveryPeriods(payEndDate, offCycleEndDate);
        }
        else
        {
            recoveryPeriods = new List<DateTime> { new DateTime(payStartDate.Year, payStartDate.Month, 1, 0, 0, 0) };
        }

        #endregion

        var employeeContracts = _employeeContractRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId)
            .Select(r => new { r.StartDate, r.EndDate })
            .ToList();

        var employeeAbsences = _employeeAbsenceRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId)
            .Select(r => new { r.StartDate, r.EndDate })
            .ToList();

        double presentDays = 0;

        // Legacy's TimeRegister cache bounds (Program.cs lines 1614-1617 for Regular, 1771-1773 for
        // OffCycle). GetPresentDays reads that cache (line 4586) with its OWN month-start..month-END
        // predicate, which -- unlike every other method in this class -- is NOT always a subset of the cache
        // window: on an OffCycle run ending mid-month, the cache stops at offCycleEndDate while the method's
        // predicate runs to the end of the month. The cap must therefore be applied explicitly here, before
        // the "IsLeaveEnded" truncation below (legacy truncates monthEndDate only AFTER the rows are read).
        // Without it the fixed-days/actual-days switch is fed a full month of rows and over-pays.
        var cacheStartDate = payStartDate;
        var cacheEndDate = payrollRunType == PayrollRunType.OffCycle ? offCycleEndDate : payEndDate;

        foreach (var rec in recoveryPeriods)
        {
            double workingDays = 0;

            var monthStartDate = rec;
            var monthEndDate = new DateTime(rec.Year, rec.Month, DateTime.DaysInMonth(rec.Year, rec.Month));

            // Intersect the month with legacy's cache window (see cacheStartDate/cacheEndDate above).
            var rowsStartDate = monthStartDate > cacheStartDate ? monthStartDate : cacheStartDate;
            var rowsEndDate = monthEndDate < cacheEndDate ? monthEndDate : cacheEndDate;

            var empTrList = GetPresentDayRows(employeeId, rowsStartDate, rowsEndDate, settings.EnableTimeRegisterApproval);

            #region << Check If IsLeaveEnded (legacy line 4623) >>

            var isLeaveEnded = false;
            if (monthEndDate > offCycleEndDate && offCycleReason != PayrollDayCountOffCycleReasons.EndOfService)
            {
                monthEndDate = offCycleEndDate;
                isLeaveEnded = true;
            }

            #endregion

            #region << Check If Mid-Month Joining Or Leaving (legacy line 4634) >>

            var isMidMonthJoining = false;
            var changeMidJoiningToFixed = false;
            double nonWorkingDayBeforeJoining = 0;
            var isMidMonthLeaving = false;

            var contractStart = employeeContracts.FirstOrDefault(r => r.StartDate <= monthStartDate && r.EndDate >= monthStartDate);
            var contractEnd = employeeContracts.FirstOrDefault(r => r.StartDate <= monthEndDate && r.EndDate >= monthEndDate);

            if (contractStart == null || contractEnd == null)
            {
                var contrStartDate = monthStartDate;
                var contrEndDate = monthEndDate;

                if (contractStart == null) // mid-month joining
                {
                    isMidMonthJoining = true;
                    contractStart = employeeContracts
                        .Where(r => r.StartDate >= monthStartDate && r.StartDate <= monthEndDate)
                        .OrderBy(r => r.StartDate)
                        .FirstOrDefault();

                    contrStartDate = contractStart == null ? LegacyDbDateNull : contractStart.StartDate;

                    if (contrStartDate != LegacyDbDateNull
                        && settings.ChangePartialPeriodFactoringInFixedDaysBilling
                        && settings.PartialPeriodCutoffDayInFixedDaysBilling != 0)
                    {
                        var daysInMonth = DateTime.DaysInMonth(rec.Year, rec.Month);

                        // DEVIATION (documented in class remarks): legacy overwrote the static setting field
                        // here; this port clamps into a local so later periods keep the configured value.
                        var cutoffDay = settings.PartialPeriodCutoffDayInFixedDaysBilling > daysInMonth
                            ? daysInMonth
                            : settings.PartialPeriodCutoffDayInFixedDaysBilling;

                        if (contrStartDate.Day < cutoffDay)
                        {
                            var beforeJoiningStart = new DateTime(rec.Year, rec.Month, 1);
                            var beforeJoiningEnd = new DateTime(rec.Year, rec.Month, contrStartDate.Day == 1 ? contrStartDate.Day : contrStartDate.Day - 1);

                            var nonWorkingWeekOffs = await GetNonWorkingWeekOffsAsync(employeeId, beforeJoiningStart, beforeJoiningEnd, settings.EnableRosterDuty);
                            nonWorkingDayBeforeJoining = beforeJoiningEnd.Subtract(beforeJoiningStart).Days + 1 - nonWorkingWeekOffs;

                            changeMidJoiningToFixed = true;
                        }
                    }
                }

                if (contractEnd == null) // mid-month leaving
                {
                    isMidMonthLeaving = true;
                    contractEnd = employeeContracts
                        .Where(r => r.EndDate <= monthEndDate && r.EndDate >= monthStartDate)
                        .OrderByDescending(r => r.EndDate)
                        .FirstOrDefault();

                    contrEndDate = contractEnd == null ? LegacyDbDateNull : contractEnd.EndDate;
                }

                monthStartDate = contrStartDate;
                monthEndDate = contrEndDate;
            }

            #endregion

            #region << Leave intersecting month start / month end (legacy lines 4695, 4704) >>

            var startOfMonthIntersect = false;
            if (settings.FixedToActualWhenLeaveOverlapOnMonthStart)
            {
                var firstDateOfMonth = new DateTime(rec.Year, rec.Month, 1);
                startOfMonthIntersect = employeeAbsences.Any(r => r.StartDate < firstDateOfMonth && r.EndDate >= firstDateOfMonth);
            }

            var endOfMonthIntersect = false;
            if (settings.FixedToActualWhenLeaveOverlapOnMonthEnd)
            {
                var lastDateOfMonth = new DateTime(rec.Year, rec.Month, DateTime.DaysInMonth(rec.Year, rec.Month));
                endOfMonthIntersect = employeeAbsences.Any(r => r.StartDate <= lastDateOfMonth && r.EndDate > lastDateOfMonth);
            }

            #endregion

            #region << Check If UnpaidLeave found (legacy line 4713) >>

            var isUnpaidLeave = false;
            if (settings.FixedToActualWhenUpaidLeaveFound)
            {
                isUnpaidLeave = empTrList.Any(r => r.LeaveTypeId != null && r.LeaveTypeIsPaid == false);
            }

            #endregion

            var absentDays = empTrList.Sum(r => r.AbsentDay);

            #region << Calculate Working days (legacy line 4726) >>

            var leaveDays = empTrList.Where(r => r.WeeklyOff == 0).Sum(r => r.LeaveDay);
            leaveDays += empTrList.Where(r => r.LeaveTypeId != null).Sum(r => r.IncludeInLeavePay);
            leaveDays += empTrList.Where(r => r.LeaveTypeId != null).Sum(r => r.LeaveAdjDay);

            var phDays = empTrList.Sum(r => r.PublicHoliday);
            var dedLateInEarlyOutDay = empTrList.Sum(r => r.DedLateInEarlyOutDay);

            if (!changeMidJoiningToFixed
                && (isMidMonthJoining
                    || isMidMonthLeaving
                    || isLeaveEnded
                    || (settings.FixedToActualWhenLeaveOverlapOnMonthStart && startOfMonthIntersect)
                    || (settings.FixedToActualWhenLeaveOverlapOnMonthEnd && endOfMonthIntersect)
                    || (settings.FixedToActualWhenUpaidLeaveFound && isUnpaidLeave)
                    || (settings.FixedToActualWhenAbsentDayFound && absentDays > 0)))
            {
                #region << Actual Working Days (legacy line 4734) >>

                if (wtPresentDays.ProrateMethod == ProrateMethod.WorkingDays)
                {
                    workingDays = empTrList.Where(r => r.LeaveDay != 1 && r.AbsentDay == 0)
                                           .Sum(r => r.ActualWorkingDay + r.PublicHoliday);
                }
                else if (wtPresentDays.ProrateMethod == ProrateMethod.WorkingDays_REST)
                {
                    // Used for "Calendar minus weekend only".
                    workingDays = empTrList.Where(r => r.LeaveDay != 1 && r.AbsentDay == 0)
                                           .Sum(r => r.ActualWorkingDay + r.PublicHoliday + r.RestDay);
                }

                if (settings.ApplyAnnualleaveNoImpactLogic)
                {
                    var foundAnnualLeaveNoImpact = empTrList.Any(r => r.LeaveDay > 0
                                                                   && r.LeaveTypeId != null
                                                                   && r.LeaveTypeNameEn == AnnualLeaveNoImpactIndemnityName);

                    if (!foundAnnualLeaveNoImpact && leaveDays > 0)
                    {
                        double calendarDays = DateTime.DaysInMonth(monthStartDate.Year, monthStartDate.Month);

                        // Legacy passes offCycleReasons = "" here (line 4763), unlike the Fixed-Days branch
                        // below which passes the real reason. Ported verbatim.
                        var weeklyOffDays = await GetPayrollActualWeekOffsAsync(employeeId, monthStartDate, payrollRunType, offCycleEndDate, string.Empty);
                        var salaryBasisDays = calendarDays - weeklyOffDays;

                        double dayDiff = 0;
                        if (salaryBasisDays > wtPresentDays.FixedDays)
                        {
                            dayDiff = salaryBasisDays - wtPresentDays.FixedDays;
                        }

                        workingDays -= dayDiff;
                    }
                }

                if (workingDays < 0)
                {
                    workingDays = 0;
                }

                #endregion
            }
            else
            {
                #region << Fixed Days (legacy line 4779) >>

                if (empTrList.Count > 0) // if TimeRegister found then calculate working days
                {
                    var totalLeaveDays = leaveDays + absentDays;

                    if (changeMidJoiningToFixed)
                    {
                        totalLeaveDays = leaveDays + nonWorkingDayBeforeJoining;
                    }

                    var fixedDays = wtPresentDays.FixedDays;
                    double calendarDays = DateTime.DaysInMonth(rec.Year, rec.Month);
                    var weeklyOffDays = await GetPayrollActualWeekOffsAsync(employeeId, rec, payrollRunType, offCycleEndDate, offCycleReason);
                    var salaryBasisDays = calendarDays - weeklyOffDays;

                    // Include DedLateInEarlyOutDay for TimeLogRequest approval (legacy line 4797).
                    if (empTrList.Any(r => r.TimeMgmtStatus == TimeMgmtStatus.ActualTime && r.MonthlyToleranceType == MonthlyToleranceType.NotSelected))
                    {
                        totalLeaveDays += dedLateInEarlyOutDay;
                    }

                    workingDays = totalLeaveDays >= salaryBasisDays ? 0 : fixedDays - totalLeaveDays;
                }

                #endregion
            }

            #endregion

            if (!settings.EnableBasicpayMoreThenFixedDays && leaveDays >= 26)
            {
                workingDays = 0;
            }

            // If the whole month is absent/unpaid, public holidays are still paid (legacy line 4816).
            if (workingDays == 0)
            {
                workingDays = phDays;
            }

            presentDays += workingDays;

            #region << MonthWiseInfo (legacy line 4824) >>

            // legacy also required Program.calculateSalRegDaysDetail == true; see class remarks (constant true).
            if (payrollRunType == PayrollRunType.OffCycle && salaryRegister.InPeriod == salaryRegister.ForPeriod)
            {
                var period = new DateTime(monthStartDate.Year, monthStartDate.Month, DateTime.DaysInMonth(monthStartDate.Year, monthStartDate.Month));
                result.MonthWiseDays.Add(new SalaryRegisterDayCount
                {
                    Period = period,
                    Rate = 0,
                    Number = workingDays,
                    Amount = 0
                });
            }

            #endregion
        }

        if (presentDays < 0)
        {
            presentDays = 0;
        }

        result.PresentDays = presentDays;
        return result;
    }

    /// <summary>
    /// The legacy anonymous-type projection at <c>Program.cs</c> line 4589, materialised as a real class
    /// (anonymous types cannot cross method boundaries).
    /// </summary>
    private sealed class PresentDayRow
    {
        public double ActualWorkingDay { get; set; }
        public double PublicHoliday { get; set; }
        public double RestDay { get; set; }
        public double WeeklyOff { get; set; }
        public double UnpaidLeaveDay { get; set; }
        public int? LeaveTypeId { get; set; }
        public bool LeaveTypeIsPaid { get; set; }
        public string LeaveTypeNameEn { get; set; }
        public double LeaveDay { get; set; }
        public double AbsentDay { get; set; }
        public double IncludeInLeavePay { get; set; }
        public double DedLateInEarlyOutDay { get; set; }
        public double LeaveAdjDay { get; set; }
        public TimeMgmtStatus TimeMgmtStatus { get; set; }
        public MonthlyToleranceType MonthlyToleranceType { get; set; }
    }

    private List<PresentDayRow> GetPresentDayRows(int employeeId, DateTime monthStartDate, DateTime monthEndDate, bool enableTimeRegisterApproval)
    {
        var rows = _timeRegisterRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId && r.DayDate >= monthStartDate && r.DayDate <= monthEndDate)
            .Select(r => new
            {
                r.ActualWorkingDay,
                r.ApprovedActualWorkingDay,
                r.PublicHoliday,
                r.RestDay,
                r.WeeklyOff,
                r.UnpaidLeaveDay,
                r.LeaveTypeId,
                r.LeaveDay,
                r.AbsentDay,
                r.ApprovedAbsentDay,
                r.IncludeInLeavePay,
                r.DedLateInEarlyOutDay,
                r.ApprovedDedLateInEarlyOutMins,
                r.PlannedWorkingMins,
                r.LeaveAdjDay,
                // TimePolicy is optional; project through the FK so a missing policy stays null instead of
                // faulting the non-nullable enum projection.
                TimeMgmtStatus = r.TimePolicyId == null ? (TimeMgmtStatus?)null : r.TimePolicy.TimeMgmtStatus,
                MonthlyToleranceType = r.TimePolicyId == null ? (MonthlyToleranceType?)null : r.TimePolicy.MonthlyToleranceType
            })
            .ToList();

        // TimeRegister has no LeaveType navigation in this port (see class remarks) -- resolve the referenced
        // AbsenceType rows once and attach IsPaid/NameEn.
        var leaveTypeIds = rows.Where(r => r.LeaveTypeId != null).Select(r => r.LeaveTypeId.Value).Distinct().ToList();
        var leaveTypes = leaveTypeIds.Count == 0
            ? new List<AbsenceTypeInfo>()
            : _absenceTypeRepository.GetAll()
                .Where(r => leaveTypeIds.Contains(r.Id))
                .Select(r => new AbsenceTypeInfo { Id = r.Id, IsPaid = r.IsPaid, NameEn = r.NameEn })
                .ToList();

        var leaveTypeMap = leaveTypes.ToDictionary(r => r.Id);

        return rows.Select(r =>
        {
            AbsenceTypeInfo leaveType = null;
            if (r.LeaveTypeId != null)
            {
                leaveTypeMap.TryGetValue(r.LeaveTypeId.Value, out leaveType);
            }

            return new PresentDayRow
            {
                ActualWorkingDay = enableTimeRegisterApproval ? r.ApprovedActualWorkingDay : r.ActualWorkingDay,
                PublicHoliday = r.PublicHoliday,
                RestDay = r.RestDay,
                WeeklyOff = r.WeeklyOff,
                UnpaidLeaveDay = r.UnpaidLeaveDay,
                LeaveTypeId = r.LeaveTypeId,
                LeaveTypeIsPaid = leaveType?.IsPaid ?? false,
                LeaveTypeNameEn = leaveType?.NameEn,
                LeaveDay = r.LeaveDay,
                AbsentDay = enableTimeRegisterApproval ? r.ApprovedAbsentDay : r.AbsentDay,
                IncludeInLeavePay = r.IncludeInLeavePay,
                DedLateInEarlyOutDay = !enableTimeRegisterApproval
                    ? r.DedLateInEarlyOutDay
                    : (r.PlannedWorkingMins != 0 ? r.ApprovedDedLateInEarlyOutMins / r.PlannedWorkingMins : 0),
                LeaveAdjDay = r.LeaveAdjDay,
                TimeMgmtStatus = r.TimeMgmtStatus ?? TimeMgmtStatus.NA,
                MonthlyToleranceType = r.MonthlyToleranceType ?? MonthlyToleranceType.NotSelected
            };
        }).ToList();
    }

    private sealed class AbsenceTypeInfo
    {
        public int Id { get; set; }
        public bool IsPaid { get; set; }
        public string NameEn { get; set; }
    }

    /// <summary>
    /// Ported from legacy <c>Program.GetRecoveryPeriods</c> (line 2922): every distinct month-start between
    /// <paramref name="payDate"/>'s month and <paramref name="offCycleEndDate"/>, inclusive.
    /// </summary>
    private static List<DateTime> GetRecoveryPeriods(DateTime payDate, DateTime offCycleEndDate)
    {
        var recoveryPeriods = new List<DateTime>();
        var recoveryStartDate = new DateTime(payDate.Year, payDate.Month, 1, 0, 0, 0);

        while (recoveryStartDate <= offCycleEndDate)
        {
            var monthStart = new DateTime(recoveryStartDate.Year, recoveryStartDate.Month, 1, 0, 0, 0);
            if (!recoveryPeriods.Contains(monthStart))
            {
                recoveryPeriods.Add(monthStart);
            }

            var daysInMonth = DateTime.DaysInMonth(recoveryStartDate.Year, recoveryStartDate.Month);
            recoveryStartDate = new DateTime(recoveryStartDate.Year, recoveryStartDate.Month, daysInMonth, 0, 0, 0).AddDays(1);
        }

        return recoveryPeriods;
    }

    /// <summary>
    /// Ported from legacy <c>Program.GetNonWorkingWeekOffs</c> (line 4853): a plain day-by-day duty walk over
    /// <paramref name="startDate"/>..<paramref name="endDate"/> counting weekly-off duties, ignoring
    /// TimeRegister entirely. Note legacy applies NO <c>EmployeeSubstituteDuty</c> override here (unlike
    /// <c>AccrualService</c>'s equivalent loop) -- preserved deliberately, see class remarks.
    /// </summary>
    private async Task<double> GetNonWorkingWeekOffsAsync(int employeeId, DateTime startDate, DateTime endDate, bool isRosterActive)
    {
        double weeklyOffDays = 0;

        var employeeDutyPeriods = isRosterActive
            ? null
            : _employeeDutyPeriodRepository.GetAll().Where(r => r.EmployeeId == employeeId).ToList();

        for (var dayDate = startDate; dayDate <= endDate; dayDate = dayDate.AddDays(1))
        {
            if (isRosterActive)
            {
                var rosterDuty = await _rosterService.GetRosterDutyAsync(employeeId, dayDate, null, false);
                weeklyOffDays += rosterDuty != null && rosterDuty.IsWeeklyOff ? 1 : 0;
                continue;
            }

            var dutyPeriod = employeeDutyPeriods
                .Where(r => r.StartDate <= dayDate && r.EndDate >= dayDate)
                .Select(r => r.DutyPeriod)
                .FirstOrDefault();

            if (dutyPeriod == null)
            {
                throw new InvalidOperationException($"DutyPeriod not found for Employee Id '{employeeId}' for '{dayDate:dd/MM/yyyy}'.");
            }

            var duty = GetDutyForDayOfWeek(dutyPeriod, dayDate);
            weeklyOffDays += duty != null && duty.IsWeeklyOff ? 1 : 0;
        }

        return weeklyOffDays;
    }

    /// <summary>
    /// Ported from legacy <c>Program.GetActualWeekOffs</c> (line 2858) -- the PAYROLL-ENGINE variant, which is
    /// a different method from the XAF <c>AccrualService.GetActualWeekOffs</c> ported as
    /// <see cref="IAccrualService.GetActualWeekOffsAsync(int, DateTime)"/>.
    ///
    /// <para>It seeds the count from <see cref="GetWeeklyOffDaysAsync"/> for the full month (passing the
    /// month-end as BOTH <c>payDate</c> and <c>offCycleEndDate</c>, so the off-cycle branch cannot shorten
    /// it), then back-fills days that have no TimeRegister row via a day-by-day duty walk. Kept private:
    /// it exists only to serve <see cref="GetPresentDaysAsync"/>, exactly as in legacy.</para>
    /// </summary>
    private async Task<double> GetPayrollActualWeekOffsAsync(int employeeId, DateTime payDate, PayrollRunType payrollRunType, DateTime offCycleEndDate, string offCycleReason)
    {
        var startDate = new DateTime(payDate.Year, payDate.Month, 1);
        var endDate = new DateTime(payDate.Year, payDate.Month, DateTime.DaysInMonth(payDate.Year, payDate.Month));
        double daysInMonth = DateTime.DaysInMonth(payDate.Year, payDate.Month);

        var daysWithTimeRegister = _timeRegisterRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId && r.DayDate >= startDate && r.DayDate <= endDate)
            .Select(r => r.DayDate)
            .ToList();

        // Legacy passes EndDate as both payDate and offCycleEndDate (line 2871).
        var weeklyOffDays = await GetWeeklyOffDaysAsync(employeeId, endDate, payrollRunType, endDate, offCycleReason);

        if (Math.Abs(daysInMonth - daysWithTimeRegister.Count) > double.Epsilon)
        {
            var settings = await GetDayCountSettingsAsync();
            var missingDays = new List<DateTime>();
            for (var dayDate = startDate; dayDate <= endDate; dayDate = dayDate.AddDays(1))
            {
                if (!daysWithTimeRegister.Contains(dayDate))
                {
                    missingDays.Add(dayDate);
                }
            }

            weeklyOffDays += await CountWeeklyOffsForDaysAsync(employeeId, missingDays, settings.EnableRosterDuty);
        }

        return weeklyOffDays;
    }

    /// <summary>
    /// The "day-by-day duty fallback" half of legacy <c>Program.GetActualWeekOffs</c> (lines 2875-2918).
    /// Deliberately does not consult <c>EmployeeSubstituteDuty</c> (legacy does not) -- see class remarks.
    /// </summary>
    private async Task<double> CountWeeklyOffsForDaysAsync(int employeeId, List<DateTime> days, bool isRosterActive)
    {
        double weeklyOffDays = 0;

        if (isRosterActive)
        {
            foreach (var dayDate in days)
            {
                var rosterDuty = await _rosterService.GetRosterDutyAsync(employeeId, dayDate, null, false);
                weeklyOffDays += rosterDuty != null && rosterDuty.IsWeeklyOff ? 1 : 0;
            }

            return weeklyOffDays;
        }

        var employeeDutyPeriods = _employeeDutyPeriodRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId)
            .ToList();

        foreach (var dayDate in days)
        {
            var dutyPeriod = employeeDutyPeriods
                .Where(r => r.StartDate <= dayDate && r.EndDate >= dayDate)
                .Select(r => r.DutyPeriod)
                .FirstOrDefault();

            if (dutyPeriod == null)
            {
                throw new InvalidOperationException($"DutyPeriod not found for Employee Id '{employeeId}' for '{dayDate:dd/MM/yyyy}'.");
            }

            var duty = GetDutyForDayOfWeek(dutyPeriod, dayDate);
            weeklyOffDays += duty != null && duty.IsWeeklyOff ? 1 : 0;
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

    #endregion

    #region << GetActiveDays (legacy Program.cs line 4895) >>

    public Task<double> GetActiveDaysAsync(int employeeId, DateTime payStartDate, DateTime payEndDate, PayrollRunType payrollRunType, DateTime offCycleEndDate)
    {
        double days = 0;

        if (payrollRunType == PayrollRunType.Regular)
        {
            days = SumTimeRegister(employeeId, payStartDate, payEndDate, r => r.ActiveDay);
        }
        else if (payrollRunType == PayrollRunType.OffCycle)
        {
            // Shortening clamp (legacy line 4909) -- see interface remarks on the off-cycle asymmetry.
            var effectiveEndDate = payEndDate < offCycleEndDate ? payEndDate : offCycleEndDate;
            days = SumTimeRegister(employeeId, payStartDate, effectiveEndDate, r => r.ActiveDay);
        }

        return Task.FromResult(days);
    }

    #endregion

    #region << GetAbsentDays (legacy Program.cs line 5042) >>

    public async Task<double> GetAbsentDaysAsync(
        int employeeId,
        DateTime payStartDate,
        DateTime payEndDate,
        PayrollRunType payrollRunType,
        DateTime offCycleEndDate,
        string wageTypeCode,
        int payrollAreaId)
    {
        var settings = await GetDayCountSettingsAsync();
        var enableTimeRegisterApproval = settings.EnableTimeRegisterApproval;

        // Legacy GetCutoffSetting (line 2797) -- the ref params become a tuple.
        var (cutoffStartDate, cutoffEndDate) = ApplyCutoffSetting(wageTypeCode, payStartDate, payEndDate, payrollAreaId);
        payStartDate = cutoffStartDate;
        payEndDate = cutoffEndDate;

        double days = 0;

        if (payrollRunType == PayrollRunType.Regular)
        {
            days = SumAbsentDays(employeeId, payStartDate, payEndDate, enableTimeRegisterApproval);
        }
        else if (payrollRunType == PayrollRunType.OffCycle)
        {
            // Unconditional assignment (legacy line 5059): can EXTEND the window.
            days = SumAbsentDays(employeeId, payStartDate, offCycleEndDate, enableTimeRegisterApproval);
        }

        return days;
    }

    private double SumAbsentDays(int employeeId, DateTime startDate, DateTime endDate, bool enableTimeRegisterApproval)
    {
        var query = _timeRegisterRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId
                     && r.DayDate >= startDate
                     && r.DayDate <= endDate
                     && r.UnpaidLeaveDay == 0);

        return enableTimeRegisterApproval
            ? query.Sum(r => (double?)r.ApprovedAbsentDay) ?? 0
            : query.Sum(r => (double?)r.AbsentDay) ?? 0;
    }

    /// <summary>
    /// Ported from legacy <c>Program.GetCutoffSetting</c> (line 2797) plus
    /// <c>DateTimeExtension.GetCutoffStartDate</c>/<c>GetCutoffEndDate</c> (lines 186/195). The two
    /// <c>ref DateTime</c> parameters become a tuple return.
    /// </summary>
    private (DateTime PayStartDate, DateTime PayEndDate) ApplyCutoffSetting(string wageTypeCode, DateTime payStartDate, DateTime payEndDate, int payrollAreaId)
    {
        var includeInCutoff = _wageTypeRepository.GetAll()
            .Where(r => r.Code == wageTypeCode)
            .Select(r => (bool?)r.IncludeInCutoff)
            .FirstOrDefault() ?? false;

        var cutOffDay = _payrollAreaRepository.GetAll()
            .Where(r => r.Id == payrollAreaId)
            .Select(r => (int?)r.CutOffDay)
            .FirstOrDefault();

        if (cutOffDay == null)
        {
            // Legacy dereferenced empPayrollArea directly and would NullReferenceException here.
            throw new InvalidOperationException($"PayrollArea '{payrollAreaId}' not found.");
        }

        if (includeInCutoff && cutOffDay.Value > 0)
        {
            return (GetCutoffStartDate(payEndDate, cutOffDay.Value), GetCutoffEndDate(payEndDate, cutOffDay.Value));
        }

        return (payStartDate, payEndDate);
    }

    /// <summary>Ported from legacy <c>DateTimeExtension.GetCutoffStartDate</c>.</summary>
    private static DateTime GetCutoffStartDate(DateTime payDate, int cutoffDay)
    {
        var month = payDate.Month;
        var year = payDate.Year + (month == 1 ? -1 : 0);
        month = month == 1 ? 12 : month - 1;
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var day = daysInMonth < cutoffDay ? daysInMonth : cutoffDay;
        return new DateTime(year, month, day, 0, 0, 0).AddDays(1);
    }

    /// <summary>Ported from legacy <c>DateTimeExtension.GetCutoffEndDate</c>.</summary>
    private static DateTime GetCutoffEndDate(DateTime payDate, int cutoffDay)
    {
        var year = payDate.Year;
        var month = payDate.Month;
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var day = daysInMonth < cutoffDay ? daysInMonth : cutoffDay;
        return new DateTime(year, month, day, 0, 0, 0);
    }

    #endregion

    #region << GetPublicHolidayDays (legacy Program.cs line 5073) >>

    public Task<double> GetPublicHolidayDaysAsync(int employeeId, DateTime payDate, PayrollRunType payrollRunType, DateTime offCycleEndDate)
    {
        var payStartDate = new DateTime(payDate.Year, payDate.Month, 1, 0, 0, 0);

        double days = 0;

        if (payrollRunType == PayrollRunType.Regular)
        {
            days = SumTimeRegister(employeeId, payStartDate, payDate, r => r.PublicHoliday);
        }
        else if (payrollRunType == PayrollRunType.OffCycle)
        {
            days = SumTimeRegister(employeeId, payStartDate, offCycleEndDate, r => r.PublicHoliday);
        }

        return Task.FromResult(days);
    }

    #endregion

    #region << GetWeeklyOffDays (legacy Program.cs line 5103) >>

    public async Task<double> GetWeeklyOffDaysAsync(int employeeId, DateTime payDate, PayrollRunType payrollRunType, DateTime offCycleEndDate, string offCycleReason)
    {
        double daysCount = 0;

        var wageType = GetWageTypeOrThrow(WeeklyOffWageTypeCode);
        var payStartDate = new DateTime(payDate.Year, payDate.Month, 1, 0, 0, 0);

        if (wageType.BillingMethod == BillingMethod.WorkingDaysPerMonth)
        {
            #region << For BillingMethod = WorkingDaysPerMonth (legacy line 5109) >>

            // NOTE: legacy's Regular and OffCycle branches here are byte-for-byte identical (lines 5119-5131) --
            // both use payEndDate = payDate and ignore offCycleEndDate. Collapsed into one query; behaviour
            // is unchanged.
            var timeReg = _timeRegisterRepository.GetAll()
                .Where(r => r.EmployeeId == employeeId && r.DayDate >= payStartDate && r.DayDate <= payDate)
                .OrderBy(r => r.DayDate)
                .Select(r => new { r.DayDate, DutyIsWeeklyOff = r.DutyId == null ? (bool?)null : r.Duty.IsWeeklyOff })
                .ToList();

            var isEndOfServiceOffCycle = payrollRunType == PayrollRunType.OffCycle
                                      && offCycleReason == PayrollDayCountOffCycleReasons.EndOfService;

            if (!isEndOfServiceOffCycle && timeReg.Count == 0)
            {
                // Day-by-day-pay (VariableTime) policies produce no TimeRegister rows, so they are exempt.
                var hasVariableTimePolicy = _employeeTimePolicyRepository.GetAll()
                    .Any(r => r.EmployeeId == employeeId
                           && !(r.StartDate > payDate || r.EndDate < payStartDate)
                           && r.TimePolicy != null
                           && r.TimePolicy.TimeMgmtStatus == TimeMgmtStatus.VariableTime);

                if (!hasVariableTimePolicy)
                {
                    throw new InvalidOperationException(
                        $"Time Register not found for Employee Id '{employeeId}' from '{payStartDate:dd/MM/yyyy}' to '{payDate:dd/MM/yyyy}'.");
                }
            }

            var settings = await GetDayCountSettingsAsync();
            if (settings.EnableRosterDuty)
            {
                foreach (var tReg in timeReg)
                {
                    var duty = await _rosterService.GetRosterDutyAsync(employeeId, tReg.DayDate, null, false);
                    daysCount += duty != null && duty.IsWeeklyOff ? 1 : 0;
                }
            }
            else
            {
                daysCount = timeReg.Count(r => r.DutyIsWeeklyOff == true);
            }

            #endregion
        }
        else
        {
            #region << For Other BillingMethod (legacy line 5168) >>

            if (payrollRunType == PayrollRunType.Regular)
            {
                daysCount = SumTimeRegister(employeeId, payStartDate, payDate, r => r.WeeklyOff);
            }
            else if (payrollRunType == PayrollRunType.OffCycle)
            {
                daysCount = SumTimeRegister(employeeId, payStartDate, offCycleEndDate, r => r.WeeklyOff);
            }

            #endregion
        }

        return daysCount;
    }

    #endregion

    #region << GetRestDays (legacy Program.cs line 5196) >>

    public async Task<double> GetRestDaysAsync(int employeeId, DateTime payDate, PayrollRunType payrollRunType, DateTime offCycleEndDate)
    {
        var settings = await GetDayCountSettingsAsync();
        var enableTimeRegisterApproval = settings.EnableTimeRegisterApproval;
        var payStartDate = new DateTime(payDate.Year, payDate.Month, 1, 0, 0, 0);

        double days = 0;

        if (payrollRunType == PayrollRunType.Regular)
        {
            days = SumRestDays(employeeId, payStartDate, payDate, enableTimeRegisterApproval);
        }
        else if (payrollRunType == PayrollRunType.OffCycle)
        {
            days = SumRestDays(employeeId, payStartDate, offCycleEndDate, enableTimeRegisterApproval);
        }

        return days;
    }

    private double SumRestDays(int employeeId, DateTime startDate, DateTime endDate, bool enableTimeRegisterApproval)
    {
        // "This is done if NotReturnFromLeave, then RestDay is Absent" (legacy comment, line 5208).
        var query = _timeRegisterRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId && r.DayDate >= startDate && r.DayDate <= endDate);

        query = enableTimeRegisterApproval
            ? query.Where(r => r.ApprovedAbsentDay == 0)
            : query.Where(r => r.AbsentDay == 0);

        return query.Sum(r => (double?)r.RestDay) ?? 0;
    }

    #endregion

    #region << GetSickLeavePaidDays / GetSickLeaveUnpaidDays (legacy Program.cs lines 5227 / 5254) >>

    public Task<double> GetSickLeavePaidDaysAsync(int employeeId, DateTime payDate, PayrollRunType payrollRunType, DateTime offCycleEndDate)
    {
        return Task.FromResult(SumSickLeaveDays(employeeId, payDate, payrollRunType, offCycleEndDate, r => r.SickLeavePaidDay));
    }

    public Task<double> GetSickLeaveUnpaidDaysAsync(int employeeId, DateTime payDate, PayrollRunType payrollRunType, DateTime offCycleEndDate)
    {
        return Task.FromResult(SumSickLeaveDays(employeeId, payDate, payrollRunType, offCycleEndDate, r => r.SickLeaveUnpaidDay));
    }

    private double SumSickLeaveDays(
        int employeeId,
        DateTime payDate,
        PayrollRunType payrollRunType,
        DateTime offCycleEndDate,
        Expression<Func<TimeRegister, double>> selector)
    {
        var payStartDate = new DateTime(payDate.Year, payDate.Month, 1, 0, 0, 0);
        var payEndDate = payDate;

        if (payrollRunType == PayrollRunType.Regular)
        {
            return SumTimeRegister(employeeId, payStartDate, payEndDate, selector);
        }

        if (payrollRunType == PayrollRunType.OffCycle)
        {
            // Shortening clamp (legacy lines 5242 / 5269), unlike GetPublicHolidayDays/GetRestDays.
            var effectiveEndDate = payEndDate < offCycleEndDate ? payEndDate : offCycleEndDate;
            return SumTimeRegister(employeeId, payStartDate, effectiveEndDate, selector);
        }

        return 0;
    }

    #endregion

    #region << GetPaidDays (legacy Program.cs line 5282) >>

    public Task<double> GetPaidDaysAsync(int salaryRegisterId)
    {
        var details = _salaryRegisterDetailRepository.GetAll()
            .Where(r => r.SalaryRegisterId == salaryRegisterId)
            .Select(r => new { Code = r.WageType.Code, r.Number })
            .ToList();

        // Legacy uses .Select(...).FirstOrDefault() per code, i.e. a missing detail contributes 0 and a
        // duplicated wage type contributes only its first row. Preserved exactly.
        var daysCount = FirstNumberOrZero(details.Where(r => r.Code == PresentDaysWageTypeCode).Select(r => r.Number))
                      + FirstNumberOrZero(details.Where(r => r.Code == PaidLeaveDaysWageTypeCode).Select(r => r.Number))
                      + FirstNumberOrZero(details.Where(r => r.Code == FirstTimeTypeWageTypeCode).Select(r => r.Number));

        return Task.FromResult(daysCount);
    }

    private static double FirstNumberOrZero(IEnumerable<double> numbers)
    {
        return numbers.FirstOrDefault();
    }

    #endregion

    #region << Shared Helpers >>

    /// <summary>
    /// The shape shared by GetActiveDays/GetPublicHolidayDays/GetWeeklyOffDays(non-WorkingDaysPerMonth branch)/
    /// GetSickLeave*Days: sum one <see cref="TimeRegister"/> column over an employee/date window.
    /// The projected values are materialised before summing so an empty window yields 0 rather than the
    /// provider's <c>SUM(NULL)</c> behaviour (at most ~31 rows per call).
    /// </summary>
    private double SumTimeRegister(int employeeId, DateTime startDate, DateTime endDate, Expression<Func<TimeRegister, double>> selector)
    {
        return _timeRegisterRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId && r.DayDate >= startDate && r.DayDate <= endDate)
            .Select(selector)
            .ToList()
            .Sum();
    }

    private WageType GetWageTypeOrThrow(string code)
    {
        var wageType = _wageTypeRepository.GetAll().FirstOrDefault(r => r.Code == code);
        if (wageType == null)
        {
            // Legacy dereferenced the FirstOrDefault() result directly (lines 4563 / 5111) and would
            // NullReferenceException; this raises an actionable message instead.
            throw new InvalidOperationException($"WageType '{code}' not found.");
        }

        return wageType;
    }

    /// <summary>
    /// The subset of legacy <c>Program</c>'s static setting mirrors that the day-count layer reads, with the
    /// legacy field initialisers as the fallback for a tenant that has no settings row yet.
    /// </summary>
    private sealed class DayCountSettings
    {
        public bool EnableRosterDuty { get; set; }                                  // legacy isRosterActive (default false)
        public bool EnableTimeRegisterApproval { get; set; }                        // legacy enableTimeRegisterApproval (default false)
        public bool ApplyAnnualleaveNoImpactLogic { get; set; }                     // legacy apply_Annualleavenoimpact_Logic (default false)
        public bool EnableBasicpayMoreThenFixedDays { get; set; } = true;           // legacy enableBasicpayMoreThenFixeddays (default TRUE)
        public bool FixedToActualWhenLeaveOverlapOnMonthStart { get; set; }         // default false
        public bool FixedToActualWhenLeaveOverlapOnMonthEnd { get; set; }           // default false
        public bool FixedToActualWhenUpaidLeaveFound { get; set; }                  // default false
        public bool FixedToActualWhenAbsentDayFound { get; set; }                   // default false
        public bool ChangePartialPeriodFactoringInFixedDaysBilling { get; set; }    // default false
        public int PartialPeriodCutoffDayInFixedDaysBilling { get; set; }           // default 0
    }

    private async Task<DayCountSettings> GetDayCountSettingsAsync()
    {
        var timeSettings = await _sysSettingsAccessor.GetTimeSettingsAsync();
        var payrollSettings = await _sysSettingsAccessor.GetPayrollSettingsAsync();

        return new DayCountSettings
        {
            EnableRosterDuty = timeSettings?.EnableRosterDuty ?? false,
            EnableTimeRegisterApproval = timeSettings?.EnableTimeRegisterApproval ?? false,
            ApplyAnnualleaveNoImpactLogic = payrollSettings?.ApplyAnnualleaveNoImpactLogic ?? false,
            EnableBasicpayMoreThenFixedDays = payrollSettings?.EnableBasicpayMoreThenFixedDays ?? true,
            FixedToActualWhenLeaveOverlapOnMonthStart = payrollSettings?.FixedToActualWhenLeaveOverlapOnMonthStart ?? false,
            FixedToActualWhenLeaveOverlapOnMonthEnd = payrollSettings?.FixedToActualWhenLeaveOverlapOnMonthEnd ?? false,
            FixedToActualWhenUpaidLeaveFound = payrollSettings?.FixedToActualWhenUpaidLeaveFound ?? false,
            FixedToActualWhenAbsentDayFound = payrollSettings?.FixedToActualWhenAbsentDayFound ?? false,
            ChangePartialPeriodFactoringInFixedDaysBilling = payrollSettings?.ChangePartialPeriodFactoringInFixedDaysBilling ?? false,
            PartialPeriodCutoffDayInFixedDaysBilling = payrollSettings?.PartialPeriodCutoffDayInFixedDaysBilling ?? 0
        };
    }

    #endregion
}
