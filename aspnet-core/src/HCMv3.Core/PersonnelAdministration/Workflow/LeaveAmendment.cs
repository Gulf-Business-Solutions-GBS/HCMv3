using System;

namespace HCMv3.PersonnelAdministration.Workflow;

/// <summary>
/// The proposed shape of a leave after an amendment: what the employee is asking it to become.
///
/// <para>
/// Serialized onto <c>WorkflowRequest.AmendmentJson</c> rather than written to the leave row, because until
/// an approver agrees, the leave on file is still the one that counts for payroll and quota.
/// </para>
///
/// <para>
/// Deliberately not the whole entity. Only these fields may be changed through an amendment, so serializing
/// a full row would invite an amendment that quietly rewrote the employee or the tenant. What is not here
/// cannot be amended.
/// </para>
/// </summary>
public class LeaveAmendment
{
    public int AbsenceTypeId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public double TotalLeaveDays { get; set; }

    public string Comments { get; set; }

    /// <summary>
    /// The day the employee actually rejoined, when this amendment is a RETURN FROM LEAVE.
    ///
    /// <para>
    /// Nullable, and null means "do not touch what the leave already has". A return is an amendment like any
    /// other -- it shortens the leave and stamps the return in one approved change -- so it needs no entity,
    /// no table and no catalog row of its own. An ordinary amendment leaves this null and the leave's
    /// existing return stamp is left alone.
    /// </para>
    /// </summary>
    public DateTime? ReturnDate { get; set; }

    /// <summary>
    /// Set true only by a return. False means "leave the flag as it is", NOT "clear it" -- a return cannot be
    /// un-stamped through an amendment, which is the safe direction: an employee who is back at their desk
    /// stays back at their desk.
    /// </summary>
    public bool IsReturned { get; set; }

    /// <summary>
    /// The leave's EndDate BEFORE an early return pulled it in, so the days that were granted but not taken
    /// stay visible after the fact. Null on an ordinary amendment, which leaves the stored value alone.
    /// </summary>
    public DateTime? InitialEndDate { get; set; }
}
