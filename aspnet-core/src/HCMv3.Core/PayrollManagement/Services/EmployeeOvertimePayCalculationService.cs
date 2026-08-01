using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings;
using HCMv3.TimeManagement.Services;

namespace HCMv3.PayrollManagement.Services;

/// <inheritdoc cref="IEmployeeOvertimePayCalculationService"/>
/// <remarks>
/// <b>WAVE P9.</b> Ported from legacy <c>HCM10.PayrollEngine/EmployeeOvertimePayService.cs</c>:
/// <c>Main</c> (legacy line 18) and its private <c>GetOvertimePerHourRate</c> (legacy line 157).
/// <c>Main_Simu</c>/<c>GetOvertimePerHourRate_Simu</c> (legacy lines 225-424) are deliberately NOT ported --
/// wave P7 collapses the simulation clone through <see cref="PayrollRegisterRef.Target"/>.
///
/// <para>Dispatched at legacy <c>Program.cs</c> line 2569. The <c>if (!onHold_AllEarnings)</c> gate (legacy
/// line 2567) is applied by the ENGINE, not here.</para>
///
/// <para><b>This arm only became reachable in wave P8</b>, when <c>OvertimeType.WageTypeId</c> was restored;
/// before that the overtime category set was always empty.</para>
///
/// <para><b>Preserved legacy quirks -- all deliberate, none "fixed":</b></para>
/// <list type="number">
/// <item><description><b>An EXISTING register line blocks the write entirely.</b> Legacy lines 123-146 look
/// the line up and then do all of their work inside <c>if (salaryRegisterDetail == null)</c> -- there is no
/// else. So when two overtime types map to the same wage type, or when an earlier rule already wrote that
/// line, the overtime amount is silently dropped rather than added. Reproduced verbatim.
/// <b>Flagged for the business.</b></description></item>
/// <item><description><b><c>Rate</c> is 0 on the <see cref="OvertimeHoursBasis.DutyHours"/> path.</b>
/// <c>totalotHourRate</c> is only ever assigned on the <see cref="OvertimeHoursBasis.FixedHours"/> path
/// (legacy lines 65-66); the DutyHours path computes a per-record rate local instead (legacy line 83). Legacy
/// then writes <c>Rate = totalotHourRate</c> (legacy line 131) and stamps the same 0 into the month-wise rows
/// (legacy line 110). Reproduced.</description></item>
/// <item><description><b>The advance-payment deduction is NOT sign-flipped.</b> Unlike
/// <see cref="EmployeePaymentCalculationService"/> (legacy <c>EmployeePaymentService.cs</c> line 114), the
/// overtime branch posts <c>innerTotalotAmount</c> raw (legacy line 97) with no
/// <c>BillingType == Earning ? amount : -amount</c>. Reproduced.</description></item>
/// <item><description><b>The month-wise rows use <c>endOfMonth</c> as <c>Period</c></b> (legacy line 109),
/// not the recovery period's first day.</description></item>
/// <item><description><b><c>TimeDataService.GetTotalHoursInDay</c> failures are swallowed</b> (legacy lines
/// 171-175: an empty <c>catch</c>), leaving <c>otHoursBasis</c> at 0, which then falls back to
/// <c>default_Working_Hours</c>. Reproduced -- that empty catch is observable behaviour, not error
/// handling.</description></item>
/// <item><description><b>Division by <c>FixedDays</c> / <c>salaryBasisDays</c> is unguarded</b> (legacy lines
/// 202, 214). Only <c>otHoursBasis != 0</c> is checked (legacy line 198). Reproduced.</description></item>
/// <item><description><b>Legacy's <c>Program.calculateSalRegDaysDetail</c> gate is a hardcoded <c>true</c>
/// literal</b> (legacy <c>Program.cs</c> line 131, never re-assigned), so it is not carried -- same decision
/// as <see cref="EmployeeBasicPayCalculationService"/> and <see cref="ITimeTypeCumulationService"/>.</description></item>
/// </list>
///
/// <para><b>No EF Core.</b> Plain synchronous LINQ over <c>IRepository&lt;T&gt;.GetAll()</c>.</para>
/// </remarks>
public class EmployeeOvertimePayCalculationService : HCMv3DomainServiceBase, IEmployeeOvertimePayCalculationService, ITransientDependency
{
    private readonly IRepository<OvertimeType> _overtimeTypeRepository;
    private readonly IRepository<OvertimeTypeDetail> _overtimeTypeDetailRepository;
    private readonly IRepository<EmployeeOvertime> _employeeOvertimeRepository;
    private readonly IRepository<WageType> _wageTypeRepository;
    private readonly IPayrollRegisterStore _registerStore;
    private readonly IPayrollAdvancePaymentService _advancePaymentService;
    private readonly IPayrollWeekOffCalculator _weekOffCalculator;
    private readonly ITimeDataService _timeDataService;

