using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Services;
using Abp.Domain.Uow;
using Abp.Linq.Extensions;
using HCMv3.Common;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Services;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Services;

namespace HCMv3.PayrollManagement.Services;

/// <inheritdoc cref="IAccrualService"/>
/// <remarks>
/// Ported from legacy XAF <c>HCM10.Module.BusinessObjects.Services.AccrualService</c> (a static-methods-only
/// class operating on an XPO <c>Session</c> and live-navigation-property XPO business objects). This port
/// replaces every <c>XPQuery&lt;T&gt;(session)</c> call with an injected <c>IRepository&lt;T&gt;</c>, and
/// every <c>emp.SomeCollection</c> navigation (Employee has NO such bidirectional collections in this port --
/// see <c>HCMv3.PersonnelAdministration.Employee</c> remarks) with a repository query filtered by
/// <c>EmployeeId</c>.
///
/// <para><b>Structural adaptations, all deliberate and documented at the point of use below:</b></para>
/// <list type="bullet">
/// <item><description><b>EmployeeGroup/EmployeeSubGroup</b>: <c>EmployeeOrgAssignment</c> in this port has no
/// <c>EmployeeGroupId</c>/<c>EmployeeSubGroupId</c> FK yet (see that entity's class remarks), so every method
/// that legacy derived these from <c>emp.OrgAssignment</c> takes them as explicit, caller-supplied nullable
/// id parameters instead (same adaptation already used by <c>TravelManagement.CalculateExpectedPerDiemAsync</c>).</description></item>
/// <item><description><b>CurrentPayrollArea</b>: <c>Employee</c> has no computed "current payroll area"
/// property in this port (PayrollArea is not wired into org assignment yet) -- <c>CalculateForfeitForPublicHolidayAsync</c>
/// takes <c>payrollAreaId</c> explicitly.</description></item>
/// <item><description><b>Roster duty resolution</b>: legacy's <c>RosterService.GetRosterDuty</c> branch
/// (used when <c>ManageTimeSettings.EnableRosterDuty</c> is on) is ported by delegating to
/// <see cref="HCMv3.TimeManagement.Services.IRosterService.GetRosterDutyAsync"/> (a parallel earlier wave's
/// full, faithful port of that legacy static class), constructor-injected here. See the
/// <c>CalculateMissingDayWeeklyOffsAsync</c> method below.</description></item>
/// <item><description><b>EmployeeAbsence.ActualLeaveDays / GetHoulyLeaveDay</b>: these two legacy static
/// helpers (holiday/rest-day-aware partial-day leave calculators) live deep inside the legacy
/// <c>EmployeeAbsence</c> calculation engine (a ~4000-line legacy file, ported separately as its own "Group
/// F" unit of work). This service now calls the faithfully-ported
/// <see cref="IEmployeeAbsenceCalculationService.ActualLeaveDaysAsync"/>/
/// <see cref="IEmployeeAbsenceCalculationService.GetHoulyLeaveDayAsync"/> directly wherever legacy used them
/// for the "overlap" adjustment inside the forfeit-day-loop methods -- see the call sites in
/// <c>CalculateOpeningBalanceAsync</c>/<c>CalculateClosingBalanceAndForfeitAsync</c> below. No more
/// approximation: full calendar-day counting and the AbsenceHours/8 hourly-fraction guess have both been
/// replaced with the real calculation.</description></item>
/// <item><description><b>SysCreator/SysCreatedOn/SysModifier/SysModifiedOn</b>: legacy manually stamped these
/// on every mutation. This port's entities inherit ABP's own <c>FullAuditedEntity</c> auditing
/// (CreatorUserId/CreationTime/LastModifierUserId/LastModificationTime), applied automatically by the ABP
/// auditing interceptor on <c>SaveChanges</c> -- no manual stamping code is needed or ported.</description></item>
/// <item><description><b>DontUpdateQuotaBalance</b>: the legacy <c>EmployeeAbsQuotaForfeit.DontUpdateQuotaBalance</c>
/// flag existed only to suppress that entity's own <c>OnSaving</c> cascade (which would otherwise re-adjust
/// <c>EmployeeAbsenceQuota.QuotaForfeited</c> a second time) while these very same
/// <c>AccrualService.CalculateXxxForfeit</c> methods were already doing that adjustment themselves. In this
/// port, <c>EmployeeAbsQuotaForfeit</c> carries no such entity-level cascade (per its own class remarks --
/// cascades belong at the app-service layer, not here), so there is nothing to suppress: this domain service
/// calling the repositories directly is the ONLY place that adjusts <c>EmployeeAbsenceQuota.QuotaForfeited</c>
/// for these code paths.</description></item>
/// </list>
///
/// <see cref="UnitOfWorkAttribute"/> at the class level, matching <c>SysSettingsAccessor</c> (this domain
/// service is independently callable, opening its own unit of work when not already inside one).
/// </remarks>
[UnitOfWork]
public class AccrualService : HCMv3DomainServiceBase, IAccrualService
{
    private const string MonthlyForfeitComment = "Monthly Forfeit";
    private const string PublicHolidayForfeitComment = "PublicHoliday Forfeit";

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeContract> _employeeContractRepository;
    private readonly IRepository<AbsenceType> _absenceTypeRepository;
    private readonly IRepository<TicketType> _ticketTypeRepository;
    private readonly IRepository<WageType> _wageTypeRepository;
    private readonly IRepository<WageTypeLeave> _wageTypeLeaveRepository;
    private readonly IRepository<TimeRegister> _timeRegisterRepository;
    private readonly IRepository<SalaryRegister> _salaryRegisterRepository;
    private readonly IRepository<SalaryRegisterDetail> _salaryRegisterDetailRepository;
    private readonly IRepository<EmployeeAbsence> _employeeAbsenceRepository;
    private readonly IRepository<EmployeeAbsAdjustment> _employeeAbsAdjustmentRepository;
    private readonly IRepository<EmployeeAbsenceQuota> _employeeAbsenceQuotaRepository;
    private readonly IRepository<EmployeeAbsQuotaForfeit> _employeeAbsQuotaForfeitRepository;
    private readonly IRepository<EmployeeAttendance> _employeeAttendanceRepository;
    private readonly IRepository<EmployeeSubGroupAbsence> _employeeSubGroupAbsenceRepository;
    private readonly IRepository<EmployeeSubGroupGratuity> _employeeSubGroupGratuityRepository;
    private readonly IRepository<EmployeeGroup> _employeeGroupRepository;
    private readonly IRepository<IndemnityPayRule> _indemnityPayRuleRepository;
    private readonly IRepository<EmployeeGratuity> _employeeGratuityRepository;
    private readonly IRepository<EmployeeBasicPay> _employeeBasicPayRepository;
    private readonly IRepository<EmployeeBasicPayDetail> _employeeBasicPayDetailRepository;
    private readonly IRepository<TimeTransfer> _timeTransferRepository;
    private readonly IRepository<EmployeeDutyPeriod> _employeeDutyPeriodRepository;
    private readonly IRepository<EmployeeSubstituteDuty> _employeeSubstituteDutyRepository;
    private readonly IRepository<PayrollControlRecord> _payrollControlRecordRepository;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;
    private readonly IRosterService _rosterService;
    private readonly IEmployeeAbsenceCalculationService _employeeAbsenceCalculationService;

    public AccrualService(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeContract> employeeContractRepository,
        IRepository<AbsenceType> absenceTypeRepository,
        IRepository<TicketType> ticketTypeRepository,
        IRepository<WageType> wageTypeRepository,
        IRepository<WageTypeLeave> wageTypeLeaveRepository,
        IRepository<TimeRegister> timeRegisterRepository,
        IRepository<SalaryRegister> salaryRegisterRepository,
        IRepository<SalaryRegisterDetail> salaryRegisterDetailRepository,
        IRepository<EmployeeAbsence> employeeAbsenceRepository,
        IRepository<EmployeeAbsAdjustment> employeeAbsAdjustmentRepository,
        IRepository<EmployeeAbsenceQuota> employeeAbsenceQuotaRepository,
        IRepository<EmployeeAbsQuotaForfeit> employeeAbsQuotaForfeitRepository,
        IRepository<EmployeeAttendance> employeeAttendanceRepository,
        IRepository<EmployeeSubGroupAbsence> employeeSubGroupAbsenceRepository,
        IRepository<EmployeeSubGroupGratuity> employeeSubGroupGratuityRepository,
        IRepository<EmployeeGroup> employeeGroupRepository,
        IRepository<IndemnityPayRule> indemnityPayRuleRepository,
        IRepository<EmployeeGratuity> employeeGratuityRepository,
        IRepository<EmployeeBasicPay> employeeBasicPayRepository,
        IRepository<EmployeeBasicPayDetail> employeeBasicPayDetailRepository,
        IRepository<TimeTransfer> timeTransferRepository,
        IRepository<EmployeeDutyPeriod> employeeDutyPeriodRepository,
        IRepository<EmployeeSubstituteDuty> employeeSubstituteDutyRepository,
        IRepository<PayrollControlRecord> payrollControlRecordRepository,
        ISysSettingsAccessor sysSettingsAccessor,
        IRosterService rosterService,
        IEmployeeAbsenceCalculationService employeeAbsenceCalculationService)
    {
        _employeeRepository = employeeRepository;
        _employeeContractRepository = employeeContractRepository;
        _absenceTypeRepository = absenceTypeRepository;
        _ticketTypeRepository = ticketTypeRepository;
        _wageTypeRepository = wageTypeRepository;
        _wageTypeLeaveRepository = wageTypeLeaveRepository;
        _timeRegisterRepository = timeRegisterRepository;
        _salaryRegisterRepository = salaryRegisterRepository;
        _salaryRegisterDetailRepository = salaryRegisterDetailRepository;
        _employeeAbsenceRepository = employeeAbsenceRepository;
        _employeeAbsAdjustmentRepository = employeeAbsAdjustmentRepository;
        _employeeAbsenceQuotaRepository = employeeAbsenceQuotaRepository;
        _employeeAbsQuotaForfeitRepository = employeeAbsQuotaForfeitRepository;
        _employeeAttendanceRepository = employeeAttendanceRepository;
        _employeeSubGroupAbsenceRepository = employeeSubGroupAbsenceRepository;
        _employeeSubGroupGratuityRepository = employeeSubGroupGratuityRepository;
        _employeeGroupRepository = employeeGroupRepository;
        _indemnityPayRuleRepository = indemnityPayRuleRepository;
        _employeeGratuityRepository = employeeGratuityRepository;
        _employeeBasicPayRepository = employeeBasicPayRepository;
        _employeeBasicPayDetailRepository = employeeBasicPayDetailRepository;
        _timeTransferRepository = timeTransferRepository;
        _employeeDutyPeriodRepository = employeeDutyPeriodRepository;
        _employeeSubstituteDutyRepository = employeeSubstituteDutyRepository;
        _payrollControlRecordRepository = payrollControlRecordRepository;
        _sysSettingsAccessor = sysSettingsAccessor;
        _rosterService = rosterService;
        _employeeAbsenceCalculationService = employeeAbsenceCalculationService;
    }

