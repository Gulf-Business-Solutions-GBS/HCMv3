using System;
using System.Threading.Tasks;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// Resolves the disciplinary escalation ladder (ported from legacy <c>DisciplinaryRequest</c>'s
/// <c>CurrentDeductionCount</c> / <c>DisciplinaryDeductionType</c> computed properties).
///
/// <para>
/// It lives here, in the domain, rather than in either of its two callers because both of them need the
/// same answer and they must not be able to disagree. The workflow handler uses it on approval to STAMP the
/// penalty onto the record; the app service uses it before submission to SHOW the person raising the record
/// what penalty they are about to trigger. If those two computed the ladder separately, the number an
/// approver agreed to and the number that got written would drift apart -- and the disagreement would only
/// ever surface as an employee being docked something nobody signed off.
/// </para>
/// </summary>
public interface IDisciplinaryEscalationService
{
    /// <summary>
    /// Counts the employee's prior offences of this type inside the type's repeat window and reads the
    /// resulting penalty off the type's detail tiers.
    ///
    /// <para>
    /// The count deliberately runs with the <c>ApprovedOnly</c> filter DISABLED, so a record that is still
    /// awaiting approval counts towards the ladder. That is the conservative direction and the correct one:
    /// two offences raised in the same week must escalate even if the first has not been signed off yet,
    /// otherwise the ladder could be defeated simply by raising both before either is approved. Records that
    /// were REJECTED are excluded -- an accusation somebody refused is not an offence.
    /// </para>
    /// </summary>
    /// <param name="employeeId">The employee the record is about.</param>
    /// <param name="disciplinaryTypeId">The offence type whose ladder is being read.</param>
    /// <param name="disciplinaryDate">The date of the offence being assessed. The window ends here.</param>
    /// <param name="excludeDisciplinaryId">
    /// The record being assessed, so it does not count itself. <c>null</c> when previewing a record that
    /// does not exist yet.
    /// </param>
    Task<DisciplinaryEscalation> ResolveAsync(
        int employeeId,
        int disciplinaryTypeId,
        DateTime disciplinaryDate,
        int? excludeDisciplinaryId = null);
}
