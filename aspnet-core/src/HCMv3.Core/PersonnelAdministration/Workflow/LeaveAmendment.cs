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
}
