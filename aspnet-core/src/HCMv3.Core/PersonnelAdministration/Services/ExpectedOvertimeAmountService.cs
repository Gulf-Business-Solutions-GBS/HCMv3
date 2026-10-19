using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Services;
using HCMv3.TimeManagement.Services;

namespace HCMv3.PersonnelAdministration.Services;

/// <inheritdoc cref="IExpectedOvertimeAmountService"/>
/// <remarks>
/// Legacy source: <c>HCM10.Module/BusinessObjects/PersonnelAdministration/EmployeeOvertime.cs</c>, getter
/// lines 87-97, <c>GetExpectedOvertimeAmount</c> lines 198-302. All line numbers below refer to that file.
///
/// <para><b>Reused, not rebuilt:</b> payroll settings come from <see cref="ISysSettingsAccessor"/>
/// (legacy <c>SysService.GetPayrollSettings</c>), duty hours from <see cref="ITimeDataService"/>
/// (legacy <c>TimeDataService.GetTotalHoursInDay</c>), week-offs from <see cref="IAccrualService"/>
/// (legacy <c>AccrualService.GetActualWeekOffs(Session, emp, payDate)</c>, the XAF overload at legacy
/// <c>AccrualService.cs</c> line 278). The per-hour rate is NOT shared with
/// <see cref="EmployeeOvertimePayCalculationService"/>: that is a port of the payroll engine's
/// <c>GetOvertimePerHourRate</c>, which sums salary-register line <c>Rate</c>s and takes the payroll-engine
/// week-off count; this screen figure sums the employee's basic-pay detail <c>Amount</c>s (line 270) and
/// takes the accrual week-off count (line 284). Same shape, different inputs -- so merging them would change
/// one of the two.</para>
///
/// <para><b>Legacy behaviour kept as-is (payroll-parity rule):</b></para>
/// <list type="number">
/// <item><description>The billing method and fixed days come from the OVERTIME TYPE's wage type (line 210),
/// not from any basic-pay wage type.</description></item>
/// <item><description>No overtime-type wage type means a rate of 0, so the amount is 0, even with a Fixed
/// rate detail (lines 208, 294).</description></item>
/// <item><description>A Fixed rate still needs a non-zero hours basis (line 235), although the rate does not
/// use it.</description></item>
/// <item><description>No detail row covering the date means "Actual" (line 245).</description></item>
/// <item><description>Only the FIRST basic-pay record covering the date is used (lines 267-268); a missing
/// one gives a basic of 0.</description></item>
/// <item><description>Division by <c>FixedDays</c> / salary-basis days is unguarded (lines 274, 286) --
/// double arithmetic, so 0 days gives Infinity or NaN rather than an error.</description></item>
/// <item><description>A failing duty-hours lookup is swallowed (line 227) and falls back to the default
/// working hours (lines 229-230).</description></item>
/// <item><description>Result: negatives become 0, then rounded to 3 places (line 301), regardless of the
/// currency's decimals.</description></item>
/// </list>
///
/// <para>Plain synchronous LINQ over <c>IRepository&lt;T&gt;.GetAll()</c> (Core has no EF reference).</para>
/// </remarks>
public class ExpectedOvertimeAmountService : HCMv3DomainServiceBase, IExpectedOvertimeAmountService
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<OvertimeType> _overtimeTypeRepository;
    private readonly IRepository<OvertimeTypeDetail> _overtimeTypeDetailRepository;
    private readonly IRepository<WageType> _wageTypeRepository;
    private readonly IRepository<EmployeeBasicPay> _employeeBasicPayRepository;
    private readonly IRepository<EmployeeBasicPayDetail> _employeeBasicPayDetailRepository;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;
    private readonly ITimeDataService _timeDataService;
    private readonly IAccrualService _accrualService;

    public ExpectedOvertimeAmountService(
        IRepository<Employee> employeeRepository,
        IRepository<OvertimeType> overtimeTypeRepository,
        IRepository<OvertimeTypeDetail> overtimeTypeDetailRepository,
        IRepository<WageType> wageTypeRepository,
        IRepository<EmployeeBasicPay> employeeBasicPayRepository,
        IRepository<EmployeeBasicPayDetail> employeeBasicPayDetailRepository,
        ISysSettingsAccessor sysSettingsAccessor,
        ITimeDataService timeDataService,
        IAccrualService accrualService)
    {
        _employeeRepository = employeeRepository;
        _overtimeTypeRepository = overtimeTypeRepository;
        _overtimeTypeDetailRepository = overtimeTypeDetailRepository;
        _wageTypeRepository = wageTypeRepository;
        _employeeBasicPayRepository = employeeBasicPayRepository;
        _employeeBasicPayDetailRepository = employeeBasicPayDetailRepository;
        _sysSettingsAccessor = sysSettingsAccessor;
        _timeDataService = timeDataService;
        _accrualService = accrualService;
    }

    public async Task<double> GetExpectedOvertimeAmountAsync(int employeeId, int overtimeTypeId, DateTime effectiveDate, double approvedOvertimeHours)
    {
        if (!PassesGetterGate(employeeId, overtimeTypeId, effectiveDate, approvedOvertimeHours))
        {
            return 0;
        }

        var settings = await _sysSettingsAccessor.GetPayrollSettingsAsync();
        return await CalculateAsync(settings, employeeId, overtimeTypeId, effectiveDate, approvedOvertimeHours);
    }

    public async Task<Dictionary<int, double>> GetExpectedOvertimeAmountsAsync(IEnumerable<EmployeeOvertime> overtimes)
    {
        var result = new Dictionary<int, double>();
        var rows = overtimes?.ToList() ?? new List<EmployeeOvertime>();
        if (rows.Count == 0)
        {
            return result;
        }

        ManagePayrollSettings settings = null;

        foreach (var row in rows)
        {
            double amount = 0;
            if (PassesGetterGate(row.EmployeeId, row.OvertimeTypeId, row.EffectiveDate, row.ApprovedOvertimeHours))
            {
                settings ??= await _sysSettingsAccessor.GetPayrollSettingsAsync();
                amount = await CalculateAsync(settings, row.EmployeeId, row.OvertimeTypeId, row.EffectiveDate, row.ApprovedOvertimeHours);
            }

            result[row.Id] = amount;
        }

        return result;
    }

    /// <summary>
    /// Legacy line 92: <c>Employee != null &amp;&amp; OvertimeType != null &amp;&amp; EffectiveDate != default
    /// &amp;&amp; ApprovedOvertimeHours != 0</c>. (The <c>!IsLoading &amp;&amp; !IsSaving</c> part is XPO
    /// object-state plumbing with no equivalent here.)
    /// </summary>
    private static bool PassesGetterGate(int employeeId, int overtimeTypeId, DateTime effectiveDate, double approvedOvertimeHours)
    {
        return employeeId > 0
               && overtimeTypeId > 0
               && effectiveDate != default
               && approvedOvertimeHours != 0;
    }

    /// <summary>Legacy <c>GetExpectedOvertimeAmount</c>, lines 198-302.</summary>
    private async Task<double> CalculateAsync(
        ManagePayrollSettings settings,
        int employeeId,
        int overtimeTypeId,
        DateTime payDate,
        double approvedOvertimeHours)
    {
        var overtimeType = _overtimeTypeRepository.GetAll().FirstOrDefault(r => r.Id == overtimeTypeId);
        if (overtimeType == null)
        {
            // Legacy line 92: OvertimeType == null -> 0.
            return 0;
        }

        double expectedOvertimeAmount = 0;

        // Legacy line 203.
        if (approvedOvertimeHours > 0)
        {
            double overtimePerHourRate = 0;

            // Legacy line 206.
            var overtimeHours = approvedOvertimeHours * overtimeType.OvertimeRate;

            // Legacy lines 208-210.
            var wtOTPay = overtimeType.WageTypeId.HasValue
                ? _wageTypeRepository.GetAll().FirstOrDefault(r => r.Id == overtimeType.WageTypeId.Value)
                : null;

            if (wtOTPay != null)
            {
                var otHoursBasis = await GetOvertimeHoursBasisAsync(settings, employeeId, payDate);

                // Legacy line 235.
                if (otHoursBasis != 0)
                {
                    // Legacy lines 237-245.
                    var overtimeTypeDetail = _overtimeTypeDetailRepository.GetAll()
                        .Where(r => r.OvertimeTypeId == overtimeType.Id
                                    && r.StartDate <= payDate
                                    && r.EndDate >= payDate)
                        .OrderBy(r => r.Id)
                        .FirstOrDefault();

                    var rateType = overtimeTypeDetail != null ? overtimeTypeDetail.OvertimeRateType : OvertimeRateType.Actual;

                    if (rateType == OvertimeRateType.Fixed)
                    {
                        // Legacy line 250.
                        overtimePerHourRate = overtimeTypeDetail.Rate;
                    }
                    else if (rateType == OvertimeRateType.Actual)
                    {
                        var totalBasicPay = GetTotalBasicPay(employeeId, payDate);

                        if (wtOTPay.BillingMethod == BillingMethod.FixedDaysPerMonth)
                        {
                            // Legacy line 274.
                            overtimePerHourRate = (totalBasicPay / wtOTPay.FixedDays) / otHoursBasis;
                        }
                        else if (wtOTPay.BillingMethod == BillingMethod.CalendarDaysPerMonth)
                        {
                            // Legacy lines 278-279.
                            double calendarDays = DateTime.DaysInMonth(payDate.Year, payDate.Month);
                            overtimePerHourRate = (totalBasicPay / calendarDays) / otHoursBasis;
                        }
                        else if (wtOTPay.BillingMethod == BillingMethod.WorkingDaysPerMonth)
                        {
                            // Legacy lines 283-286.
                            double calendarDays = DateTime.DaysInMonth(payDate.Year, payDate.Month);
                            var weeklyOffDays = await _accrualService.GetActualWeekOffsAsync(employeeId, payDate);
                            var salaryBasisDays = calendarDays - weeklyOffDays;
                            overtimePerHourRate = (totalBasicPay / salaryBasisDays) / otHoursBasis;
                        }
                    }
                }
            }

            // Legacy line 294.
            expectedOvertimeAmount = overtimeHours * overtimePerHourRate;
        }

        // Legacy line 301.
        return Math.Round(expectedOvertimeAmount < 0 ? 0 : expectedOvertimeAmount, 3);
    }

    /// <summary>Legacy lines 213-231.</summary>
    private async Task<double> GetOvertimeHoursBasisAsync(ManagePayrollSettings settings, int employeeId, DateTime payDate)
    {
        double otHoursBasis = 0;
        var defaultWorkingHours = settings?.DefaultWorkingHours ?? 0;
        var basis = settings?.OvertimeHoursBasis ?? OvertimeHoursBasis.FixedHours;

        if (basis == OvertimeHoursBasis.FixedHours)
        {
            otHoursBasis = defaultWorkingHours;
        }
        else if (basis == OvertimeHoursBasis.DutyHours)
        {
            try
            {
                var employee = _employeeRepository.GetAll().FirstOrDefault(r => r.Id == employeeId);
                otHoursBasis = await _timeDataService.GetTotalHoursInDayAsync(employee, payDate);
            }
            catch (Exception)
            {
                // Legacy line 227: empty catch, kept.
            }

            if (otHoursBasis == 0)
            {
                otHoursBasis = defaultWorkingHours;
            }
        }

        return otHoursBasis;
    }

    /// <summary>
    /// Legacy lines 266-270: the first basic-pay record covering the date, then the sum of its detail
    /// amounts whose wage type is "Include in Overtime Pay".
    /// </summary>
    private double GetTotalBasicPay(int employeeId, DateTime payDate)
    {
        var empBasicPay = _employeeBasicPayRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId && r.StartDate <= payDate && r.EndDate >= payDate)
            .OrderBy(r => r.Id)
            .FirstOrDefault();

        if (empBasicPay == null)
        {
            return 0;
        }

        var includeInOvertimePayWageTypeIds = _wageTypeRepository.GetAll()
            .Where(r => r.IncludeInOvertimePay)
            .Select(r => r.Id)
            .ToList();

        return _employeeBasicPayDetailRepository.GetAll()
            .Where(r => r.EmployeeBasicPayId == empBasicPay.Id)
            .ToList()
            .Where(r => includeInOvertimePayWageTypeIds.Contains(r.WageTypeId))
            .Sum(r => r.Amount);
    }
}
