using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// Shared machinery of the two late-in/early-out deduction rules, wage codes <b>2000</b>
/// (<c>GetLateEarlyDeduction</c>, legacy <c>HCM10.PayrollEngine/EmployeeCalculatedPayService.cs</c> lines
/// 1803-1864) and <b>2010</b> (<c>GetLateEarlyDeductionAfterMonthlyTol</c>, lines 1868-1936). The two legacy
/// methods differ ONLY in how they arrive at the deductible DAY COUNT; the pay window, the rate base and the
/// per-day-rate switch are character-for-character identical, so they live here.
///
/// <para><b>Per-day-rate switch</b> (legacy lines 1835-1850 / 1907-1922), on the CURRENT wage type's
/// <see cref="WageType.BillingMethod"/>:</para>
/// <list type="bullet">
/// <item><description><c>FixedDaysPerMonth</c>: <c>rate / currWageType.FixedDays</c></description></item>
/// <item><description><c>CalendarDaysPerMonth</c>: <c>rate / DateTime.DaysInMonth(payDate)</c></description></item>
/// <item><description><c>WorkingDaysPerMonth</c>: <c>rate / (calendarDays - actualWeekOffs)</c></description></item>
/// <item><description>anything else: per-day rate stays 0, so the deduction is 0 and the line is deleted</description></item>
/// </list>
/// <para>Legacy guards NONE of these divisions against zero -- a wage type configured with
/// <c>FixedDays == 0</c> yields Infinity/NaN. Preserved: adding a guard would turn a loud data error into a
/// silent zero deduction.</para>
///
/// <para><b>OUTSTANDING GAP -- WorkingDaysPerMonth.</b> Legacy calls
/// <c>Program.GetActualWeekOffs(objectSpace, emp, empTimeRegister, payDate, payrollRunType, offCycleEndDate,
/// offCycleReasons)</c> (legacy <c>Program.cs</c> lines 2858-2905). That method is
/// <c>Program.GetWeeklyOffDays</c> (== <see cref="IPayrollDayCountService.GetWeeklyOffDaysAsync"/>) PLUS a
/// day-by-day roster/duty back-fill for every day of the month that has NO <c>TimeRegister</c> row
/// (legacy lines 2873-2900). In this port that back-fill exists only as the PRIVATE
/// <c>GetPayrollActualWeekOffsAsync</c> helper inside <c>PayrollDayCountService</c> and is not reachable from
/// a wage rule. Rather than under-count weekly offs (which would inflate <c>salaryBasisDays</c> and
/// UNDER-charge the deduction), this base uses <see cref="IPayrollDayCountService.GetWeeklyOffDaysAsync"/>
/// only when the month is fully covered by TimeRegister rows -- precisely the case in which legacy's
/// back-fill loop contributes nothing (legacy line 2873:
/// <c>if (DaysInMonth != timeReg.Count())</c>) -- and throws a loud
/// <see cref="NotImplementedException"/> otherwise. <b>Fix:</b> promote
/// <c>GetPayrollActualWeekOffsAsync</c> to <see cref="IPayrollDayCountService"/> and call it here.</para>
/// </summary>
public abstract class LateEarlyDeductionCalculatorBase : HCMv3DomainServiceBase, IWageTypeCalculator
{
    protected LateEarlyDeductionCalculatorBase(
        IPayrollRegisterStore registerStore,
        IPayrollDayCountService dayCountService,
        IRepository<WageType> wageTypeRepository)
    {
        RegisterStore = registerStore;
        DayCountService = dayCountService;
        WageTypeRepository = wageTypeRepository;
    }

    /// <inheritdoc/>
    public abstract string WageCode { get; }

    protected IPayrollRegisterStore RegisterStore { get; }

    protected IPayrollDayCountService DayCountService { get; }

    protected IRepository<WageType> WageTypeRepository { get; }

    /// <summary>
    /// The deductible day count. Legacy lines 1816-1826 for 2000, 1878-1900 for 2010.
    /// </summary>
    protected abstract double GetDeductionDays(PayrollCalculationContext context, DateTime payStartDate, DateTime payEndDate);

