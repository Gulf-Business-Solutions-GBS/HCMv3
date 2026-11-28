using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using HCMv3.SystemSettings.Services;

namespace HCMv3.PersonnelAdministration.Services;

/// <inheritdoc cref="IExpectedPaymentAmountService"/>
/// <remarks>
/// Legacy source: <c>EmployeePayment.ExpectedAmount</c> getter and <c>GetExpectedAmount</c>. Steps, in legacy order:
/// <list type="number">
/// <item><description>Days: Day = the number; Hour = number / <c>PayrollSettings.DefaultWorkingHours</c>
/// (0 when that setting is 0).</description></item>
/// <item><description>The FIRST basic-pay record covering the payment date (lowest id); none means 0.</description></item>
/// <item><description>Pay rate = sum of that record's detail amounts whose wage type has
/// <c>IncludeInPartialPay</c>.</description></item>
/// <item><description>Per-day rate: Daily pay interval = the pay rate; Monthly = by the PAYMENT wage type's
/// billing method (calendar days in the month / calendar days minus actual week-offs / FixedDays / 0 for a
/// fixed amount), each guarded against a zero divisor. Any other pay interval gives 0.</description></item>
/// <item><description>Result = per-day rate x days, negatives become 0, rounded to 3 places.</description></item>
/// </list>
/// Week-offs come from <see cref="IAccrualService.GetActualWeekOffsAsync(int, DateTime)"/>, the port of legacy
/// <c>AccrualService.GetActualWeekOffs(Session, emp, payDate)</c>. Legacy swallowed every exception and
/// returned 0; that is kept.
/// </remarks>
public class ExpectedPaymentAmountService : HCMv3DomainServiceBase, IExpectedPaymentAmountService
{
    private readonly IRepository<WageType> _wageTypeRepository;
    private readonly IRepository<EmployeeBasicPay> _employeeBasicPayRepository;
    private readonly IRepository<EmployeeBasicPayDetail> _employeeBasicPayDetailRepository;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;
    private readonly IAccrualService _accrualService;

    public ExpectedPaymentAmountService(
        IRepository<WageType> wageTypeRepository,
        IRepository<EmployeeBasicPay> employeeBasicPayRepository,
        IRepository<EmployeeBasicPayDetail> employeeBasicPayDetailRepository,
        ISysSettingsAccessor sysSettingsAccessor,
        IAccrualService accrualService)
    {
        _wageTypeRepository = wageTypeRepository;
        _employeeBasicPayRepository = employeeBasicPayRepository;
        _employeeBasicPayDetailRepository = employeeBasicPayDetailRepository;
        _sysSettingsAccessor = sysSettingsAccessor;
        _accrualService = accrualService;
    }

    public async Task<double> GetExpectedAmountAsync(int employeeId, int wageTypeId, DateTime paymentDate, UnitType unitType, double number)
    {
        // Legacy getter gate.
        if (unitType == UnitType.Amount || employeeId <= 0 || wageTypeId <= 0 || paymentDate == default || number == 0)
        {
            return 0;
        }

        try
        {
            return await CalculateAsync(employeeId, wageTypeId, paymentDate.Date, unitType, number);
        }
        catch (Exception ex)
        {
            // Legacy: "safe fallback if calculation fails".
            Logger.Warn($"Expected payment amount could not be calculated for employee {employeeId}: {ex.Message}", ex);
            return 0;
        }
    }

    private async Task<double> CalculateAsync(int employeeId, int wageTypeId, DateTime payDate, UnitType unitType, double number)
    {
        var wageType = _wageTypeRepository.GetAll().FirstOrDefault(w => w.Id == wageTypeId);
        if (wageType == null)
        {
            return 0;
        }

        double totalDays = 0;
        switch (unitType)
        {
            case UnitType.Day:
                totalDays = number;
                break;
            case UnitType.Hour:
                var defaultWorkingHours = (await _sysSettingsAccessor.GetPayrollSettingsAsync())?.DefaultWorkingHours ?? 0;
                if (defaultWorkingHours > 0)
                {
                    totalDays = number / defaultWorkingHours;
                }
                break;
        }

        if (totalDays == 0)
        {
            return 0;
        }

        var basicPay = _employeeBasicPayRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId && r.StartDate <= payDate && r.EndDate >= payDate)
            .OrderBy(r => r.Id)
            .FirstOrDefault();
        if (basicPay == null)
        {
            return 0;
        }

        var partialPayWageTypeIds = _wageTypeRepository.GetAll()
            .Where(w => w.IncludeInPartialPay)
            .Select(w => w.Id)
            .ToList();

        var payRate = _employeeBasicPayDetailRepository.GetAll()
            .Where(d => d.EmployeeBasicPayId == basicPay.Id && partialPayWageTypeIds.Contains(d.WageTypeId))
            .Select(d => d.Amount)
            .ToList()
            .Sum();

        double perDaySalary = 0;

        if (basicPay.PayInterval == PayInterval.Monthly)
        {
            switch (wageType.BillingMethod)
            {
                case BillingMethod.CalendarDaysPerMonth:
                    perDaySalary = payRate / DateTime.DaysInMonth(payDate.Year, payDate.Month);
                    break;

                case BillingMethod.WorkingDaysPerMonth:
                    double calendarDays = DateTime.DaysInMonth(payDate.Year, payDate.Month);
                    var weeklyOffDays = await _accrualService.GetActualWeekOffsAsync(employeeId, payDate);
                    var salaryBasisDays = calendarDays - weeklyOffDays;
                    perDaySalary = salaryBasisDays > 0 ? payRate / salaryBasisDays : 0;
                    break;

                case BillingMethod.FixedDaysPerMonth:
                    perDaySalary = wageType.FixedDays > 0 ? payRate / wageType.FixedDays : 0;
                    break;

                case BillingMethod.FixedAmountPerMonth:
                    perDaySalary = 0;
                    break;
            }
        }
        else if (basicPay.PayInterval == PayInterval.Daily)
        {
            perDaySalary = payRate;
        }

        var expectedAmount = perDaySalary * totalDays;
        return Math.Round(expectedAmount < 0 ? 0 : expectedAmount, 3);
    }
}