    // NOTE: HCMv3.Core has no reference to EntityFrameworkCore (per architecture rules), so the usual
    // Microsoft.EntityFrameworkCore async IQueryable extensions (.ToListAsync()/.FirstOrDefaultAsync()/etc.)
    // are not available here. All queries use the plain synchronous IQueryable returned by
    // IRepository<T>.GetAll() (same pattern as HCMv3.SystemSettings.Services.SysSettingsAccessor), with
    // public methods still exposed as Task<T> for interface consistency and to leave room for a future
    // EF-Core-aware implementation swap.

    #region << GetAsOnDateQuota >>

    public async Task<double> GetAsOnDateQuotaAsync(int employeeId, int? employeeSubGroupId, int absenceTypeId, DateTime asOnDate, bool includeFutureTrans)
    {
        var employee = GetEmployeeOrThrow(employeeId);
        var absenceType = GetAbsenceTypeOrThrow(absenceTypeId);

        double balanceQuota = 0;

        double perYearQuota = 0;
        double maxLimit = 0;
        var empSubGroupAbs = GetEmployeeSubGroupLeave(employeeSubGroupId, employee.JoiningDate, absenceTypeId, asOnDate);
        if (empSubGroupAbs != null)
        {
            perYearQuota = empSubGroupAbs.TotalDays;
            maxLimit = empSubGroupAbs.MaxLimit;
        }

        if (perYearQuota != 0)
        {
            if (absenceType.Code == "1" || absenceType.QuotaType == QuotaType.AnnualLeave)
            {
                var startDate = new DateTime(asOnDate.Year, 1, 1);
                var endDate = new DateTime(asOnDate.Year, 12, 31);
                var totalDaysInYear = endDate.Subtract(startDate).TotalDays + 1;
                var perDayQuota = perYearQuota / totalDaysInYear;

                var empTimeRegister = _timeRegisterRepository.GetAll()
                    .Where(r => r.EmployeeId == employeeId && r.DayDate >= employee.JoiningDate && r.DayDate <= asOnDate)
                    .ToList();

                // Legacy calls .Max()/.Sum() directly on an XPQuery that is assumed non-empty; if TimeRegister
                // has never been run for this employee up to asOnDate, legacy would throw too (parity kept).
                var lastDateOfTe = empTimeRegister.Max(r => r.DayDate);
                var totalAccrualInTe = empTimeRegister.Sum(r => r.AnnualLeaveAccrualPerDay);

                var totalDaysNotInTe = asOnDate.Subtract(lastDateOfTe).TotalDays;
                var totalAccrualNotInTe = totalDaysNotInTe * perDayQuota;

                var asOnDateTotalAccrual = totalAccrualInTe + totalAccrualNotInTe;

                var asOnDateTotalUsedQuota = _employeeAbsenceRepository.GetAll()
                    .Where(r => r.EmployeeId == employeeId && r.AbsenceTypeId == absenceTypeId)
                    .WhereIf(!includeFutureTrans, r => r.StartDate <= asOnDate)
                    .Sum(r => (double?)r.TotalLeaveDays) ?? 0;

                var asOnDateTotalAdjustQuota = _employeeAbsAdjustmentRepository.GetAll()
                    .Where(r => r.EmployeeId == employeeId && r.AbsenceTypeId == absenceTypeId && r.AdjustmentType != AdjustmentType.NoImpactOnQuota)
                    .WhereIf(!includeFutureTrans, r => r.EffectiveDate <= asOnDate)
                    .Sum(r => (double?)r.QuotaAdjusted) ?? 0;

                var asOnDateTotalBalanceQuota = asOnDateTotalAccrual - asOnDateTotalUsedQuota - asOnDateTotalAdjustQuota;

                if (maxLimit != 0 && asOnDateTotalBalanceQuota > maxLimit)
                {
                    asOnDateTotalBalanceQuota = maxLimit;
                }

                balanceQuota = asOnDateTotalBalanceQuota;
            }
            else if (absenceType.Code == "3" || absenceType.QuotaType == QuotaType.SickLeave)
            {
                balanceQuota = perYearQuota;

                var (deductionStartDate, deductionEndDate) = await GetLeaveAccrualStartEndAsync(employeeId, absenceTypeId, asOnDate);

                balanceQuota = GetProRataBasedBalanceQuota(employee.JoiningDate, absenceType.AccrualPeriod, absenceType.LeaveEntitlement, balanceQuota, deductionStartDate, deductionEndDate);

                var usedLeavesCount = _employeeAbsenceRepository.GetAll()
                    .Where(r => r.AbsenceTypeId == absenceTypeId && r.EmployeeId == employeeId
                             && !(r.StartDate > deductionEndDate || r.EndDate < deductionStartDate))
                    .Sum(r => (double?)r.TotalLeaveDays) ?? 0;

                if (absenceType.Code == "3")
                {
                    usedLeavesCount += _timeTransferRepository.GetAll()
                        .Where(r => r.EmployeeId == employeeId && r.TransferType.Code == "2"
                                 && r.TransferDate >= deductionStartDate && r.TransferDate <= deductionEndDate)
                        .Sum(r => (double?)r.TransferCount) ?? 0;
                }

                balanceQuota -= usedLeavesCount;
            }
        }

        return balanceQuota;
    }

    #endregion

    #region << GetAsOnDateProvision >>

    public async Task<double> GetAsOnDateProvisionAsync(int employeeId, int absenceTypeId, DateTime asOnDate, double asOnDateQuota)
    {
        var absenceType = GetAbsenceTypeOrThrow(absenceTypeId);

        double provision = 0;

        var empSalaryRegister = _salaryRegisterRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId && r.InPeriod <= asOnDate && r.ForPeriod <= asOnDate)
            .OrderByDescending(r => r.InPeriod).ThenByDescending(r => r.ForPeriod)
            .FirstOrDefault();

        if (empSalaryRegister != null && absenceType.WageTypeId != null)
        {
            var includeInPaidLeaveWageTypeIds = _wageTypeLeaveRepository.GetAll()
                .Where(r => r.AbsenceTypeId == absenceTypeId)
                .Select(r => r.WageTypeId)
                .ToList();

            var totalBasicPay = _salaryRegisterDetailRepository.GetAll()
                .Where(r => r.SalaryRegisterId == empSalaryRegister.Id && includeInPaidLeaveWageTypeIds.Contains(r.WageTypeId))
                .Sum(r => (double?)r.Rate) ?? 0;

            var wageType = _wageTypeRepository.GetAll().FirstOrDefault(r => r.Id == absenceType.WageTypeId.Value);
            var payDate = empSalaryRegister.ForPeriod;

            if (wageType != null)
            {
                var calendarDays = DateTime.DaysInMonth(payDate.Year, payDate.Month);
                double amount;

                if (wageType.BillingMethod == BillingMethod.FixedDaysPerMonth)
                {
                    amount = (totalBasicPay / wageType.FixedDays) * asOnDateQuota;
                }
                else if (wageType.BillingMethod == BillingMethod.CalendarDaysPerMonth)
                {
                    amount = (totalBasicPay / calendarDays) * asOnDateQuota;
                }
                else if (wageType.BillingMethod == BillingMethod.WorkingDaysPerMonth)
                {
                    var firstDetail = _salaryRegisterDetailRepository.GetAll()
                        .Where(r => r.SalaryRegisterId == empSalaryRegister.Id)
                        .OrderBy(r => r.Id).FirstOrDefault();
                    var weeklyOffDays = firstDetail != null
                        ? await GetActualWeekOffsAsync(employeeId, payDate, firstDetail.Id)
                        : 0;
                    var salaryBasisDays = calendarDays - weeklyOffDays;
                    amount = (totalBasicPay / salaryBasisDays) * asOnDateQuota;
                }
                else
                {
                    amount = 0;
                }

                provision = amount;
            }
        }

