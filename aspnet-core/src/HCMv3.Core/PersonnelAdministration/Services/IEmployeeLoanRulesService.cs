using System.Threading.Tasks;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// The loan rules legacy (LHRMS) ran in <c>EmployeeLoan.PerformValidation</c> / <c>UpdateDueBalance</c> and in
/// <c>EmployeeLoanRecovery.PerformValidation</c> / <c>UpdateDueBalance</c>, which the first port left out: the loan
/// type's eligibility limits, its recovery option and instalment limit, and keeping the loan's balance due in step
/// with its recoveries.
/// </summary>
public interface IEmployeeLoanRulesService
{
    /// <summary>
    /// Legacy <c>EmployeeLoan.PerformValidation</c>, loan-type part: a loan only after <c>EligiblePeriod</c> months
    /// of service, at most <c>EligibleCountPerYear</c> loans of the type in a service year, and a due amount no
    /// larger than <c>EligibleAmountNumber</c> times the basic salary (wage type 1000) in force on the loan date.
    /// A limit of 0 means no limit. Throws a user-friendly error naming the limit that was crossed.
    /// </summary>
    Task ValidateEligibilityAsync(EmployeeLoan loan);

    /// <summary>
    /// Legacy <c>EmployeeLoanRecovery.PerformValidation</c>, loan-type part: the recovery method must be one the
    /// loan type's <c>RecoveryOption</c> allows, and the loan may not have more than <c>MaxInstallments</c>
    /// payroll or external recoveries (0 = no limit), this one included.
    /// </summary>
    Task ValidateRecoveryAsync(EmployeeLoanRecovery recovery);

    /// <summary>Legacy <c>UpdateDueBalance</c>: due amount less everything recovered, rounded for the employee.</summary>
    Task<double> CalculateBalanceDueAsync(EmployeeLoan loan, int? excludeRecoveryId = null);

    /// <summary>
    /// Stores <see cref="CalculateBalanceDueAsync"/> on the loan -- after a recovery is added, changed or removed,
    /// as legacy's recovery save and delete did. Pending changes are flushed first so the sum sees them.
    /// </summary>
    Task UpdateBalanceDueAsync(int employeeLoanId, int? excludeRecoveryId = null);
}
