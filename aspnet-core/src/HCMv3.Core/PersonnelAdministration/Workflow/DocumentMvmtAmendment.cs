using System;

namespace HCMv3.PersonnelAdministration.Workflow;

/// <summary>
/// The proposed shape of a document movement after an amendment: what the employee is asking it to become.
///
/// <para>
/// Serialized onto <c>WorkflowRequest.AmendmentJson</c> rather than written to the movement row, because
/// until an approver agrees, the movement on file is still the one that says who is holding the passport.
/// </para>
///
/// <para>
/// Deliberately not the whole entity. Only this field may be changed through an amendment, so serializing a
/// full row would invite an amendment that quietly rewrote the employee, the document or the tenant. What is
/// not here cannot be amended.
/// </para>
///
/// <para>
/// This is the RETURN leg of the legacy <c>EmployeeDocMvmtRequest</c>, which carried a <c>MovementType</c>
/// of Withdraw or Return and, for a Return, hunted down the still-open movement row and stamped its actual
/// return date. Changing a field on a row that is already live and approved is an amendment by definition,
/// so the return needs no entity, no table and no catalog row of its own -- it rides the existing
/// <see cref="EmployeeDocumentMvmt"/> transaction type and its existing approval chain.
/// </para>
/// </summary>
public class DocumentMvmtAmendment
{
    /// <summary>
    /// The day the document was actually handed back.
    ///
    /// <para>
    /// Nullable, and null means "do not touch what the movement already has". A return cannot be un-stamped
    /// through an amendment, which is the safe direction: a passport that is back in the safe stays back in
    /// the safe.
    /// </para>
    /// </summary>
    public DateTime? ActualReturnDate { get; set; }
}
