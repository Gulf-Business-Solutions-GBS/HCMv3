using System;

namespace HCMv3.TimeManagement.Workflow;

/// <summary>
/// The proposed shape of a shift override after an amendment: which Duty the employee should work, and on
/// which date.
///
/// <para>
/// Serialized onto <c>WorkflowRequest.AmendmentJson</c> rather than written to the override row, because
/// until an approver agrees the override on file is still the one the roster, the Time Engine and every
/// attendance report are working from.
/// </para>
///
/// <para>
/// Deliberately only these two fields. The employee and the tenant are NOT amendable: an amendment that
/// could rewrite the employee would be a way of moving somebody else's shift under cover of editing your
/// own. What is not here cannot be amended.
/// </para>
///
/// <para>
/// <b>This is also how an override is REMOVED.</b> There is no delete request and no pending-removal state:
/// a manager taking a planned shift away amends <see cref="DutyId"/> to an off-duty / rest
/// <see cref="Duty"/>, and that goes through approval like any other change -- leaving a record of who
/// decided the shift should not happen, which a delete would erase.
/// </para>
/// </summary>
public class SubstituteDutyAmendment
{
    /// <summary>The Duty the employee should work on <see cref="SubstituteDate"/>.</summary>
    public int DutyId { get; set; }

    /// <summary>The date the override applies to. Amendable, so a shift can be MOVED rather than cancelled
    /// and re-raised -- which would lose the link back to the original decision.</summary>
    public DateTime SubstituteDate { get; set; }
}
