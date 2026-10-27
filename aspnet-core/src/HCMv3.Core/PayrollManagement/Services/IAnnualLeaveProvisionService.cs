using System;
using System.Threading.Tasks;
using Abp.Domain.Services;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// Wave P5. The annual-leave half of the provisions cluster, ported from legacy
/// <c>HCM10.PayrollEngine/EmployeeCalculatedPayService.cs</c>:
/// <c>GetAnnualLeaveProvision</c> (line 2281), <c>GetTotalAnnualLeaveProvision</c> (line 2317),
/// <c>CalculateAnnualLeaveProvision</c> (line 2356), <c>GetAnnualLeaveProvisionDayWiseEntry</c> (line 2422)
/// and <c>GetProvisionChangeAnnualLeave</c> (line 2531).
///
/// <para><b>Why this is a service and not inlined into the two rules.</b> 3001 and 3004 share
/// <c>CalculateAnnualLeaveProvision</c> verbatim, and the day-wise entry (3001's tail) shares
/// <c>GetProvisionChangeAnnualLeave</c> with it. Keeping the shared bodies here mirrors what
/// <see cref="IIndemnityCalculationService"/> already does for the indemnity half, so the two
/// <see cref="IWageTypeCalculator"/> rules stay thin branch scaffolding on both sides of the cluster.</para>
///
/// <para><b>Nothing here duplicates <see cref="IAccrualService"/>.</b> That service's
/// <c>GetAsOnDateProvisionAsync</c>/<c>GetAsOnDateQuotaAsync</c> answer a different question (an employee's
/// leave QUOTA/provision from the absence-quota ledger). Legacy's annual-leave PROVISION posting reads the
/// already-posted register lines "4010"/"4022" and, in period-wise mode, sums
/// <c>TimeRegister.AnnualLeaveAccrualPerDay</c> directly -- it never calls <c>AccrualService</c>. Checked
/// before writing; the two are not interchangeable.</para>
/// </summary>
public interface IAnnualLeaveProvisionService : IDomainService
{
    /// <summary>
    /// Legacy <c>GetAnnualLeaveProvision</c> (line 2281): monthly annual-leave provision. Number source is
    /// register wage code <b>"4010"</b>; when that line is absent legacy zeroes Rate/Number/Amount.
    /// Period-wise mode (<see cref="PayrollEngineSettings.IsPeriodwiseProvision"/>) delegates to the shared
    /// <c>CalculateAnnualLeaveProvision</c> over the CURRENT month.
    /// </summary>
    Task<AnnualLeaveProvisionAmounts> GetAnnualLeaveProvisionAsync(PayrollCalculationContext context);

    /// <summary>
    /// Legacy <c>GetTotalAnnualLeaveProvision</c> (line 2317): closing annual-leave provision. Identical to
    /// <see cref="GetAnnualLeaveProvisionAsync"/> except the number source is register wage code
    /// <b>"4022"</b> and, in period-wise mode, the shared calculation starts at the employee's
    /// <c>JoiningDate</c> instead of the month start.
    /// </summary>
    Task<AnnualLeaveProvisionAmounts> GetTotalAnnualLeaveProvisionAsync(PayrollCalculationContext context);

    /// <summary>
    /// Legacy <c>GetAnnualLeaveProvisionDayWiseEntry</c> (line 2422): when the employee got a salary increment
    /// inside the current period, splits the wage code <b>"3001"</b> line into per-period
    /// <c>SalaryRegisterDaysDetail</c> rows (this period's own provision plus a previous-period catch-up
    /// difference from <see cref="GetProvisionChangeAnnualLeaveAsync"/>) and adds that difference into the
    /// "3001" line's own Amount.
    ///
    /// <para>Legacy deletes any pre-existing day-wise rows FIRST and then may return early (no increment this
    /// month / no earlier basic pay), leaving them deleted. Preserved.</para>
    /// </summary>
    Task<AnnualLeaveProvisionDayWiseResult> GetAnnualLeaveProvisionDayWiseEntryAsync(PayrollCalculationContext context);

    /// <summary>
    /// Legacy <c>GetProvisionChangeAnnualLeave</c> (line 2531):
    /// <c>prevClosingBalance * Math.Round(salaryInProvision / 26, 6) - prevClosingAmount</c>, where both
    /// previous figures come from wage code "3004" on the previous month's REGULAR, non-retro register
    /// (<c>ForPeriod == InPeriod == previous month end</c>). Returns 0 when no such register exists.
    ///
    /// <para><b>The literal 26 and the 6-digit rounding are legacy's</b> (line 2591) and are reproduced
    /// exactly -- this is the only rounding anywhere in the annual-leave provision scope. Note the divisor is
    /// hardcoded 26 here even though every other annual-leave provision path divides by the wage type's
    /// <c>FixedDays</c>; that inconsistency is legacy's and is NOT harmonised.</para>
    /// </summary>
    /// <param name="registerTarget">
    /// Which register family to read the previous month's lines from. Passed through (rather than hardcoded
    /// to <see cref="PayrollRegisterTarget.Live"/>) so this method needs no edit in wave P7 -- the previous
    /// register's lines are read via <see cref="IPayrollRegisterStore"/>, never via
    /// <c>SalaryRegisterDetail</c> directly.
    /// </param>
    Task<double> GetProvisionChangeAnnualLeaveAsync(int employeeId, DateTime payDate, double salaryInProvision, PayrollRegisterTarget registerTarget);
}

/// <summary>
/// The Rate/Number/Amount triple legacy wrote onto the <c>SalaryRegisterDetail</c> by reference in
/// <c>GetAnnualLeaveProvision</c>/<c>GetTotalAnnualLeaveProvision</c>/<c>CalculateAnnualLeaveProvision</c>.
/// Returned instead of mutating an entity, so the calling <see cref="IWageTypeCalculator"/> stays behind
/// <see cref="IPayrollRegisterStore"/> (wave P7 requirement).
/// </summary>
public class AnnualLeaveProvisionAmounts
{
    public double Rate { get; set; }

    public double Number { get; set; }

    public double Amount { get; set; }
}

/// <summary>Result of <see cref="IAnnualLeaveProvisionService.GetAnnualLeaveProvisionDayWiseEntryAsync"/> (legacy method was <c>void</c>).</summary>
public class AnnualLeaveProvisionDayWiseResult
{
    /// <summary>Number of day-wise rows written (0 when legacy returned early or the difference was 0).</summary>
    public int CreatedDaysDetailCount { get; set; }

    /// <summary>The value returned by <see cref="IAnnualLeaveProvisionService.GetProvisionChangeAnnualLeaveAsync"/>.</summary>
    public double ProvisionChangeAmount { get; set; }

    /// <summary>Final Amount written to the wage code "3001" line, when one was written.</summary>
    public double PostedAmount { get; set; }

    /// <summary>Id of the wage code "3001" register line touched, if any.</summary>
    public int? SalaryRegisterDetailId { get; set; }
}
