using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// <b>Wage code 2007 -- Absent Unpaid Days Deduction.</b> Legacy
/// <c>HCM10.PayrollEngine/EmployeeCalculatedPayService.cs</c> lines 395-430 (branch) and 1940-1996
/// (<c>GetAbsentUnpaidDeductionNew</c>). Legacy's older <c>GetAbsentUnpaidDeduction</c> is dead (its call is
/// commented out at line 420) and is NOT ported.
///
/// <para><b>Formula.</b> <c>workingDaysDed = absentDays - (calendarDays - basicPayNumber)</c>, then
/// <c>amount = workingDaysDed * basicPayAmount / basicPayNumber</c>, where "basicPay" is the register line
/// for wage code <b>1000</b>. The subtraction backs out the non-working portion of the month that the basic
/// pay line already excluded, so absences falling on those days are not charged twice. No rounding.</para>
///
/// <para><b>"Zero means delete".</b> Legacy creates the detail row, runs the calculation into it and deletes
/// it when the amount is 0 (lines 421-426), including every early-exit path inside the helper
/// (no absences, <c>workingDays == 0</c>, computed amount exactly 0). Reproduced by always calling
/// <see cref="IPayrollRegisterStore.SaveLineAsync"/> with <c>deleteWhenAmountIsZero: true</c>.</para>
///
/// <para><b>Hard failure preserved:</b> a missing 1000 line throws
/// "Basic salary not found for the WageType=1000" (legacy line 1993) -- but ONLY when there are absent days,
/// exactly as legacy nests it.</para>
///
/// <para><b>DELIBERATELY NOT DELEGATED to <see cref="IPayrollDayCountService.GetAbsentDaysAsync"/>.</b> That
/// method is a port of a DIFFERENT legacy method (<c>Program.GetAbsentDays</c>, which produces wage types
/// 4001/4002) and differs from this one in three money-changing ways: (a) it EXCLUDES days that also carry an
/// unpaid-leave day, this one does not; (b) it applies the <c>WageType.IncludeInCutoff</c> cut-off window
/// shift, this one always uses the plain calendar month of the pay date; (c) it honours the off-cycle end
/// date, this one does not. Using it here would silently change the deduction. The day counting below is
/// therefore a faithful copy of legacy lines 1943-1955, not a re-derivation.</para>
///
/// <para><b>Preserved legacy quirk:</b> the Regular arm reads the preloaded
/// <see cref="PayrollCalculationContext.TimeRegisterList"/> while the OffCycle arm reads the unfiltered
/// <see cref="PayrollCalculationContext.TimeRegisterQuery"/> -- legacy nulls the list on off-cycle runs
/// (legacy <c>Program.cs</c> line 1771), which is why it must not be used there.</para>
/// </summary>
public class AbsentUnpaidDaysDeductionWageTypeCalculator : HCMv3DomainServiceBase, IWageTypeCalculator, ITransientDependency
{
    /// <summary>Legacy's literal "1000" (basic salary).</summary>
    private const string BasicSalaryWageCode = "1000";

    private readonly IPayrollRegisterStore _registerStore;

    public AbsentUnpaidDaysDeductionWageTypeCalculator(IPayrollRegisterStore registerStore)
    {
        _registerStore = registerStore;
    }

    /// <inheritdoc/>
    public string WageCode => "2007";

    /// <inheritdoc/>
    public async Task CalculateAsync(PayrollCalculationContext context)
    {
        // Legacy line 398.
        if (!PayrollRunGates.AllowEndOfServiceOnly(context))
        {
            return;
        }

        var payDate = context.PayDate;
        var monthStart = PayrollRunGates.MonthStart(payDate);
        var monthEnd = PayrollRunGates.MonthEnd(payDate);
        var useApproved = context.Settings.EnableTimeRegisterApproval;

        // Legacy lines 1942-1955.
        double totalDayAbsentDays = 0;

        if (context.PayrollRunType == PayrollRunType.Regular)
        {
            var rows = context.TimeRegisterList;
            if (rows != null)
            {
                totalDayAbsentDays = rows
                    .Where(r => r.EmployeeId == context.EmployeeId
                                && r.DayDate >= monthStart
                                && r.DayDate <= monthEnd
                                && (useApproved ? r.ApprovedAbsentDay == 1 : r.AbsentDay == 1))
                    .Sum(r => useApproved ? r.ApprovedAbsentDay : r.AbsentDay);
            }
        }
        else if (context.PayrollRunType == PayrollRunType.OffCycle)
        {
            var query = context.TimeRegisterQuery;
            if (query != null)
            {
                totalDayAbsentDays = query
                    .Where(r => r.EmployeeId == context.EmployeeId
                                && r.DayDate >= monthStart
                                && r.DayDate <= monthEnd
                                && (useApproved ? r.ApprovedAbsentDay == 1 : r.AbsentDay == 1))
                    .ToList()
                    .Sum(r => useApproved ? r.ApprovedAbsentDay : r.AbsentDay);
            }
        }

        var existing = await _registerStore.GetLineByWageTypeIdAsync(context.Register, context.WageType.Id);

        double number = existing?.Number ?? 0;
        double amount = 0;

        // Legacy line 1961.
        if (totalDayAbsentDays > 0)
        {
            var basicPay = await _registerStore.GetLineAsync(context.Register, BasicSalaryWageCode);

            // Legacy lines 1991-1994.
            if (basicPay == null)
            {
                throw new InvalidOperationException(
                    string.Format("Basic salary not found for the WageType={0}", BasicSalaryWageCode));
            }

            double calendarDays = DateTime.DaysInMonth(payDate.Year, payDate.Month);
            var workingDays = basicPay.Number;
            var workingDaysDed = totalDayAbsentDays - (calendarDays - workingDays);

            // Legacy line 1974: when the basic-pay line carries no days, nothing is written and the row is
            // deleted by the caller's zero check.
            if (workingDays != 0)
            {
                amount = (workingDaysDed * basicPay.Amount) / workingDays;

                if (amount != 0)
                {
                    number = workingDaysDed;
                }
            }
        }

        await _registerStore.SaveLineAsync(context.Register, new PayrollRegisterLine
        {
            Id = existing?.Id ?? 0,
            WageTypeId = context.WageType.Id,
            WageCode = context.WageType.Code,
            // Legacy never assigns Rate on this branch.
            Rate = existing?.Rate ?? 0,
            Number = number,
            Amount = amount
        });
    }
}
