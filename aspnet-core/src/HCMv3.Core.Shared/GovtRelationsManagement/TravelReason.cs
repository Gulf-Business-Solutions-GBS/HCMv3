namespace HCMv3.GovtRelationsManagement;

/// <summary>
/// Why the employee is leaving the country, as recorded on an exit permit (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Workflow.GovtRelations.TravelReason</c>).
///
/// <para>
/// Descriptive only. The permit itself is issued by the government outside this system; the reason is what
/// the internal approver reads when deciding whether to sign the absence off, so it drives no calculation
/// and has no side effect anywhere.
/// </para>
/// </summary>
public enum TravelReason
{
    PlannedLeave = 0,

    EmergencyLeave = 1,

    PersonalTravel = 2
}
