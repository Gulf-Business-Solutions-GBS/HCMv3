using System;
using System.Threading.Tasks;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// Keeps the <see cref="EmployeeOnHold"/> row that a separation's on-hold flags stand for in step with the
/// separation (legacy <c>EmployeeSeparation.SaveOtherInfo</c> "Set OnHold" block, <c>OnDeleting</c>, and the
/// two OnHold checks in <c>PerformValidations</c>/<c>ValidatePreviousEntry</c>).
///
/// <para>
/// <b>Why payroll needs this.</b> Payroll never reads the separation's flags. It reads
/// <see cref="EmployeeOnHold"/>. Legacy turned the flags on the exit screen into an on-hold row on every save,
/// and that row is what actually held the leaver's pay. Storing the flags without the row would show HR a
/// hold that payroll ignores.
/// </para>
///
/// <para>
/// <b>How legacy found "its" row, kept as-is.</b> There is no link column. The row is the employee's hold
/// whose start date equals the separation's previous on-hold from-date. Legacy saved the normalised date back
/// onto the separation (first of the month), so that is the same value the hold was created with. Matching
/// the same way means separations and holds migrated from legacy keep working without a backfill.
/// </para>
/// </summary>
public interface ISeparationOnHoldService
{
    /// <summary>
    /// Legacy's two refusals. A separation cannot be added, edited or deleted when the employee already has an
    /// on-hold that starts after the separation's on-hold from-date -- payroll has moved on past it.
    /// </summary>
    /// <param name="separation">The separation as it will be saved; null when deleting.</param>
    /// <param name="previousEmployeeId">The employee as stored before this change; null for a new separation.</param>
    /// <param name="previousOnHoldFromDate">The on-hold from-date as stored before this change.</param>
    Task EnsureNoLaterOnHoldAsync(
        EmployeeSeparation separation,
        int? previousEmployeeId,
        DateTime? previousOnHoldFromDate);

    /// <summary>
    /// Deletes the hold created for the previous values, then -- when an on-hold from-date is set and at least
    /// one hold flag is ticked -- normalises the separation's dates to whole months and creates the new hold
    /// from the flags. The separation's own <c>OnHoldFromDate</c>/<c>OnHoldTillDate</c> are rewritten in place,
    /// exactly as legacy did.
    /// </summary>
    Task ApplyAsync(EmployeeSeparation separation, int? previousEmployeeId, DateTime? previousOnHoldFromDate);

    /// <summary>Deletes the hold a separation created, when the separation itself is deleted.</summary>
    Task RemoveAsync(int employeeId, DateTime? onHoldFromDate);
}
