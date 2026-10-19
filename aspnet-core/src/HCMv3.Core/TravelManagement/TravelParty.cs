namespace HCMv3.TravelManagement;

/// <summary>
/// Who a <see cref="Travel"/> or <see cref="TravelFeedback"/> belongs to: an employee or a job applicant.
///
/// <para>
/// Legacy (<c>Travel.PerformValidation</c>, <c>TravelFeedback.PerformValidation</c>) refused only the case
/// where both were blank -- "Employee and Applicant both together should not be blank." HCMv3 also refuses
/// both being set: a row naming two people is ambiguous about whose trip it is, whose per diem applies,
/// and which claims match it. Kept in one place so the trip, the claim and the approval handler cannot
/// drift apart on the rule.
/// </para>
/// </summary>
public static class TravelParty
{
    /// <summary>True when exactly one of the two is set. Ids of zero or less count as not set.</summary>
    public static bool HasExactlyOne(int? employeeId, int? applicantId)
    {
        var hasEmployee = employeeId.HasValue && employeeId.Value > 0;
        var hasApplicant = applicantId.HasValue && applicantId.Value > 0;

        return hasEmployee != hasApplicant;
    }

    /// <summary>Treats a zero or negative id from a client as "not chosen".</summary>
    public static int? Normalize(int? id)
    {
        return id.HasValue && id.Value > 0 ? id : null;
    }
}