    public EmployeeOvertimePayCalculationService(
        IRepository<OvertimeType> overtimeTypeRepository,
        IRepository<OvertimeTypeDetail> overtimeTypeDetailRepository,
        IRepository<EmployeeOvertime> employeeOvertimeRepository,
        IRepository<WageType> wageTypeRepository,
        IPayrollRegisterStore registerStore,
        IPayrollAdvancePaymentService advancePaymentService,
        IPayrollWeekOffCalculator weekOffCalculator,
        ITimeDataService timeDataService)
    {
        _overtimeTypeRepository = overtimeTypeRepository;
        _overtimeTypeDetailRepository = overtimeTypeDetailRepository;
        _employeeOvertimeRepository = employeeOvertimeRepository;
        _wageTypeRepository = wageTypeRepository;
        _registerStore = registerStore;
        _advancePaymentService = advancePaymentService;
        _weekOffCalculator = weekOffCalculator;
        _timeDataService = timeDataService;
    }

    /// <summary>Legacy <c>EmployeeOvertimePayService.Main</c> (legacy line 18).</summary>
    public async Task CalculateAsync(PayrollCalculationContext context)
    {
        if (context?.WageType == null)
        {
            throw new ArgumentException("PayrollCalculationContext.WageType must be set.", nameof(context));
        }

        // Legacy lines 24-27.
        if (!PayrollRunGates.AllowIncludingAdvanceLeave(context) || context.IsNotReturnFromLeave)
        {
            return;
        }

        // Legacy line 29 over Program.OvertimeTypeList, pre-filtered to `WageType != null`
        // (legacy Program.cs line 2667).
        var overtimeTypes = _overtimeTypeRepository.GetAll()
            .Where(r => r.WageTypeId != null && r.WageTypeId == context.WageType.Id)
            .ToList();

        foreach (var overtimeType in overtimeTypes)
        {
            await CalculateForOvertimeTypeAsync(context, overtimeType);
        }
    }

