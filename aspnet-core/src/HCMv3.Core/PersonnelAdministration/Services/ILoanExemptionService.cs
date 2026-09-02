using System;
using System.Threading.Tasks;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// What a recovery exemption means to the loan it hangs off: whether it may be granted at all, and what
/// granting it does to the repayment schedule.
///
/// <para>
/// Both members are here rather than inside the workflow handler because three entry points need them --
/// approval of a self-service request, an HR-entered exemption, and the removal of one -- and three copies
/// of loan arithmetic would drift apart on the first change. The handler expresses the rules to the engine
/// by calling this; it does not own them.
/// </para>
/// </summary>
public interface ILoanExemptionService
{
    /// <summary>
    /// Throws <see cref="Abp.UI.UserFriendlyException"/> unless the exemption may be granted: the loan must
    /// exist, the date must be set, the month must not already be exempted on that loan, and the date must
    /// fall inside the loan's own recovery period.
    ///
    /// <para>
    /// The duplicate check looks PAST the <c>ApprovedOnly</c> filter, so an exemption still sitting in an
    /// approver's inbox blocks a second one for the same month. Without that an employee who did not see
    /// their first submission land could file it twice, each invisible to the other, and two approvals would
    /// push the loan out by two months for one skipped instalment.
    /// </para>
    /// </summary>
    /// <param name="employeeLoanId">The loan the exemption is against.</param>
    /// <param name="exemptionDate">Any date inside the month being skipped.</param>
    /// <param name="excludeExemptionId">The exemption being checked, so it never competes with itself.</param>
    Task EnsureExemptionIsAllowedAsync(int employeeLoanId, DateTime exemptionDate, int? excludeExemptionId = null);

    /// <summary>
    /// Recomputes and stores <see cref="EmployeeLoan.EndDate"/> from the loan's own figures and the
    /// exemptions currently in force against it.
    ///
    /// <para>
    /// This is the port of legacy <c>EmployeeLoanExemption.UpdateRecoveryEndDate</c>, and it is the whole
    /// point of an exemption. Skipping a month's instalment without pushing the end date out does not DEFER
    /// the money, it FORGIVES it: the employee pays one instalment fewer and the loan is never fully
    /// recovered. An exemption without this recompute is cosmetic.
    /// </para>
    ///
    /// <para>
    /// A full recompute from the loan's base schedule, never an increment. That is what makes removing an
    /// exemption self-reversing: re-running it with the removed row excluded lands back on exactly the date
    /// the loan had before, with no stored "months added" counter to keep straight.
    /// </para>
    ///
    /// <para>
    /// Does nothing at all when the payroll setting <c>AutoUpdateLoanRecoveryEndDate</c> is off, or when the
    /// loan lacks the figures the schedule is derived from -- both matching legacy exactly.
    /// </para>
    /// </summary>
    /// <param name="employeeLoanId">The loan to recompute.</param>
    /// <param name="excludeExemptionId">
    /// An exemption to leave out of the count -- the one being deleted. Legacy passes the Oid being deleted
    /// for the same reason: at <c>OnDeleting</c> the row is still in the collection.
    /// </param>
    /// <returns>The end date after the recompute, or null when nothing was changed.</returns>
    Task<DateTime?> RecomputeRecoveryEndDateAsync(int employeeLoanId, int? excludeExemptionId = null);
}
