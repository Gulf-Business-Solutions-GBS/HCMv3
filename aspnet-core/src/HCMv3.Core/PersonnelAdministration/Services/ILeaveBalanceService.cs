using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// Answers "how much leave can this employee still apply for" -- the BOOKABLE balance, which is not the
/// same as the quota balance.
///
/// <para>
/// The distinction is the rule this system runs on. <c>EmployeeAbsenceQuota.QuotaUsed</c> is only ever
/// touched when leave is APPROVED, so payroll, accrual and every report keep seeing exactly what they saw
/// before workflow existed. Leave awaiting approval consumes nothing -- it may still be rejected. But it
/// must reduce what the employee can go on to book, or they could submit the same fortnight three times and
/// every submission would pass its own balance check independently.
/// </para>
/// <para>
/// So there is no "reserved" column and no reservation lifecycle to maintain -- nothing to leak when a
/// request is abandoned, nothing to reconcile nightly. Bookable is computed, and it can be computed only
/// because the pending leave is sitting in <c>EmployeeAbsences</c> alongside the approved leave. In the
/// legacy two-table design the pending rows lived somewhere the balance calculation could not see.
/// </para>
/// </summary>
public interface ILeaveBalanceService
{
    /// <summary>
    /// Balance for one absence type, as of <paramref name="asOf"/> (defaults to today, which selects the
    /// quota period covering that date).
    /// </summary>
    Task<LeaveBalance> GetBalanceAsync(int employeeId, int absenceTypeId, DateTime? asOf = null);

    /// <summary>Balances for every absence type the employee holds quota for -- what the ESS leave screen
    /// lists.</summary>
    /// <paramref name="excludeWorkflowRequestId"/> leaves one pending change out of the pending figure: the
    /// change being validated must not count against itself.
    Task<List<LeaveBalance>> GetBalancesAsync(int employeeId, DateTime? asOf = null, int? excludeWorkflowRequestId = null);
}