    /// <inheritdoc/>
    public async Task CalculateAsync(PayrollCalculationContext context)
    {
        // Legacy lines 435 / 470.
        if (!PayrollRunGates.AllowEndOfServiceOnly(context))
        {
            return;
        }

        var payDate = context.PayDate;
        var payStartDate = PayrollRunGates.MonthStart(payDate);

        // Legacy lines 1808-1810 / 1873-1876: the window ends at the MONTH END, not at payDate.
        var payEndDate = PayrollRunGates.MonthEnd(payDate);
        if (context.PayrollRunType == PayrollRunType.OffCycle
            && context.Register.InPeriod == context.Register.ForPeriod)
        {
            payEndDate = context.OffCycleEndDate;
        }

        var existing = await RegisterStore.GetLineByWageTypeIdAsync(context.Register, context.WageType.Id);

        var deductionDays = GetDeductionDays(context, payStartDate, payEndDate);

        double rate = existing?.Rate ?? 0;
        double number = existing?.Number ?? 0;
        double amount = 0;

        // Legacy lines 1828 / 1885.
        if (deductionDays != 0)
        {
            // Legacy lines 1833 / 1905.
            var lines = await RegisterStore.GetLinesAsync(context.Register);
            var includedIds = WageTypeFlagLookup.WageTypeIdsWith(WageTypeRepository, w => w.IncludeInLateEarlyDed);
            rate = lines.Where(l => includedIds.Contains(l.WageTypeId)).Sum(l => l.Rate);

            var perDayRate = await GetPerDayRateAsync(context, rate);

            number = deductionDays;
            amount = perDayRate * deductionDays;
        }

        // Legacy lines 456-461 / 492-497: "zero means delete".
        await RegisterStore.SaveLineAsync(context.Register, new PayrollRegisterLine
        {
            Id = existing?.Id ?? 0,
            WageTypeId = context.WageType.Id,
            WageCode = context.WageType.Code,
            Rate = rate,
            Number = number,
            Amount = amount
        });
    }

    /// <summary>Legacy lines 1835-1850 / 1907-1922.</summary>
    private async Task<double> GetPerDayRateAsync(PayrollCalculationContext context, double rate)
    {
        var payDate = context.PayDate;
        double calendarDays = DateTime.DaysInMonth(payDate.Year, payDate.Month);

        switch (context.WageType.BillingMethod)
        {
            case BillingMethod.FixedDaysPerMonth:
                return rate / context.WageType.FixedDays;

            case BillingMethod.CalendarDaysPerMonth:
                return rate / calendarDays;

            case BillingMethod.WorkingDaysPerMonth:
                var weeklyOffDays = await GetActualWeekOffsAsync(context);
                return rate / (calendarDays - weeklyOffDays);

            default:
                return 0;
        }
    }

    /// <summary>See the class remarks: partial port of legacy <c>Program.GetActualWeekOffs</c>.</summary>
    private async Task<double> GetActualWeekOffsAsync(PayrollCalculationContext context)
    {
        var payDate = context.PayDate;
        var monthStart = PayrollRunGates.MonthStart(payDate);
        var monthEnd = PayrollRunGates.MonthEnd(payDate);
        var daysInMonth = DateTime.DaysInMonth(payDate.Year, payDate.Month);

        // Legacy lines 2867-2873.
        var registeredDays = context.TimeRegisterQuery == null
            ? 0
            : context.TimeRegisterQuery.Count(r => r.EmployeeId == context.EmployeeId
                                                   && r.DayDate >= monthStart
                                                   && r.DayDate <= monthEnd);

        if (registeredDays != daysInMonth)
        {
            throw new NotImplementedException(
                "Wage code " + WageCode + " with BillingMethod.WorkingDaysPerMonth needs legacy "
                + "Program.GetActualWeekOffs (HCM10.PayrollEngine/Program.cs lines 2858-2905), whose "
                + "day-by-day roster/duty back-fill for days with no TimeRegister row is only available in "
                + "this port as the PRIVATE PayrollDayCountService.GetPayrollActualWeekOffsAsync helper. "
                + "The employee has " + registeredDays + " of " + daysInMonth + " days evaluated for "
                + monthStart.ToString("yyyy-MM") + ", so the back-fill would contribute and cannot be "
                + "skipped. Fix: expose GetPayrollActualWeekOffsAsync on IPayrollDayCountService and call it "
                + "from LateEarlyDeductionCalculatorBase.GetActualWeekOffsAsync.");
        }

        // Legacy line 2871 -- note BOTH date arguments are the month end.
        return await DayCountService.GetWeeklyOffDaysAsync(
            context.EmployeeId, monthEnd, context.PayrollRunType, monthEnd, context.OffCycleReason);
    }
}
