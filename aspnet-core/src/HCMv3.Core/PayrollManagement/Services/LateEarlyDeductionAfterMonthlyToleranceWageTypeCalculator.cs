using System;
using System.Linq;
using Abp.Dependency;
using Abp.Domain.Repositories;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// <b>Wage code 2010 -- Late/Early Ded After Monthly Tolerance.</b> Legacy
/// <c>HCM10.PayrollEngine/EmployeeCalculatedPayService.cs</c> lines 467-501 (branch) and 1868-1936
/// (<c>GetLateEarlyDeductionAfterMonthlyTol</c>). Everything except the day count lives in
/// <see cref="LateEarlyDeductionCalculatorBase"/>.
///
/// <para><b>Day count</b> (legacy lines 1878-1900): sum the per-day <c>DedAfterMonthlyTol</c> MINUTES over
/// the pay window, convert to hours, and divide by the employee's daily working hours -- taken from the FIRST
/// row in the window with a non-zero <c>PlannedWorkingMins</c> (legacy line 1896:
/// <c>.Where(PlannedWorkingMins != 0).Select(PlannedWorkingMins).FirstOrDefault()</c>), falling back to
/// <c>ManageTimeSettings.DefaultWorkingHours</c> when there is none.</para>
///
/// <para><b>Preserved legacy quirk:</b> this rule does NOT consult
/// <c>EnableTimeRegisterApproval</c> at all -- unlike wage code 2000, which switches to the approved minutes.
/// <c>DedAfterMonthlyTol</c> has no "approved" twin on <c>TimeRegister</c>.</para>
/// </summary>
public class LateEarlyDeductionAfterMonthlyToleranceWageTypeCalculator : LateEarlyDeductionCalculatorBase, ITransientDependency
{
    public LateEarlyDeductionAfterMonthlyToleranceWageTypeCalculator(
        IPayrollRegisterStore registerStore,
        IPayrollDayCountService dayCountService,
        IRepository<WageType> wageTypeRepository)
        : base(registerStore, dayCountService, wageTypeRepository)
    {
    }

    /// <inheritdoc/>
    public override string WageCode => "2010";

    /// <inheritdoc/>
    protected override double GetDeductionDays(PayrollCalculationContext context, DateTime payStartDate, DateTime payEndDate)
    {
        if (context.TimeRegisterQuery == null)
        {
            return 0;
        }

        var rows = context.TimeRegisterQuery
            .Where(r => r.EmployeeId == context.EmployeeId
                        && r.DayDate >= payStartDate
                        && r.DayDate <= payEndDate)
            .Select(r => new { r.PlannedWorkingMins, r.DedAfterMonthlyTol })
            .ToList();

        var dedAfterMonthlyTolMins = rows.Sum(r => r.DedAfterMonthlyTol);

        // Legacy line 1885.
        if (dedAfterMonthlyTolMins == 0)
        {
            return 0;
        }

        var dedAfterMonthlyTolHours = dedAfterMonthlyTolMins / 60;

        var empWorkingMins = rows.Where(r => r.PlannedWorkingMins != 0)
            .Select(r => r.PlannedWorkingMins)
            .FirstOrDefault();

        var empWorkingHours = empWorkingMins != 0
            ? empWorkingMins / 60
            : context.Settings.DefaultWorkingHours;

        // Legacy lines 1899-1900: no division when the working hours resolve to 0.
        return empWorkingHours != 0 ? dedAfterMonthlyTolHours / empWorkingHours : 0;
    }
}