    /// <summary>Legacy lines 32-148, the body of the <c>foreach (var overtimeType in overtimeTypes)</c> loop.</summary>
    private async Task CalculateForOvertimeTypeAsync(PayrollCalculationContext context, OvertimeType overtimeType)
    {
        // Legacy lines 32-36.
        var payStartDate = PayrollRunGates.MonthStart(context.PayDate);
        var payEndDate = context.PayDate;

        if (context.PayrollRunType == PayrollRunType.OffCycle
            && context.Register.InPeriod == context.Register.ForPeriod)
        {
            payEndDate = context.OffCycleEndDate;
        }

        double totalOvertimeHourRate = 0;
        double totalOvertimeHours = 0;
        double totalOvertimeAmount = 0;
        var monthWiseDays = new List<SalaryRegisterDayCount>();

        // Legacy line 44.
        var recoveryPeriods = _advancePaymentService.GetRecoveryPeriods(payStartDate, payEndDate);

        foreach (var recoveryPeriod in recoveryPeriods)
        {
            var endOfMonth = PayrollRunGates.MonthEnd(recoveryPeriod);

            // Legacy lines 52-56.
            var approvedOvertimeHours = _employeeOvertimeRepository.GetAll()
                .Where(r => r.EmployeeId == context.EmployeeId
                            && r.EffectiveDate >= recoveryPeriod
                            && r.EffectiveDate <= endOfMonth
                            && r.OvertimeTypeId == overtimeType.Id)
                .Sum(r => r.ApprovedOvertimeHours);

            // Legacy line 57.
            if (approvedOvertimeHours == 0)
            {
                continue;
            }

            double innerOvertimeHours = 0;
            double innerOvertimeAmount = 0;

            if (context.Settings.OvertimeHoursBasis == OvertimeHoursBasis.FixedHours)
            {
                // Legacy lines 65-69.
                totalOvertimeHourRate = totalOvertimeHourRate != 0
                    ? totalOvertimeHourRate
                    : await GetOvertimePerHourRateAsync(context, context.PayDate, overtimeType);

                innerOvertimeHours = approvedOvertimeHours * overtimeType.OvertimeRate;
                innerOvertimeAmount = innerOvertimeHours * totalOvertimeHourRate;
            }
            else if (context.Settings.OvertimeHoursBasis == OvertimeHoursBasis.DutyHours)
            {
                // Legacy lines 76-90.
                var employeeOvertimes = _employeeOvertimeRepository.GetAll()
                    .Where(r => r.EmployeeId == context.EmployeeId
                                && r.EffectiveDate >= recoveryPeriod
                                && r.EffectiveDate <= endOfMonth
                                && r.OvertimeTypeId == overtimeType.Id)
                    .Select(r => new { r.EffectiveDate, r.ApprovedOvertimeHours })
                    .ToList();

                foreach (var employeeOvertime in employeeOvertimes)
                {
                    var perRecordRate = await GetOvertimePerHourRateAsync(
                        context, employeeOvertime.EffectiveDate, overtimeType);

                    var actualOvertimeHours = employeeOvertime.ApprovedOvertimeHours * overtimeType.OvertimeRate;

                    innerOvertimeHours += actualOvertimeHours;
                    innerOvertimeAmount += actualOvertimeHours * perRecordRate;
                }
            }

            // Legacy lines 95-98 -- NOT sign-flipped, see class remarks quirk 3.
            if (PayrollRunGates.ShouldPostAdvancePaymentDed(context))
            {
                await AdvancePaymentDedAsync(context, recoveryPeriod, innerOvertimeAmount);
            }

            totalOvertimeHours += innerOvertimeHours;
            totalOvertimeAmount += innerOvertimeAmount;

            // Legacy lines 105-114.
            if (context.PayrollRunType == PayrollRunType.OffCycle
                && context.Register.InPeriod == context.Register.ForPeriod
                && innerOvertimeAmount != 0)
            {
                monthWiseDays.Add(new SalaryRegisterDayCount
                {
                    Period = endOfMonth,
                    Rate = totalOvertimeHourRate,
                    Number = innerOvertimeHours,
                    Amount = innerOvertimeAmount
                });
            }
        }

        // Legacy line 121.
        if (totalOvertimeAmount == 0)
        {
            return;
        }

        // Legacy lines 123-124: looked up by the OVERTIME TYPE's wage type (identical to context.WageType by
        // the filter above), and the whole write is skipped when a line already exists -- quirk 1.
        var existing = await _registerStore.GetLineByWageTypeIdAsync(
            context.Register, overtimeType.WageTypeId.Value);

        if (existing != null)
        {
            return;
        }

        // Legacy lines 126-140.
        var line = new PayrollRegisterLine
        {
            WageTypeId = context.WageType.Id,
            WageCode = context.WageType.Code,
            Number = totalOvertimeHours,
            Rate = totalOvertimeHourRate,
            Amount = totalOvertimeAmount
        };

        var saved = await _registerStore.SaveLineAsync(context.Register, line, deleteWhenAmountIsZero: false);

        // Legacy lines 142-145 (Program.AddSalaryRegisterDaysDetail), including its row-level
        // `rate != 0 || number != 0 || amount != 0` guard (legacy Program.cs line 5443).
        var days = monthWiseDays
            .Where(d => d.Rate != 0 || d.Number != 0 || d.Amount != 0)
            .ToList();

        if (saved != null && days.Count > 0)
        {
            await _registerStore.SaveDayDetailsAsync(context.Register, saved.Id, days);
        }
    }

