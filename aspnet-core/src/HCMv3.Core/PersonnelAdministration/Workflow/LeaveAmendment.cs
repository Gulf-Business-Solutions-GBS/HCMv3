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

    /// <summary>
    /// LATE RETURN only: the calendar range, after the leave's last day, to be booked as further paid leave of
    /// the SAME absence type (charged to its quota). Legacy <c>AbsenceRequest.PaidStartDate/PaidEndDate</c>.
    ///
    /// <para>
    /// Together with the unpaid range these must cover the late gap exactly; the rules are legacy's
    /// (<c>WorkFlowServices</c> lines 434-503) and are checked in <c>LeaveWorkflowTransactionHandler</c>.
    /// On approval each range becomes a NEW approved <c>EmployeeAbsence</c> row -- there is still no request
    /// table, the rows are written straight into the one leave table, linked to the approving request.
    /// </para>
    /// </summary>
    public DateTime? PaidStartDate { get; set; }

    /// <inheritdoc cref="PaidStartDate"/>
    public DateTime? PaidEndDate { get; set; }

    /// <summary>
    /// LATE RETURN only: the calendar range to be booked as UNPAID leave (the absence type with code "2", as
    /// legacy hard-codes). Legacy <c>AbsenceRequest.UnpaidStartDate/UnpaidEndDate</c>.
    /// </summary>
    public DateTime? UnpaidStartDate { get; set; }

    /// <inheritdoc cref="UnpaidStartDate"/>
    public DateTime? UnpaidEndDate { get; set; }

    /// <summary>
    /// True when this change CANCELS an approved leave (legacy <c>ActionType.Cancellation</c>). On approval the
    /// leave row is deleted and its days go back to the quota; until then the leave stays exactly as approved,
    /// visible to payroll. The date fields carry the leave's current values so the inbox summary can show
    /// what is being cancelled.
    /// </summary>
    public bool IsCancellation { get; set; }

    /// <summary>True when this amendment reports a late return with a paid and/or unpaid range to book.</summary>
    public bool IsLateReturn => IsReturned && (PaidStartDate.HasValue || UnpaidStartDate.HasValue);

    /// <summary>
    /// What a return does to the ADDITIONAL UNPAID leave that was booked alongside this one (legacy
    /// <c>EarlyJoinType</c>): nothing, remove it (the employee came back before it started), or cut it short
    /// (they came back during it).
    /// </summary>
    public AdditionalLeaveAction AdditionalLeaveAction { get; set; }

    /// <summary>The day the additional unpaid leave should now end, when it is being cut short.</summary>
    public DateTime? AdditionalLeaveEndDate { get; set; }
}

/// <summary>Legacy <c>EarlyJoinType</c>, as what happens to the additional unpaid leave row.</summary>
public enum AdditionalLeaveAction
{
    /// <summary>Leave it as it is (no additional leave, or the return does not reach it).</summary>
    None = 0,

    /// <summary>The employee returned before it began: legacy <c>UpdateMainLeaveDeleteAdditional</c>.</summary>
    Delete = 1,

    /// <summary>The employee returned during it: legacy <c>UpdateAdditional</c> -- the main leave is untouched
    /// and the unpaid row ends the day before they came back.</summary>
    Shorten = 2,
}
