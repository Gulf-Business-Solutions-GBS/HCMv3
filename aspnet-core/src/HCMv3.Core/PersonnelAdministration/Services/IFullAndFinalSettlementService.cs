using System.Threading.Tasks;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// The Full &amp; Final (F&amp;F) settlement logic of legacy
/// <c>HCM10.Module/BusinessObjects/PersonnelAdministration/EmployeeFnFPayment.cs</c> -- the business object's
/// <c>OnSaving</c>/<c>OnDeleting</c> side effects and the indemnity suggestion behind its screen.
///
/// <para><b>Why this matters: it is the ONLY bridge from an employee's exit to their end-of-service
/// indemnity.</b> Saving an F&amp;F writes an <c>EmployeeGratuity</c> row; the end-of-service off-cycle's wage
/// code 4005 sums those rows and wage code 1010 pays rate x days. Before this port the HCMv3 F&amp;F service only
/// stored the fields you typed, so an end-of-service off-cycle paid ZERO indemnity unless someone also
/// hand-entered a gratuity record.</para>
///
/// <para><b>And it is where resignation and termination differ.</b> The payroll engine pays whatever days the
/// gratuity row holds. The Kuwait resignation reduction (0 / 50 / 66.66 / 100 % by service) is applied HERE, to
/// the suggested DAYS, via <c>IndemnityPayRule</c>. Provision is identical for both; payment is not. The
/// engine-level version of the same reduction (<c>EmployeeCalculatedPayService.cs</c> 1745-1770) was commented
/// out in legacy on 23/09/2019 and is deliberately not ported.</para>
/// </summary>
public interface IFullAndFinalSettlementService
{
    /// <summary>
    /// The balances an F&amp;F entry may compensate, the suggested indemnity days, and the values legacy would
    /// auto-fill on a NEW entry.
    /// </summary>
    Task<FullAndFinalBalances> GetBalancesAsync(int employeeId);

    /// <summary>
    /// Legacy <c>PerformValidations</c> + <c>ValidatePreviousEntry</c>, run before a save. Throws a
    /// <c>UserFriendlyException</c> with legacy's own wording.
    /// </summary>
    /// <param name="current">The values being saved.</param>
    /// <param name="previous">The values the entry had when loaded; <c>null</c> for a new entry.</param>
    /// <param name="currentFnFPaymentId">The entry's own id, excluded from the "another F&amp;F exists" check; <c>null</c> when new.</param>
    Task ValidateSaveAsync(FullAndFinalValues current, FullAndFinalValues previous, int? currentFnFPaymentId);

    /// <summary>
    /// Legacy <c>UpdateQuotaBalance</c> + <c>AddIndemnityPayAdjust</c>: undo what <paramref name="previous"/>
    /// wrote, then write <paramref name="current"/>'s leave, indemnity and ticket records.
    /// </summary>
    Task ApplyAsync(FullAndFinalValues current, FullAndFinalValues previous);

    /// <summary>
    /// Legacy <c>OnDeleting</c>'s guards: <c>ValidatePreviousEntry</c>, then the hard lock once the
    /// end-of-service salary register for this compensation date exists.
    /// </summary>
    Task ValidateDeleteAsync(FullAndFinalValues previous, int fnfPaymentId);

    /// <summary>Legacy <c>OnDeleting</c>'s side effects: undo everything <paramref name="previous"/> wrote.</summary>
    Task UndoAsync(FullAndFinalValues previous);
}