    /// <summary>
    /// Legacy <c>EmployeeOvertimePayService.GetOvertimePerHourRate</c> (legacy line 157).
    /// </summary>
    private async Task<double> GetOvertimePerHourRateAsync(
        PayrollCalculationContext context,
        DateTime payDate,
        OvertimeType overtimeType)
    {
        // Legacy lines 163-178.
        double otHoursBasis = 0;
        var defaultWorkingHours = context.Settings.DefaultWorkingHours;

        if (context.Settings.OvertimeHoursBasis == OvertimeHoursBasis.FixedHours)
        {
            otHoursBasis = defaultWorkingHours;
        }
        else if (context.Settings.OvertimeHoursBasis == OvertimeHoursBasis.DutyHours)
        {
            try
            {
                otHoursBasis = await _timeDataService.GetTotalHoursInDayAsync(context.Employee, payDate);
            }
            catch (Exception)
            {
                // Legacy line 175: empty catch, preserved -- see class remarks quirk 5.
            }

            if (otHoursBasis == 0)
            {
                otHoursBasis = defaultWorkingHours;
            }
        }

        // Legacy lines 181-184.
        var detail = _overtimeTypeDetailRepository.GetAll()
            .Where(r => r.OvertimeTypeId == overtimeType.Id
                        && r.StartDate <= payDate
                        && r.EndDate >= payDate)
            .FirstOrDefault();

        var rateType = detail != null ? detail.OvertimeRateType : OvertimeRateType.Actual;

        // Legacy lines 187-190.
        if (rateType == OvertimeRateType.Fixed)
        {
            return detail.Rate;
        }

        if (rateType != OvertimeRateType.Actual)
        {
            return 0;
        }

        // Legacy lines 196-216.
        var lines = await _registerStore.GetLinesAsync(context.Register);
        var includeInOvertimePayWageTypeIds = GetIncludeInOvertimePayWageTypeIds();
        var totalBasicPay = lines
            .Where(l => includeInOvertimePayWageTypeIds.Contains(l.WageTypeId))
            .Sum(l => l.Rate);

        if (otHoursBasis == 0)
        {
            return 0;
        }

        var wageType = context.WageType;

        if (wageType.BillingMethod == BillingMethod.FixedDaysPerMonth)
        {
            return (totalBasicPay / wageType.FixedDays) / otHoursBasis;
        }

        if (wageType.BillingMethod == BillingMethod.CalendarDaysPerMonth)
        {
            double calendarDays = DateTime.DaysInMonth(payDate.Year, payDate.Month);
            return (totalBasicPay / calendarDays) / otHoursBasis;
        }

        if (wageType.BillingMethod == BillingMethod.WorkingDaysPerMonth)
        {
            double calendarDays = DateTime.DaysInMonth(payDate.Year, payDate.Month);
            var weeklyOffDays = await _weekOffCalculator.GetActualWeekOffsAsync(context, payDate);
            var salaryBasisDays = calendarDays - weeklyOffDays;
            return (totalBasicPay / salaryBasisDays) / otHoursBasis;
        }

        return 0;
    }

    /// <summary>
    /// Legacy line 196 is <c>salaryRegister.SalaryRegisterDetail.Where(r =&gt; r.WageType.IncludeInOvertimePay)</c>.
    /// <see cref="PayrollRegisterLine"/> carries no <c>WageType</c> navigation, so the id set is resolved from
    /// the wage-type table and intersected with the register lines instead.
    /// </summary>
    private List<int> GetIncludeInOvertimePayWageTypeIds()
    {
        return _wageTypeRepository.GetAll()
            .Where(r => r.IncludeInOvertimePay)
            .Select(r => r.Id)
            .ToList();
    }

    /// <summary>Legacy <c>Program.AdvancePaymentDed</c>, reached from legacy line 97.</summary>
    private Task AdvancePaymentDedAsync(PayrollCalculationContext context, DateTime paymentDate, double amount)
    {
        if (amount == 0)
        {
            return Task.CompletedTask;
        }

        if (context.PayrollRunType == PayrollRunType.OffCycle && context.IsEndOfService)
        {
            return Task.CompletedTask;
        }

        return _advancePaymentService.AdvancePaymentDedAsync(
            context.EmployeeId,
            paymentDate,
            context.OffCyclePaymentDate,
            amount,
            context.PayrollRunType,
            context.OffCycleReason,
            context.AdvancePayComment,
            context.Settings);
    }
}
