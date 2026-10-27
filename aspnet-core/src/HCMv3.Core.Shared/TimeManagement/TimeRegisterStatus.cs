namespace HCMv3.TimeManagement;

/// <summary>
/// Approval-dashboard status of a <see cref="HCMv3.TimeManagement.TimeRegister"/> row (ported from legacy
/// XAF <c>HCM10.Module.BusinessObjects.TimeManagement.Status</c>, Wave 4). Renamed from the legacy bare
/// <c>Status</c> to avoid clashing with the very common "Status" identifier used elsewhere in this codebase.
/// </summary>
public enum TimeRegisterStatus
{
    Opened = 0,
    Approved = 1,
    Reopened = 2
}