        return provision;
    }

    #endregion

    #region << GetActualWeekOffs >>

    public async Task<double> GetActualWeekOffsAsync(int employeeId, DateTime payDate, int salaryRegisterDetailId)
    {
        var startDate = new DateTime(payDate.Year, payDate.Month, 1);
        var endDate = new DateTime(payDate.Year, payDate.Month, DateTime.DaysInMonth(payDate.Year, payDate.Month));

        var salaryRegisterDetail = _salaryRegisterDetailRepository.GetAll().FirstOrDefault(r => r.Id == salaryRegisterDetailId);
        double weeklyOffDays = 0;
        if (salaryRegisterDetail != null)
        {
            weeklyOffDays = _salaryRegisterDetailRepository.GetAll()
                .Where(r => r.SalaryRegisterId == salaryRegisterDetail.SalaryRegisterId && r.WageType.Code == "4008") // 4008 Weekly off
                .Select(r => r.Number)
                .FirstOrDefault();
        }

        var daysInMonth = DateTime.DaysInMonth(payDate.Year, payDate.Month);
        var daysWithTimeRegister = _timeRegisterRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId && r.DayDate >= startDate && r.DayDate <= endDate)
            .Select(r => r.DayDate)
            .ToList();

        if (daysInMonth != daysWithTimeRegister.Count)
        {
            weeklyOffDays += await CalculateMissingDayWeeklyOffsAsync(employeeId, startDate, endDate, daysWithTimeRegister);
        }

        return weeklyOffDays;
    }

    public async Task<double> GetActualWeekOffsAsync(int employeeId, DateTime payDate)
    {
        var startDate = new DateTime(payDate.Year, payDate.Month, 1);
        var endDate = new DateTime(payDate.Year, payDate.Month, DateTime.DaysInMonth(payDate.Year, payDate.Month));

        var timeReg = _timeRegisterRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId && r.DayDate >= startDate && r.DayDate <= endDate)
            .Select(r => new { r.DayDate, r.WeeklyOff })
            .ToList();

        double weeklyOffDays = timeReg.Count(r => r.WeeklyOff > 0);

        var daysInMonth = DateTime.DaysInMonth(payDate.Year, payDate.Month);
        if (daysInMonth != timeReg.Count)
        {
            weeklyOffDays += await CalculateMissingDayWeeklyOffsAsync(employeeId, startDate, endDate, timeReg.Select(r => r.DayDate).ToList());
        }

        return weeklyOffDays;
    }

    /// <summary>
    /// Shared "day-by-day duty fallback" loop used by both <c>GetActualWeekOffs</c> overloads for days that
    /// have no <see cref="TimeRegister"/> row yet. See class remarks for the roster-duty parity notes.
    /// </summary>
    private async Task<double> CalculateMissingDayWeeklyOffsAsync(int employeeId, DateTime startDate, DateTime endDate, List<DateTime> daysAlreadyCovered)
    {
        var timeSettings = await _sysSettingsAccessor.GetTimeSettingsAsync();
        if (timeSettings != null && timeSettings.EnableRosterDuty)
        {
            // Legacy delegates to RosterService.GetRosterDuty here. Ported by calling into IRosterService
            // (parallel earlier wave), which already implements the full priority chain (EmployeeSubstituteDuty
            // -> EmployeeDutyPeriod -> RosterGroupDetail/RosterGroupSchedule, with the multi-week rotation
            // formula on both StartPoint-bearing paths).
            double rosterWeeklyOffDays = 0;

            for (var dayDate = startDate; dayDate <= endDate; dayDate = dayDate.AddDays(1))
            {
                if (daysAlreadyCovered.Contains(dayDate))
                {
                    continue;
                }

                var duty = await _rosterService.GetRosterDutyAsync(employeeId, dayDate);
                if (duty == null)
                {
                    throw new InvalidOperationException($"DutyPeriod not found for Employee Id '{employeeId}' for '{dayDate:dd/MM/yyyy}'.");
                }

                rosterWeeklyOffDays += duty.IsWeeklyOff ? 1 : 0;
            }

            return rosterWeeklyOffDays;
        }

        var employeeDutyPeriods = _employeeDutyPeriodRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId && !(r.StartDate > startDate || r.EndDate < endDate))
            .ToList();

        var employeeSubstituteDuties = _employeeSubstituteDutyRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId && r.SubstituteDate >= startDate && r.SubstituteDate <= endDate)
            .ToList();

        double weeklyOffDays = 0;

        for (var dayDate = startDate; dayDate <= endDate; dayDate = dayDate.AddDays(1))
        {
            if (daysAlreadyCovered.Contains(dayDate))
            {
                continue;
            }

            var substituteDuty = employeeSubstituteDuties.Where(r => r.SubstituteDate == dayDate).Select(r => r.Duty).FirstOrDefault();
            var duty = substituteDuty;

            if (duty == null)
            {
                var dutyPeriod = employeeDutyPeriods
                    .Where(r => r.StartDate <= dayDate && r.EndDate >= dayDate)
                    .Select(r => r.DutyPeriod)
                    .FirstOrDefault();

                if (dutyPeriod == null)
                {
                    throw new InvalidOperationException($"DutyPeriod not found for Employee Id '{employeeId}' for '{dayDate:dd/MM/yyyy}'.");
                }

                duty = dayDate.DayOfWeek switch
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

            weeklyOffDays += duty != null && duty.IsWeeklyOff ? 1 : 0;
        }

        return weeklyOffDays;
    }

    #endregion

    #region << GetMaxIndemnityLimit >>

    public async Task<double> GetMaxIndemnityLimitAsync(int employeeId, DateTime payDate, int? employeeGroupId, int? indemnityPayRuleId, bool enableUnpaidDedSlabWise, List<IndemnitySlab> indemnitySlabs)
    {
        double daysCount = 0;

        double currentPay = 0;
        double monthCount = 0; // As per Kuwait labour law, 18 months
        double maxAccruePercentage = 0;

        var employeeGroup = employeeGroupId.HasValue ? _employeeGroupRepository.GetAll().FirstOrDefault(r => r.Id == employeeGroupId.Value) : null;
        var indemnityPayRule = indemnityPayRuleId.HasValue ? _indemnityPayRuleRepository.GetAll().FirstOrDefault(r => r.Id == indemnityPayRuleId.Value) : null;

        if (employeeGroup != null)
        {
            maxAccruePercentage = employeeGroup.IndemnityMaxAccruePercentage;

            if (employeeGroup.IndemnityMaxUnit == Unit.Month)
            {
                monthCount = employeeGroup.IndemnityMaxNumber;
            }
            else if (employeeGroup.IndemnityMaxUnit == Unit.Day)
            {
                monthCount = employeeGroup.IndemnityMaxNumber / 30;
            }
        }
        else if (indemnityPayRule != null)
        {
            maxAccruePercentage = indemnityPayRule.MaxPayPercentage;

            if (indemnityPayRule.MaxUnit == Unit.Month)
            {
                monthCount = indemnityPayRule.MaxNumber;
            }
            else if (indemnityPayRule.MaxUnit == Unit.Day)
            {
                monthCount = indemnityPayRule.MaxNumber / 30;
            }
        }

        var empBasicPay = _employeeBasicPayRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId && r.StartDate <= payDate && r.EndDate >= payDate)
            .FirstOrDefault();

        if (empBasicPay != null)
        {
            currentPay = _employeeBasicPayDetailRepository.GetAll()
                .Where(r => r.EmployeeBasicPayId == empBasicPay.Id && r.WageType.IncludeInIndemnity)
                .Sum(r => (double?)r.Amount) ?? 0;
        }

        var totalIndemnityPayLimit = Math.Round(currentPay * monthCount * (maxAccruePercentage / 100), 3);

        var fixedDays = _wageTypeRepository.GetAll().Where(r => r.Code == "1010").Select(r => r.FixedDays).FirstOrDefault();
        var totalBasicPay = currentPay;

        if (totalBasicPay != 0)
        {
            if (enableUnpaidDedSlabWise && indemnitySlabs != null)
            {
                var remainingIndemnityAmount = totalIndemnityPayLimit;

                foreach (var slab in indemnitySlabs.OrderBy(r => r.SrNo))
                {
                    var basisDays = slab.BasisDays == 0 ? fixedDays : slab.BasisDays;
                    var slabIndemnityDays = slab.IndemnityDays;

                    var indemnityAmount = (totalBasicPay / basisDays) * slabIndemnityDays;
                    if (indemnityAmount < remainingIndemnityAmount)
                    {
                        daysCount += slabIndemnityDays;
                        remainingIndemnityAmount -= indemnityAmount;
                    }
                    else
                    {
                        daysCount += (remainingIndemnityAmount * basisDays) / totalBasicPay;
                        remainingIndemnityAmount = 0;
                    }

                    if (remainingIndemnityAmount == 0)
                    {
                        break;
                    }
                }
            }
            else
            {
                daysCount = (totalIndemnityPayLimit * fixedDays) / totalBasicPay;
            }
        }

        return await Task.FromResult(daysCount);
    }

    #endregion

    #region << Accrual Start/End >>

    public Task<(DateTime DeductionStartDate, DateTime DeductionEndDate)> GetLeaveAccrualStartEndAsync(int employeeId, int absenceTypeId, DateTime endDate)
    {
        var employee = GetEmployeeOrThrow(employeeId);
        var absenceType = GetAbsenceTypeOrThrow(absenceTypeId);

        DateTime deductionStartDate;
        DateTime deductionEndDate;

        if (absenceType.AccrualPeriod == AccrualPeriod.PerEmployment)
        {
            deductionStartDate = employee.JoiningDate;
            deductionEndDate = _employeeContractRepository.GetAll()
                .Where(r => r.EmployeeId == employeeId)
                .OrderByDescending(r => r.EndDate)
                .Select(r => r.EndDate)
                .FirstOrDefault();

            if (deductionStartDate == default || deductionEndDate == default)
            {
                throw new InvalidOperationException(
                    $"JoiningDate or Contract not found for Employee Id '{employeeId}', Leave Type '{absenceType.NameEn}', Accrual Period '{AccrualPeriod.PerEmployment}'.");
            }
        }
        else
        {
            var joiningDate = ResolveAccrualPeriodStartDate(employee.JoiningDate, absenceType.AccrualPeriod, absenceType.CustomStartMonth);

            if (absenceType.ReInitialize)
            {
                var reInitializeAfter = absenceType.ReInitializeAfter == 0 ? 1 : absenceType.ReInitializeAfter;
                deductionStartDate = joiningDate;
                deductionEndDate = joiningDate.AddYears(reInitializeAfter).AddDays(-1);
                while (deductionEndDate < endDate)
                {
                    deductionStartDate = deductionEndDate.AddDays(1);
                    deductionEndDate = deductionStartDate.AddYears(reInitializeAfter).AddDays(-1);
                }
            }
            else
            {
                deductionStartDate = joiningDate;
                deductionEndDate = new DateTime(9999, 12, 31);
            }
        }

        return Task.FromResult((deductionStartDate, deductionEndDate));
    }

    public Task<(DateTime DeductionStartDate, DateTime DeductionEndDate)> GetAirTicketAccrualStartEndAsync(int employeeId, int ticketTypeId, AccrualPeriod accrualPeriod, DateTime endDate)
    {
        var employee = GetEmployeeOrThrow(employeeId);
        var ticketType = _ticketTypeRepository.GetAll().FirstOrDefault(r => r.Id == ticketTypeId)
                          ?? throw new InvalidOperationException($"TicketType Id '{ticketTypeId}' not found.");

        DateTime deductionStartDate;
        DateTime deductionEndDate;

        if (accrualPeriod == AccrualPeriod.PerEmployment)
        {
            deductionStartDate = employee.JoiningDate;
            deductionEndDate = _employeeContractRepository.GetAll()
                .Where(r => r.EmployeeId == employeeId)
                .OrderByDescending(r => r.EndDate)
                .Select(r => r.EndDate)
                .FirstOrDefault();

            if (deductionStartDate == default || deductionEndDate == default)
            {
                throw new InvalidOperationException(
                    $"JoiningDate or Contract not found for Employee Id '{employeeId}', Ticket Type '{ticketType.NameEn}', Accrual Period '{AccrualPeriod.PerEmployment}'.");
            }
        }
        else if (accrualPeriod == AccrualPeriod.BiAnnual)
        {
            var joiningDate = employee.JoiningDate;
            if (joiningDate != default)
            {
                joiningDate = new DateTime(joiningDate.Year, 1, 1);
            }

            const int reInitializeAfter = 2;
            deductionStartDate = joiningDate;
            deductionEndDate = joiningDate.AddYears(reInitializeAfter).AddDays(-1);
            while (deductionEndDate < endDate)
            {
                deductionStartDate = deductionEndDate.AddDays(1);
                deductionEndDate = deductionStartDate.AddYears(reInitializeAfter).AddDays(-1);
            }
        }
        else
        {
            var joiningDate = employee.JoiningDate;
            if (joiningDate != default)
            {
                if (accrualPeriod == AccrualPeriod.CalendarYear)
                {
                    joiningDate = new DateTime(joiningDate.Year, 1, 1);
                }
                else if (accrualPeriod == AccrualPeriod.FiscalYear)
                {
                    joiningDate = new DateTime(joiningDate.Year, 4, 1);
                }
            }

            if (ticketType.Lapse)
            {
                var reInitializeAfter = ticketType.LapseAfter;
                deductionStartDate = joiningDate;
                deductionEndDate = joiningDate.AddYears(reInitializeAfter).AddDays(-1);
                while (deductionEndDate < endDate)
                {
                    deductionStartDate = deductionEndDate.AddDays(1);
                    deductionEndDate = deductionStartDate.AddYears(reInitializeAfter).AddDays(-1);
                }
            }
            else
            {
                deductionStartDate = joiningDate;
                deductionEndDate = new DateTime(9999, 12, 31);
            }
        }

        return Task.FromResult((deductionStartDate, deductionEndDate));
    }

    public (DateTime DeductionStartDate, DateTime DeductionEndDate) GetHealthReimbAccrualStartEnd(DateTime joiningDate, CustomStartMonth healthInsurancePeriod, DateTime endDate)
    {
        var adjustedJoiningDate = joiningDate;
        if (adjustedJoiningDate != default)
        {
            adjustedJoiningDate = healthInsurancePeriod == CustomStartMonth.None
                ? adjustedJoiningDate
                : new DateTime(adjustedJoiningDate.Year, (int)healthInsurancePeriod, 1);
        }

        // Legacy always reinitializes yearly for health reimbursement (reInitialize = true, reInitializeAfter = 1),
        // there is no configurable flag for this accrual type.
        const int reInitializeAfter = 1;
        var deductionStartDate = adjustedJoiningDate;
        var deductionEndDate = adjustedJoiningDate.AddYears(reInitializeAfter).AddDays(-1);
        while (deductionEndDate < endDate)
        {
            deductionStartDate = deductionEndDate.AddDays(1);
            deductionEndDate = deductionStartDate.AddYears(reInitializeAfter).AddDays(-1);
        }

        return (deductionStartDate, deductionEndDate);
    }

    private static DateTime ResolveAccrualPeriodStartDate(DateTime joiningDate, AccrualPeriod accrualPeriod, CustomStartMonth customStartMonth)
    {
        if (joiningDate == default)
        {
            return joiningDate;
        }

        if (accrualPeriod == AccrualPeriod.CalendarYear)
        {
            return new DateTime(joiningDate.Year, 1, 1);
        }

        if (accrualPeriod == AccrualPeriod.FiscalYear)
        {
            return new DateTime(joiningDate.Year, 4, 1);
        }

        if (accrualPeriod == AccrualPeriod.Custom && customStartMonth != CustomStartMonth.None)
        {
            return new DateTime(joiningDate.Year, (int)customStartMonth, 1);
        }

        return joiningDate;
    }

    #endregion

    #region << Pro-Rata / Projection >>

    public double GetProRataBasedBalanceQuota(DateTime employeeJoiningDate, AccrualPeriod accrualPeriod, LeaveEntitlement leaveEntitlement, double balanceQuota, DateTime deductionStartDate, DateTime deductionEndDate)
    {
        var result = balanceQuota;

        if (employeeJoiningDate > deductionStartDate && accrualPeriod == AccrualPeriod.CalendarYear && leaveEntitlement == LeaveEntitlement.Prorated)
        {
            var actualDays = deductionEndDate.Subtract(deductionStartDate).TotalDays + 1;
            var remainingDays = deductionEndDate.Subtract(employeeJoiningDate).TotalDays + 1;
            if (actualDays != 0)
            {
                result = Math.Round((balanceQuota * remainingDays) / actualDays, 2);
            }
        }

        return result;
    }

    public async Task<double> GetAsOnDateQuotaWithProjectionAsync(int employeeId, int? employeeSubGroupId, int absenceTypeId, DateTime asOnDate)
    {
        var employee = GetEmployeeOrThrow(employeeId);
        var absenceType = GetAbsenceTypeOrThrow(absenceTypeId);

        double asOnDateTotalBalanceQuota = 0;

        if (asOnDate <= employee.JoiningDate)
        {
            return asOnDateTotalBalanceQuota;
        }

        var annualLeaveType = _absenceTypeRepository.GetAll().FirstOrDefault(r => r.Code == "1");
        const int leaveRoundoff = 2;

        double totalDaysAllowed = absenceType.TotalDaysAllowed;
        double maxLimit = 0;
        var empSubGroupAbs = GetEmployeeSubGroupLeave(employeeSubGroupId, employee.JoiningDate, absenceTypeId, asOnDate);
        if (empSubGroupAbs != null)
        {
            totalDaysAllowed = empSubGroupAbs.TotalDays;
            maxLimit = empSubGroupAbs.MaxLimit;
        }

        if (!absenceType.HaveQuotaAccrual)
        {
            return asOnDateTotalBalanceQuota;
        }

        var empAbsenceQuota = _employeeAbsenceQuotaRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId && r.EntitlementFrom <= asOnDate && r.EntitlementTo >= asOnDate && r.AbsenceTypeId == absenceTypeId)
            .FirstOrDefault();

        if (empAbsenceQuota == null)
        {
            return asOnDateTotalBalanceQuota;
        }

        var timeSettings = await _sysSettingsAccessor.GetTimeSettingsAsync();
        var earliestEvaluationDate = timeSettings?.EarliestEvaluationDate ?? default;
        var forfeitRule = timeSettings?.ForfeitRule ?? ForfeitRule.None;

        var startDate = new DateTime(asOnDate.Year, 1, 1);
        var endDate = new DateTime(asOnDate.Year, 12, 31);
        var totalDaysInYear = endDate.Subtract(startDate).TotalDays + 1;
        var perDayQuota = totalDaysAllowed / totalDaysInYear;

        var isAnnualLeave = (annualLeaveType != null && absenceTypeId == annualLeaveType.Id) || absenceType.QuotaType == QuotaType.AnnualLeave;

        double asOnDateTotalAccrual;
        if (isAnnualLeave)
        {
            var empTimeRegister = _timeRegisterRepository.GetAll()
                .Where(r => r.EmployeeId == employeeId && r.DayDate >= earliestEvaluationDate && r.DayDate <= asOnDate)
                .ToList();

            var lastDateOfTe = earliestEvaluationDate > employee.JoiningDate ? earliestEvaluationDate : employee.JoiningDate;
            double totalAccrualInTe = 0;
            if (empTimeRegister.Count > 0)
            {
                lastDateOfTe = empTimeRegister.Max(r => r.DayDate);
                totalAccrualInTe = empTimeRegister.Sum(r => r.AnnualLeaveAccrualPerDay);
            }

            var totalDaysNotInTe = asOnDate.Subtract(lastDateOfTe).TotalDays;
            var totalAccrualNotInTe = totalDaysNotInTe * perDayQuota;

            asOnDateTotalAccrual = totalAccrualInTe + totalAccrualNotInTe;
        }
        else
        {
            asOnDateTotalAccrual = _employeeAttendanceRepository.GetAll()
                .Where(r => r.EmployeeId == employeeId && r.AttendanceType.LinkLeaveQuotaId == absenceTypeId)
                .Sum(r => (double?)r.TotalAttendDays) ?? 0;
        }

        double asOnDateTotalUsedQuota;
        if (isAnnualLeave)
        {
            asOnDateTotalUsedQuota = _employeeAbsenceRepository.GetAll()
                .Where(r => (r.AbsenceTypeId == absenceTypeId || r.AbsenceType.QuotaType == QuotaType.AnnualLeave)
                         && r.EmployeeId == employeeId
                         && r.StartDate >= earliestEvaluationDate && r.EndDate <= asOnDate)
                .Sum(r => (double?)r.TotalLeaveDays) ?? 0;

            var overlapStartDate = _employeeAbsenceRepository.GetAll()
                .Where(r => (r.AbsenceTypeId == absenceTypeId || r.AbsenceType.QuotaType == QuotaType.AnnualLeave)
                         && r.EmployeeId == employeeId
                         && r.StartDate <= earliestEvaluationDate && r.EndDate > asOnDate)
                .Select(r => (DateTime?)r.StartDate)
                .FirstOrDefault();

            if (overlapStartDate.HasValue && overlapStartDate.Value != default)
            {
                var leaveOverlapDays = _timeRegisterRepository.GetAll()
                    .Where(r => r.EmployeeId == employeeId && r.DayDate >= overlapStartDate.Value && r.DayDate <= asOnDate)
                    .Sum(r => (double?)r.AnnualLeaveDay) ?? 0;

                asOnDateTotalUsedQuota += leaveOverlapDays;
            }
        }
        else
        {
            asOnDateTotalUsedQuota = _employeeAbsenceRepository.GetAll()
                .Where(r => r.AbsenceTypeId == absenceTypeId && r.EmployeeId == employeeId
                         && r.StartDate >= earliestEvaluationDate && r.EndDate <= asOnDate)
                .Sum(r => (double?)r.TotalLeaveDays) ?? 0;
        }

        double asOnDateTotalAdjustQuota;
        if (isAnnualLeave)
        {
            asOnDateTotalAdjustQuota = _employeeAbsAdjustmentRepository.GetAll()
                .Where(r => (r.AbsenceType.Code == absenceType.Code || r.AbsenceType.QuotaType == QuotaType.AnnualLeave)
                         && r.EmployeeId == employeeId
                         && r.EffectiveDate >= earliestEvaluationDate && r.EffectiveDate <= asOnDate
                         && r.AdjustmentType != AdjustmentType.NoImpactOnQuota)
                .Sum(r => (double?)r.QuotaAdjusted) ?? 0;
        }
        else
        {
            asOnDateTotalAdjustQuota = _employeeAbsAdjustmentRepository.GetAll()
                .Where(r => r.AbsenceTypeId == absenceTypeId && r.EmployeeId == employeeId
                         && r.EffectiveDate >= earliestEvaluationDate && r.EffectiveDate <= asOnDate
                         && r.AdjustmentType != AdjustmentType.NoImpactOnQuota)
                .Sum(r => (double?)r.QuotaAdjusted) ?? 0;
        }

        asOnDateTotalBalanceQuota = asOnDateTotalAccrual - asOnDateTotalUsedQuota - asOnDateTotalAdjustQuota;

        if (isAnnualLeave)
        {
            if (forfeitRule == ForfeitRule.Monthly)
            {
                var asOnDateTotalForfeitQuota = _employeeAbsQuotaForfeitRepository.GetAll()
                    .Where(r => r.EmployeeId == employeeId
                             && (r.AbsenceType.Code == absenceType.Code || r.AbsenceType.QuotaType == QuotaType.AnnualLeave)
                             && r.EffectiveDate >= earliestEvaluationDate && r.EffectiveDate <= asOnDate)
                    .Sum(r => (double?)r.QuotaForfeited) ?? 0;

                asOnDateTotalBalanceQuota = Math.Round(asOnDateTotalBalanceQuota - asOnDateTotalForfeitQuota, leaveRoundoff);
            }
            else
            {
                asOnDateTotalBalanceQuota = maxLimit != 0 && asOnDateTotalBalanceQuota > maxLimit
                    ? Math.Round(maxLimit, leaveRoundoff)
                    : Math.Round(asOnDateTotalBalanceQuota, leaveRoundoff);
            }
        }

        return asOnDateTotalBalanceQuota;
    }

    #endregion

    #region << Forfeit Calculation >>

    public async Task<ForfeitCalculationResult> CalculateMonthlyForfeitAsync(int employeeId, int? employeeSubGroupId, DateTime monthStartDate, DateTime monthEndDate)
    {
        var result = new ForfeitCalculationResult();

        var employee = GetEmployeeOrThrow(employeeId);
        var timeSettings = await _sysSettingsAccessor.GetTimeSettingsAsync();
        var earliestEvaluationDate = timeSettings?.EarliestEvaluationDate ?? default;
        var annualLeaveType = _absenceTypeRepository.GetAll().FirstOrDefault(r => r.Code == "1");
        if (annualLeaveType == null)
        {
            return result;
        }

        var prevMonthStartDate = earliestEvaluationDate < employee.JoiningDate ? employee.JoiningDate : earliestEvaluationDate;
        var prevMonthEndDate = monthStartDate.AddDays(-1);

        var empAbsenceQuota = _employeeAbsenceQuotaRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId && r.AbsenceTypeId == annualLeaveType.Id
                     && r.DeductionFrom <= monthEndDate && r.DeductionTo >= monthEndDate)
            .FirstOrDefault();

        if (empAbsenceQuota == null)
        {
            return result;
        }

        double maxLimit = 0;
        var empSubGroupAbs = GetEmployeeSubGroupLeave(employeeSubGroupId, employee.JoiningDate, annualLeaveType.Id, monthEndDate);
        if (empSubGroupAbs != null)
        {
            maxLimit = empSubGroupAbs.MaxLimit;
        }

        if (maxLimit <= 0)
        {
            return result;
        }

        double openingBalance = 0;
        if (prevMonthStartDate < prevMonthEndDate)
        {
            openingBalance = await CalculateOpeningBalanceAsync(employeeId, annualLeaveType, prevMonthStartDate, prevMonthEndDate, excludeEmployeeAbsenceId: null);
        }

        var (totalForfeitDays, _) = await CalculateClosingBalanceAndForfeitAsync(employeeId, annualLeaveType.Id, monthStartDate, monthEndDate, openingBalance, maxLimit, excludeEmployeeAbsenceId: null);

        await ApplyForfeitAsync(employeeId, annualLeaveType.Id, monthEndDate, MonthlyForfeitComment, totalForfeitDays, empAbsenceQuota, result);

        return result;
    }

    public async Task<ForfeitCalculationResult> CalculateForfeitForLeaveAsync(int employeeId, int? employeeSubGroupId, DateTime startDate, DateTime endDate, int? prevEmployeeAbsenceId)
    {
        var result = new ForfeitCalculationResult();

        var employee = GetEmployeeOrThrow(employeeId);
        var timeSettings = await _sysSettingsAccessor.GetTimeSettingsAsync();
        var earliestEvaluationDate = timeSettings?.EarliestEvaluationDate ?? default;
        var annualLeaveType = _absenceTypeRepository.GetAll().FirstOrDefault(r => r.Code == "1");
        if (annualLeaveType == null)
        {
            return result;
        }

        // Extend endDate to cover the latest of TimeRegister/Absence/Adjustment/Forfeit for this employee's
        // annual-leave-linked records, matching legacy's "widen the window" behaviour.
        var timeRegEndDate = _timeRegisterRepository.GetAll().Where(r => r.EmployeeId == employeeId).Select(r => (DateTime?)r.DayDate).DefaultIfEmpty().Max();
        var absEndDate = _employeeAbsenceRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId && (r.AbsenceTypeId == annualLeaveType.Id || r.AbsenceType.QuotaType == QuotaType.AnnualLeave))
            .Select(r => (DateTime?)r.EndDate).DefaultIfEmpty().Max();
        var absAdjustEndDate = _employeeAbsAdjustmentRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId && (r.AbsenceTypeId == annualLeaveType.Id || r.AbsenceType.QuotaType == QuotaType.AnnualLeave) && r.AdjustmentType != AdjustmentType.NoImpactOnQuota)
            .Select(r => (DateTime?)r.EffectiveDate).DefaultIfEmpty().Max();
        var absForfeitEndDate = _employeeAbsQuotaForfeitRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId && (r.AbsenceTypeId == annualLeaveType.Id || r.AbsenceType.QuotaType == QuotaType.AnnualLeave))
            .Select(r => (DateTime?)r.EffectiveDate).DefaultIfEmpty().Max();

        if (timeRegEndDate.HasValue && timeRegEndDate.Value > endDate) endDate = timeRegEndDate.Value;
        if (absEndDate.HasValue && absEndDate.Value > endDate) endDate = absEndDate.Value;
        if (absAdjustEndDate.HasValue && absAdjustEndDate.Value > endDate) endDate = absAdjustEndDate.Value;
        if (absForfeitEndDate.HasValue && absForfeitEndDate.Value > endDate) endDate = absForfeitEndDate.Value;

        var recoveryPeriods = DateRangeUtils.GetMonthYearList(startDate, endDate);

        double openingBalance = 0;
        var isOpeningCalculated = false;
        double maxLimit = 0;
        var totalForfeitDaysAcrossPeriods = 0d;

        foreach (var rec in recoveryPeriods)
        {
            var monthStartDate = new DateTime(rec.Year, rec.Month, 1);
            var monthEndDate = new DateTime(rec.Year, rec.Month, DateTime.DaysInMonth(rec.Year, rec.Month));

            var prevMonthStartDate = earliestEvaluationDate < employee.JoiningDate ? employee.JoiningDate : earliestEvaluationDate;
            var prevMonthEndDate = monthStartDate.AddDays(-1);

            var empSubGroupAbs = GetEmployeeSubGroupLeave(employeeSubGroupId, employee.JoiningDate, annualLeaveType.Id, monthEndDate);
            if (empSubGroupAbs != null)
            {
                maxLimit = empSubGroupAbs.MaxLimit;
            }

            if (maxLimit <= 0)
            {
                continue;
            }

            if (!isOpeningCalculated && prevMonthStartDate < prevMonthEndDate)
            {
                openingBalance = await CalculateOpeningBalanceAsync(employeeId, annualLeaveType, prevMonthStartDate, prevMonthEndDate, prevEmployeeAbsenceId);
            }

            var (monthForfeitDays, closingBalance) = await CalculateClosingBalanceAndForfeitAsync(employeeId, annualLeaveType.Id, monthStartDate, monthEndDate, openingBalance, maxLimit, prevEmployeeAbsenceId);

            var empAbsenceQuota = _employeeAbsenceQuotaRepository.GetAll()
                .Where(r => r.EmployeeId == employeeId && r.AbsenceTypeId == annualLeaveType.Id
                         && r.DeductionFrom <= monthEndDate && r.DeductionTo >= monthEndDate)
                .FirstOrDefault();

            if (empAbsenceQuota != null)
            {
                await ApplyForfeitAsync(employeeId, annualLeaveType.Id, monthEndDate, MonthlyForfeitComment, monthForfeitDays, empAbsenceQuota, result);
                totalForfeitDaysAcrossPeriods += monthForfeitDays;
            }

            openingBalance = closingBalance;
            isOpeningCalculated = true;
        }

        result.TotalForfeitDays = totalForfeitDaysAcrossPeriods;
        return result;
    }

    /// <summary>
    /// Shared opening-balance computation (accrual - used - adjust - forfeit over
    /// [prevMonthStartDate, prevMonthEndDate]), identical logic in legacy's <c>CalculateMonthlyForfeit</c> and
    /// <c>CalculateForfeitForLeave</c>.
    /// </summary>
    private async Task<double> CalculateOpeningBalanceAsync(int employeeId, AbsenceType annualLeaveType, DateTime prevMonthStartDate, DateTime prevMonthEndDate, int? excludeEmployeeAbsenceId)
    {
        var openingAccrual = _timeRegisterRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId && r.DayDate >= prevMonthStartDate && r.DayDate <= prevMonthEndDate)
            .Sum(r => (double?)r.AnnualLeaveAccrualPerDay) ?? 0;

        var openingUsedQuery = _employeeAbsenceRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId
                     && (r.AbsenceTypeId == annualLeaveType.Id || r.AbsenceType.QuotaType == QuotaType.AnnualLeave)
                     && r.StartDate >= prevMonthStartDate && r.EndDate <= prevMonthEndDate);
        if (excludeEmployeeAbsenceId.HasValue)
        {
            openingUsedQuery = openingUsedQuery.Where(r => r.Id != excludeEmployeeAbsenceId.Value);
        }
        var openingUsed = openingUsedQuery.Sum(r => (double?)r.TotalLeaveDays) ?? 0;

        var overlapStartQuery = _employeeAbsenceRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId
                     && (r.AbsenceTypeId == annualLeaveType.Id || r.AbsenceType.QuotaType == QuotaType.AnnualLeave)
                     && r.StartDate <= prevMonthStartDate && r.EndDate > prevMonthEndDate);
        if (excludeEmployeeAbsenceId.HasValue)
        {
            overlapStartQuery = overlapStartQuery.Where(r => r.Id != excludeEmployeeAbsenceId.Value);
        }
        var overlapStart = overlapStartQuery.Select(r => (DateTime?)r.StartDate).FirstOrDefault();

        if (overlapStart.HasValue && overlapStart.Value != default)
        {
            // Legacy computes the exact overlap-day count via EmployeeAbsence.ActualLeaveDays (holiday/
            // rest-day aware): calendar days in the overlap window, minus the holiday/weekend/rest-day
            // exclusions the AbsenceType doesn't include. Now wired to the real, faithfully-ported
            // IEmployeeAbsenceCalculationService.ActualLeaveDaysAsync (see that service's class remarks) --
            // no longer a full-calendar-day approximation.
            var totalOverlapWindowDays = (prevMonthEndDate - overlapStart.Value).TotalDays + 1;
            var excludedOverlapDays = (double)await _employeeAbsenceCalculationService.ActualLeaveDaysAsync(employeeId, overlapStart.Value, prevMonthEndDate, annualLeaveType.Id);
            var leaveOverlapDays = totalOverlapWindowDays - excludedOverlapDays;
            openingUsed += leaveOverlapDays;
        }

        var openingAdjust = _employeeAbsAdjustmentRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId
                     && (r.AbsenceTypeId == annualLeaveType.Id || r.AbsenceType.QuotaType == QuotaType.AnnualLeave)
                     && r.EffectiveDate >= prevMonthStartDate && r.EffectiveDate <= prevMonthEndDate
                     && r.AdjustmentType != AdjustmentType.NoImpactOnQuota)
            .Sum(r => (double?)r.QuotaAdjusted) ?? 0;

        var openingForfeit = _employeeAbsQuotaForfeitRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId
                     && (r.AbsenceTypeId == annualLeaveType.Id || r.AbsenceType.QuotaType == QuotaType.AnnualLeave)
                     && r.EffectiveDate >= prevMonthStartDate && r.EffectiveDate <= prevMonthEndDate)
            .Sum(r => (double?)r.QuotaForfeited) ?? 0;

        return openingAccrual - openingUsed - openingAdjust - openingForfeit;
    }

    /// <summary>
    /// Shared day-by-day closing-balance/forfeit-day loop, identical logic in legacy's
    /// <c>CalculateMonthlyForfeit</c> and <c>CalculateForfeitForLeave</c> (the latter additionally
    /// considers the in-flight <see cref="EmployeeAbsence"/> excluded by <paramref name="excludeEmployeeAbsenceId"/>).
    /// </summary>
    private async Task<(double TotalForfeitDays, double ClosingBalance)> CalculateClosingBalanceAndForfeitAsync(int employeeId, int annualLeaveTypeId, DateTime monthStartDate, DateTime monthEndDate, double openingBalance, double maxLimit, int? excludeEmployeeAbsenceId)
    {
        var timeRegisterRows = _timeRegisterRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId && r.DayDate >= monthStartDate && r.DayDate <= monthEndDate)
            .Select(r => new { r.DayDate, r.AnnualLeaveDay, r.AnnualLeaveAccrualPerDay, r.LeaveAdjDay })
            .ToList();

        var absAdjustQuery = _employeeAbsAdjustmentRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId
                     && (r.AbsenceTypeId == annualLeaveTypeId || r.AbsenceType.QuotaType == QuotaType.AnnualLeave)
                     && r.EffectiveDate >= monthStartDate && r.EffectiveDate <= monthEndDate
                     && r.AdjustmentType != AdjustmentType.NoImpactOnQuota);
        var absAdjustRows = absAdjustQuery.Select(r => new { r.EffectiveDate, r.QuotaAdjusted }).ToList();

        var overlappingAbsenceQuery = _employeeAbsenceRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId && (r.AbsenceTypeId == annualLeaveTypeId || r.AbsenceType.QuotaType == QuotaType.AnnualLeave)
                     && r.StartDate <= monthEndDate && r.EndDate >= monthStartDate);
        if (excludeEmployeeAbsenceId.HasValue)
        {
            overlappingAbsenceQuery = overlappingAbsenceQuery.Where(r => r.Id != excludeEmployeeAbsenceId.Value);
        }
        var overlappingAbsences = overlappingAbsenceQuery.Select(r => new { r.StartDate, r.EndDate, r.AbsenceHours }).ToList();

        var closingBalance = openingBalance;
        double totalForfeitDays = 0;

        for (var day = monthStartDate; day <= monthEndDate; day = day.AddDays(1))
        {
            var dayAccrual = timeRegisterRows.Where(r => r.DayDate == day).Sum(r => r.AnnualLeaveAccrualPerDay);

            double dayUsed;
            var timeRegDay = timeRegisterRows.Where(r => r.DayDate == day).ToList();
            if (timeRegDay.Count > 0)
            {
                // CalculateMonthlyForfeit's shape: leave usage comes straight from TimeRegister for the day.
                dayUsed = timeRegDay.Sum(r => r.AnnualLeaveDay + r.LeaveAdjDay);
            }
            else
            {
                // CalculateForfeitForLeave's shape: no TimeRegister row for the day yet -- derive usage from
                // any EmployeeAbsence overlapping this day.
                var abs = overlappingAbsences.FirstOrDefault(r => r.StartDate <= day && r.EndDate >= day);
                if (abs != null)
                {
                    if (abs.AbsenceHours == 0)
                    {
                        // Legacy computes `1 - ActualLeaveDays(...)` here (holiday/rest-day exclusion). Now
                        // wired to the real IEmployeeAbsenceCalculationService.ActualLeaveDaysAsync.
                        var excludedDays = (double)await _employeeAbsenceCalculationService.ActualLeaveDaysAsync(employeeId, day, day, annualLeaveTypeId);
                        dayUsed = 1 - excludedDays;
                    }
                    else
                    {
                        // Legacy computes the hourly-leave day-fraction via EmployeeAbsence.GetHoulyLeaveDay.
                        // Now wired to the real IEmployeeAbsenceCalculationService.GetHoulyLeaveDayAsync (no
                        // longer an AbsenceHours/8 approximation).
                        dayUsed = await _employeeAbsenceCalculationService.GetHoulyLeaveDayAsync(employeeId, day, abs.AbsenceHours);
                    }
                }
                else
                {
                    dayUsed = 0;
                }
            }

            var dayAdjust = absAdjustRows.Where(r => r.EffectiveDate == day).Sum(r => r.QuotaAdjusted);

            closingBalance = closingBalance + dayAccrual - dayUsed - dayAdjust;

            var forfeitDays = closingBalance > maxLimit ? closingBalance - maxLimit : 0;
            totalForfeitDays += forfeitDays;
            closingBalance = closingBalance > maxLimit ? maxLimit : closingBalance;
        }

        return (totalForfeitDays, closingBalance);
    }

    public async Task<ForfeitCalculationResult> CalculateForfeitForPublicHolidayAsync(int employeeId, int? employeeSubGroupId, int payrollAreaId, DateTime phDate, bool deleted)
    {
        var result = new ForfeitCalculationResult();

        var employee = GetEmployeeOrThrow(employeeId);
        var annualLeaveType = _absenceTypeRepository.GetAll().FirstOrDefault(r => r.Code == "1");
        if (annualLeaveType == null)
        {
            return result;
        }

        double maxLimit = 0;
        var empSubGroupAbs = GetEmployeeSubGroupLeave(employeeSubGroupId, employee.JoiningDate, annualLeaveType.Id, phDate);
        if (empSubGroupAbs != null)
        {
            maxLimit = empSubGroupAbs.MaxLimit;
        }

        if (maxLimit <= 0)
        {
            return result;
        }

        var payrollControlRecord = _payrollControlRecordRepository.GetAll().FirstOrDefault(r => r.PayrollAreaId == payrollAreaId)
                                    ?? throw new InvalidOperationException($"PayrollControlRecord does not exist for Employee Id '{employeeId}', PayrollArea Id '{payrollAreaId}'.");

        var currPayrollControlDate = payrollControlRecord.PeriodClosed
            ? payrollControlRecord.CalendarMonthEnd.AddDays(1)
            : payrollControlRecord.CalendarMonthStart;

        if (currPayrollControlDate <= phDate)
        {
            return result;
        }

        var empAbsenceQuota = _employeeAbsenceQuotaRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId && r.AbsenceTypeId == annualLeaveType.Id
                     && r.DeductionFrom <= phDate && r.DeductionTo >= phDate)
            .FirstOrDefault();

        if (empAbsenceQuota == null)
        {
            return result;
        }

        if (!deleted)
        {
            const double phForfeitDays = 1;
            await ApplyForfeitAsync(employeeId, annualLeaveType.Id, phDate, PublicHolidayForfeitComment, phForfeitDays, empAbsenceQuota, result);
        }
        else
        {
            var existing = _employeeAbsQuotaForfeitRepository.GetAll()
                .Where(r => r.EmployeeId == employeeId && r.AbsenceTypeId == annualLeaveType.Id
                         && r.EffectiveDate == phDate && r.Comments == PublicHolidayForfeitComment)
                .FirstOrDefault();

            if (existing != null)
            {
                empAbsenceQuota.QuotaForfeited = Math.Round(empAbsenceQuota.QuotaForfeited - existing.QuotaForfeited, 6);
                await _employeeAbsenceQuotaRepository.UpdateAsync(empAbsenceQuota);
                await _employeeAbsQuotaForfeitRepository.DeleteAsync(existing);
                result.ForfeitRecordDeleted = true;
            }
        }

        return result;
    }

    /// <summary>
    /// Shared create/update/delete of an <see cref="EmployeeAbsQuotaForfeit"/> row plus the parent
    /// <see cref="EmployeeAbsenceQuota.QuotaForfeited"/> adjustment. See class remarks for the
    /// <c>DontUpdateQuotaBalance</c> parity note.
    /// </summary>
    private async Task ApplyForfeitAsync(int employeeId, int absenceTypeId, DateTime effectiveDate, string comment, double totalForfeitDays, EmployeeAbsenceQuota empAbsenceQuota, ForfeitCalculationResult result)
    {
        var existing = _employeeAbsQuotaForfeitRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId && r.AbsenceTypeId == absenceTypeId && r.EffectiveDate == effectiveDate && r.Comments == comment)
            .FirstOrDefault();

        result.TotalForfeitDays = totalForfeitDays;

        if (totalForfeitDays == 0)
        {
            if (existing != null)
            {
                empAbsenceQuota.QuotaForfeited = Math.Round(empAbsenceQuota.QuotaForfeited - existing.QuotaForfeited, 6);
                await _employeeAbsenceQuotaRepository.UpdateAsync(empAbsenceQuota);
                await _employeeAbsQuotaForfeitRepository.DeleteAsync(existing);
                result.ForfeitRecordDeleted = true;
            }

            return;
        }

        double prevForfeitQuota = 0;
        var isNew = existing == null;
        if (isNew)
        {
            existing = new EmployeeAbsQuotaForfeit
            {
                TenantId = empAbsenceQuota.TenantId,
                EmployeeId = employeeId,
                AbsenceTypeId = absenceTypeId,
                EffectiveDate = effectiveDate,
                Comments = comment
            };
        }
        else
        {
            prevForfeitQuota = existing.QuotaForfeited;
        }

        empAbsenceQuota.QuotaForfeited = Math.Round(empAbsenceQuota.QuotaForfeited - prevForfeitQuota + totalForfeitDays, 6);
        await _employeeAbsenceQuotaRepository.UpdateAsync(empAbsenceQuota);

        existing.QuotaForfeited = totalForfeitDays;

        if (isNew)
        {
            await _employeeAbsQuotaForfeitRepository.InsertAsync(existing);
            result.ForfeitRecordCreated = true;
        }
        else
        {
            await _employeeAbsQuotaForfeitRepository.UpdateAsync(existing);
            result.ForfeitRecordUpdated = true;
        }
    }

    #endregion

    #region << Indemnity Calculation >>

    public async Task<double> GetTotalIndemnityDaysAsync(int employeeId, DateTime payDate, int? employeeGroupId, int? employeeSubGroupId, int? indemnityPayRuleId)
    {
        var indemnitySlabs = await GetTotalIndemnityTableAsync(employeeId, payDate, employeeSubGroupId);
        var daysCount = indemnitySlabs.Sum(r => r.IndemnityDays);

        var payrollSettings = await _sysSettingsAccessor.GetPayrollSettingsAsync();
        var enableUnpaidDedSlabWise = payrollSettings?.EnableUnpaidDedSlabWise ?? false;

        if (daysCount != 0 && employeeGroupId.HasValue)
        {
            var employeeGroup = _employeeGroupRepository.GetAll().FirstOrDefault(r => r.Id == employeeGroupId.Value);
            if (employeeGroup != null && employeeGroup.IndemnityMaxNumber != 0)
            {
                var maxIndemnityLimit = await GetMaxIndemnityLimitAsync(employeeId, payDate, employeeGroupId, indemnityPayRuleId, enableUnpaidDedSlabWise, indemnitySlabs);
                daysCount = daysCount > maxIndemnityLimit ? maxIndemnityLimit : daysCount;
            }
        }

        return daysCount;
    }

    public async Task<List<IndemnitySlab>> GetTotalIndemnityTableAsync(int employeeId, DateTime payDate, int? employeeSubGroupId)
    {
        var indemnitySlabs = new List<IndemnitySlab>();

        var payrollSettings = await _sysSettingsAccessor.GetPayrollSettingsAsync();
        var enableUnpaidDedSlabWise = payrollSettings?.EnableUnpaidDedSlabWise ?? false;

        var empContractList = _employeeContractRepository.GetAll().Where(r => r.EmployeeId == employeeId).ToList();
        var empGratuityList = _employeeGratuityRepository.GetAll().Where(r => r.EmployeeId == employeeId).ToList();
        // GetAllIncluding (ABP's IRepository API -- NOT Microsoft.EntityFrameworkCore's .Include(), which is
        // unavailable in HCMv3.Core) is required here: this list is materialised eagerly and
        // GetTotalUnpaidLeaveDays below dereferences r.TransferType.Code in memory. Lazy-loading proxies are
        // not enabled, so without the eager load TransferType is null and every employee with even one
        // TimeTransfer row throws NullReferenceException.
        var empTimeTransferList = _timeTransferRepository.GetAllIncluding(r => r.TransferType).Where(r => r.EmployeeId == employeeId).ToList();
        var empTimeRegister = _timeRegisterRepository.GetAll().Where(r => r.EmployeeId == employeeId);
        var empAbsAdjustmentList = _employeeAbsAdjustmentRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId && r.AdjustmentType != AdjustmentType.NoImpactOnQuota)
            .ToList();
        var empSubGroupGratuityList = employeeSubGroupId.HasValue
            ? _employeeSubGroupGratuityRepository.GetAll().Where(r => r.EmployeeSubGroupId == employeeSubGroupId.Value).ToList()
            : new List<EmployeeSubGroupGratuity>();
        var unpaidLeaveTypeId = _absenceTypeRepository.GetAll().Where(r => r.Code == "2").Select(r => r.Id).FirstOrDefault();

        // Legacy GetTotalUnpaidLeaveDays (Program.cs line 3701) reads TWO DIFFERENT absence types, and this
        // port was collapsing them onto one. The EmployeeAbsAdjustment term (legacy line 3739) filters on
        // Program.UnpaidLeaveOid == code "2", but the TimeRegister.LeaveDay term (legacy lines 3734-3737)
        // filters on `r.LeaveType.Code == "15"` -- Unapproved Absence Leave, a hard-coded literal in legacy.
        // Passing the code-"2" id to both summed genuine unpaid-leave LeaveDay rows into the indemnity
        // unpaid-day total on top of the UnpaidLeaveDay column that had already counted them.
        var unapprovedAbsenceLeaveTypeId = _absenceTypeRepository.GetAll().Where(r => r.Code == "15").Select(r => r.Id).FirstOrDefault();
        var timeSettings = await _sysSettingsAccessor.GetTimeSettingsAsync();
        var enableTimeRegisterApproval = timeSettings?.EnableTimeRegisterApproval ?? false;

        var payStartDate = new DateTime(payDate.Year, payDate.Month, 1);

        var empContract = empContractList
            .Where(r => r.EndDate >= payStartDate)
            .Select(r => new { r.StartDate, r.EndDate })
            .FirstOrDefault();

        if (empContract == null)
        {
            return indemnitySlabs;
        }

        var indemnityCountFromDate = empContractList.OrderBy(r => r.StartDate).Select(r => r.StartDate).FirstOrDefault();

        foreach (var empGrat in empGratuityList.Where(r => r.GratuityPaidDate <= payDate).OrderByDescending(r => r.GratuityPaidDate))
        {
            if (empGrat.ResetGratuityCalc)
            {
                indemnityCountFromDate = empGrat.GratuityPaidDate;
                break;
            }
        }

        var totalDaysCount = empContract.EndDate >= payDate
            ? IndemnityServiceDays.Count(indemnityCountFromDate, payDate)
            : IndemnityServiceDays.Count(indemnityCountFromDate, empContract.EndDate);

        if (!enableUnpaidDedSlabWise)
        {
            var absStartDate = indemnityCountFromDate;
            var absEndDate = empContract.EndDate >= payDate ? payDate : empContract.EndDate;
            var totalUnpaidDays = GetTotalUnpaidLeaveDays(absStartDate, absEndDate, empTimeRegister, empAbsAdjustmentList, empTimeTransferList, unpaidLeaveTypeId, unapprovedAbsenceLeaveTypeId, enableTimeRegisterApproval);
            totalDaysCount -= totalUnpaidDays;
        }

        if (totalDaysCount == 0 || employeeSubGroupId == null)
        {
            return indemnitySlabs;
        }

        var empSubGroupIndemnityList = empSubGroupGratuityList
            .Where(r => r.StartDate <= payDate && r.EndDate >= payDate)
            .Select(r => new { r.ChangeYear, r.GratuityDays, r.BasisDays })
            .OrderBy(r => r.ChangeYear)
            .ToList();

        if (empSubGroupIndemnityList.Count == 0)
        {
            return indemnitySlabs;
        }

        var remainingPeriod = totalDaysCount;
        double prevChangeYear = 0;
        var startDate = indemnityCountFromDate;

        foreach (var rec in empSubGroupIndemnityList)
        {
            var endDate = startDate.AddYears((int)rec.ChangeYear).AddDays(-1);
            endDate = endDate > payDate ? payDate : endDate;

            if (remainingPeriod <= (rec.ChangeYear - prevChangeYear) * 365)
            {
                var slabTotalDays = remainingPeriod;
                var slabUnpaidDays = GetTotalUnpaidLeaveDays(startDate, endDate, empTimeRegister, empAbsAdjustmentList, empTimeTransferList, unpaidLeaveTypeId, unapprovedAbsenceLeaveTypeId, enableTimeRegisterApproval);
                var slabActualDays = enableUnpaidDedSlabWise ? slabTotalDays - slabUnpaidDays : slabTotalDays;
                var slabIndemnity = (rec.GratuityDays / 365) * slabActualDays;

                AddIndemnityRecord(indemnitySlabs, startDate, endDate, slabTotalDays, slabUnpaidDays, slabActualDays, rec.GratuityDays, slabIndemnity, rec.BasisDays);
                break;
            }

            var fullSlabTotalDays = rec.ChangeYear * 365;
            var fullSlabUnpaidDays = GetTotalUnpaidLeaveDays(startDate, endDate, empTimeRegister, empAbsAdjustmentList, empTimeTransferList, unpaidLeaveTypeId, unapprovedAbsenceLeaveTypeId, enableTimeRegisterApproval);
            var fullSlabActualDays = enableUnpaidDedSlabWise ? fullSlabTotalDays - fullSlabUnpaidDays : fullSlabTotalDays;
            var fullSlabIndemnity = (rec.GratuityDays / 365) * fullSlabActualDays;

            AddIndemnityRecord(indemnitySlabs, startDate, endDate, fullSlabTotalDays, fullSlabUnpaidDays, fullSlabActualDays, rec.GratuityDays, fullSlabIndemnity, rec.BasisDays);

            remainingPeriod -= fullSlabTotalDays;
            prevChangeYear = rec.ChangeYear;
            startDate = endDate.AddDays(1);
        }

        return indemnitySlabs;
    }

    private double GetTotalUnpaidLeaveDays(DateTime startDate, DateTime endDate, IQueryable<TimeRegister> empTimeRegister, List<EmployeeAbsAdjustment> empAbsAdjustmentList, List<TimeTransfer> empTimeTransferList, int unpaidLeaveTypeId, int unapprovedAbsenceLeaveTypeId, bool enableTimeRegisterApproval)
    {
        double totalAbsCount = 0;

        var openingFromTransfer = empTimeTransferList
            // TransferType is eagerly loaded by the caller (GetAllIncluding). The null-conditional additionally
            // guards the soft-deleted-lookup case: ABP's IsDeleted filter propagates into the include, so a
            // soft-deleted TimeTransferType leaves the navigation null. Skipping such a row matches the
            // SQL-translated sibling query in GetBalanceQuotaAsync, whose join drops it too.
            .Where(r => r.TransferType?.Code == "5" && r.TransferDate >= startDate && r.TransferDate <= endDate)
            .Sum(r => r.TransferCount);
        totalAbsCount += openingFromTransfer;

        var unpaidLeaveWageTypeExists = _wageTypeRepository.GetAll().Any(r => r.Code == "4001" && r.IncludeInIndemnity);
        if (unpaidLeaveWageTypeExists)
        {
            totalAbsCount += empTimeRegister.Where(r => r.DayDate >= startDate && r.DayDate <= endDate).Sum(r => (double?)r.UnpaidLeaveDay) ?? 0;

            totalAbsCount += empTimeRegister
                // Legacy lines 3734-3737: `r.LeaveType != null && r.LeaveType.Code == "15"` -- Unapproved
                // Absence Leave (for a Planned time policy), NOT the code-"2" Unpaid Leave used by the
                // adjustment term below. A zero id (no such AbsenceType configured) matches nothing, which is
                // the same outcome as legacy's `LeaveType != null` guard on a database without code "15".
                .Where(r => r.DayDate >= startDate && r.DayDate <= endDate && r.LeaveTypeId == unapprovedAbsenceLeaveTypeId)
                .Sum(r => (double?)r.LeaveDay) ?? 0;

            totalAbsCount += empAbsAdjustmentList
                .Where(r => r.EffectiveDate >= startDate && r.EffectiveDate <= endDate && r.AbsenceTypeId == unpaidLeaveTypeId)
                .Sum(r => r.QuotaAdjusted);
        }

        var absentDayWageTypeExists = _wageTypeRepository.GetAll().Any(r => r.Code == "4029" && r.IncludeInIndemnity);
        if (absentDayWageTypeExists)
        {
            totalAbsCount += empTimeRegister
                .Where(r => r.DayDate >= startDate && r.DayDate <= endDate && r.UnpaidLeaveDay == 0)
                .Sum(r => (double?)(enableTimeRegisterApproval ? r.ApprovedAbsentDay : r.AbsentDay)) ?? 0;
        }

        return totalAbsCount;
    }

    private static void AddIndemnityRecord(List<IndemnitySlab> indemnitySlabs, DateTime startDate, DateTime endDate, double totalDays, double unpaidDays, double actualDays, double indemnityRate, double indemnityDays, double basisDays)
    {
        indemnitySlabs.Add(new IndemnitySlab
        {
            SrNo = indemnitySlabs.Count + 1,
            StartDate = startDate,
            EndDate = endDate,
            TotalDays = totalDays,
            UnpaidDays = unpaidDays,
            ActualDays = actualDays,
            IndemnityRate = indemnityRate,
            IndemnityDays = indemnityDays,
            BasisDays = basisDays
        });
    }

    #endregion

    #region << Available Negative Balance >>

    public async Task<double> GetAvailableNegativeBalanceAsync(int employeeId, bool enableNegativeBalance, double negativeBalanceLimit, double yearlyQuota, DateTime leaveStartDate)
    {
        if (!enableNegativeBalance || negativeBalanceLimit <= 0)
        {
            return 0;
        }

        var employee = GetEmployeeOrThrow(employeeId);

        var startDateOfYear = new DateTime(leaveStartDate.Year, 1, 1);
        var totalDaysInYear = new DateTime(leaveStartDate.Year, 12, 31).Subtract(startDateOfYear).TotalDays + 1;
        var perDayQuota = yearlyQuota / totalDaysInYear;

        var evaluationStartDate = employee.JoiningDate > startDateOfYear ? employee.JoiningDate : startDateOfYear;

        var lastDateOfTe = evaluationStartDate;
        var empTimeRegister = _timeRegisterRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId && r.DayDate >= evaluationStartDate && r.DayDate <= leaveStartDate)
            .ToList();

        if (empTimeRegister.Count > 0)
        {
            lastDateOfTe = empTimeRegister.Max(r => r.DayDate);
        }

        double totalAccrualNotInTe = 0;
        if (lastDateOfTe < leaveStartDate)
        {
            var totalDaysNotInTe = leaveStartDate.Subtract(lastDateOfTe).TotalDays;
            totalAccrualNotInTe = totalDaysNotInTe * perDayQuota;
        }

        var result = totalAccrualNotInTe > negativeBalanceLimit ? negativeBalanceLimit : totalAccrualNotInTe;
        if (result < 0)
        {
            result = 0;
        }

        return await Task.FromResult(Math.Round(result, 2));
    }

    #endregion

    #region << Shared Helpers >>

    private Employee GetEmployeeOrThrow(int employeeId)
    {
        return _employeeRepository.GetAll().FirstOrDefault(r => r.Id == employeeId)
               ?? throw new InvalidOperationException($"Employee Id '{employeeId}' not found.");
    }

    private AbsenceType GetAbsenceTypeOrThrow(int absenceTypeId)
    {
        return _absenceTypeRepository.GetAll().FirstOrDefault(r => r.Id == absenceTypeId)
               ?? throw new InvalidOperationException($"AbsenceType Id '{absenceTypeId}' not found.");
    }

    /// <summary>
    /// Ported from legacy <c>EmployeeSubGroupAbsence.GetEmployeeSubGroupLeave</c>: the row with the smallest
    /// <c>ChangeYear</c> strictly greater than the employee's total years-of-service as of
    /// <paramref name="dayDate"/> (a "years-of-service tiered" leave quota rule), falling back to any
    /// date-effective row with no ChangeYear tiering.
    /// </summary>
    public EmployeeSubGroupAbsence GetEmployeeSubGroupLeave(int? employeeSubGroupId, DateTime employeeJoiningDate, int absenceTypeId, DateTime dayDate)
    {
        if (employeeSubGroupId == null)
        {
            return null;
        }

        var totalYears = Math.Round((dayDate.Subtract(employeeJoiningDate).TotalDays + 1) / 365, 2);

        var tiered = _employeeSubGroupAbsenceRepository.GetAll()
            .Where(r => r.EmployeeSubGroupId == employeeSubGroupId.Value && r.AbsenceTypeId == absenceTypeId
                     && r.ChangeYear > totalYears && r.StartDate <= dayDate && r.EndDate >= dayDate)
            .OrderBy(r => r.ChangeYear)
            .FirstOrDefault();

        if (tiered != null)
        {
            return tiered;
        }

        return _employeeSubGroupAbsenceRepository.GetAll()
            .Where(r => r.EmployeeSubGroupId == employeeSubGroupId.Value && r.AbsenceTypeId == absenceTypeId
                     && r.StartDate <= dayDate && r.EndDate >= dayDate)
            .FirstOrDefault();
    }

    #endregion
}
