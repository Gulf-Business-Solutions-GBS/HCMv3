using System;
using System.Linq;
using Abp.Dependency;
using Abp.Domain.Repositories;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// <b>Wage code 2000 -- Late/Early Deduction.</b> Legacy
/// <c>HCM10.PayrollEngine/EmployeeCalculatedPayService.cs</c> lines 432-465 (branch) and 1803-1864
/// (<c>GetLateEarlyDeduction</c>). Everything except the day count lives in
/// <see cref="LateEarlyDeductionCalculatorBase"/>.
///
/// <para><b>Day count</b> (legacy lines 1816-1826): the per-day <c>DedLateInEarlyOutDay</c> column summed
/// over the pay window -- or, when <c>ManageTimeSettings.EnableTimeRegisterApproval</c> is on, the APPROVED
/// minutes re-expressed as a fraction of the day: <c>ApprovedDedLateInEarlyOutMins / PlannedWorkingMins</c>
/// (0 when <c>PlannedWorkingMins</c> is 0).</para>
///
/// <para>Reads <see cref="PayrollCalculationContext.TimeRegisterQuery"/> (the unfiltered query) on BOTH run
/// types -- legacy does the same here, unlike wage code 2007 which switches between the list and the
/// query.</para>
/// </summary>
public class LateEarlyDeductionWageTypeCalculator : LateEarlyDeductionCalculatorBase, ITransientDependency
{
    public LateEarlyDeductionWageTypeCalculator(
        IPayrollRegisterStore registerStore,
        IPayrollDayCountService dayCountService,
        IRepository<WageType> wageTypeRepository)
        : base(registerStore, dayCountService, wageTypeRepository)
    {
    }

    /// <inheritdoc/>
    public override string WageCode => "2000";

    /// <inheritdoc/>
    protected override double GetDeductionDays(PayrollCalculationContext context, DateTime payStartDate, DateTime payEndDate)
    {
        if (context.TimeRegisterQuery == null)
        {
            return 0;
        }

        var useApproved = context.Settings.EnableTimeRegisterApproval;

        var rows = context.TimeRegisterQuery
            .Where(r => r.EmployeeId == context.EmployeeId
                        && r.DayDate >= payStartDate
                        && r.DayDate <= payEndDate)
            .Select(r => new
            {
                r.DedLateInEarlyOutDay,
                r.ApprovedDedLateInEarlyOutMins,
                r.PlannedWorkingMins
            })
            .ToList();

        return rows.Sum(r => !useApproved
            ? r.DedLateInEarlyOutDay
            : (r.PlannedWorkingMins != 0 ? r.ApprovedDedLateInEarlyOutMins / r.PlannedWorkingMins : 0));
    }
}
