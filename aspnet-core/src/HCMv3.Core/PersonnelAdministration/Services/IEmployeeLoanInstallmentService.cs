using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Services;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// Saving and removing <see cref="EmployeeLoanInstallment"/> rows with everything legacy's <c>OnSaving</c> and
/// <c>OnDeleting</c> did around them: validation, end-date chaining, the loan's recovery end date, and the payroll
/// retro date. Every writer goes through here -- the HR screen, loan approval and rejoin -- so the rules cannot
/// drift between them.
/// </summary>
public interface IEmployeeLoanInstallmentService : IDomainService
{
    /// <summary>
    /// Validates and inserts <paramref name="installment"/>, re-chains the loan's end dates and recalculates its
    /// recovery end date. Unless <paramref name="isSystemGenerated"/>, also moves the employee's payroll retro date
    /// back to the row's effective date (legacy <c>SetRetroForInstallmentChange</c>).
    /// </summary>
    Task CreateAsync(EmployeeLoanInstallment installment, bool isSystemGenerated = false);

    /// <summary>
    /// The update counterpart of <see cref="CreateAsync"/>. <paramref name="previousStartDate"/> and
    /// <paramref name="previousAmount"/> are the values as loaded, before the form's were mapped on (legacy's
    /// <c>prevStartDate</c>/<c>prevAmount</c>): nothing changed means no retro, a moved date means retro from the
    /// earlier of the two.
    /// </summary>
    Task UpdateAsync(EmployeeLoanInstallment installment, DateTime previousStartDate, double previousAmount);

    /// <summary>
    /// Legacy <c>OnDeleting</c>: re-chains and recalculates as though the row were already gone, deletes it, and
    /// moves the retro date back to its effective date.
    /// </summary>
    Task DeleteAsync(EmployeeLoanInstallment installment);

    /// <summary>
    /// Legacy <c>LoanRequest</c> approval (lines 197-212): a loan coming into force with a fixed amount and a
    /// recovery start, and no schedule yet, gets its first row -- the fixed amount from the first of the recovery
    /// start month. Then the recovery end date is recalculated, as legacy's loan save did. Safe to call twice.
    /// </summary>
    Task EnsureInitialInstallmentAsync(EmployeeLoan loan);

    /// <summary>
    /// The stored schedules of the given loans, keyed by loan id, for working out the instalment in force with
    /// <see cref="LoanInstallmentSchedule.GetInstallmentAmount"/> -- one read for a whole list of loans.
    /// </summary>
    ILookup<int, EmployeeLoanInstallment> GetSchedules(IEnumerable<int> employeeLoanIds);

    /// <summary>
    /// Legacy <c>EmployeeLoan.CalculateRecoveryDate</c> for <paramref name="loan"/> as it stands in memory, read
    /// against its stored schedule and the exemptions in force. Null when the payroll setting
    /// <c>AutoUpdateLoanRecoveryEndDate</c> is off; otherwise the date (the loan's own when nothing can be walked).
    /// Stores nothing.
    /// </summary>
    Task<DateTime?> CalculateRecoveryEndDateAsync(
        EmployeeLoan loan,
        int? excludeInstallmentId = null,
        int? excludeExemptionId = null);

    /// <summary>
    /// Legacy <c>EmployeeLoan.RecalculateRecoveryEndDate</c>: calculates and stores the loan's recovery end date.
    /// When <paramref name="isEffectiveDatedChildChange"/>, the loan's own save does not trigger a retro of its
    /// own -- the instalment or exemption that caused it already set the right date (legacy
    /// <c>StopRetroEffect</c>). Null when the setting is off or the loan is not found.
    /// </summary>
    Task<DateTime?> RecalculateRecoveryEndDateAsync(
        int employeeLoanId,
        int? excludeInstallmentId = null,
        int? excludeExemptionId = null,
        bool isEffectiveDatedChildChange = false);
}
