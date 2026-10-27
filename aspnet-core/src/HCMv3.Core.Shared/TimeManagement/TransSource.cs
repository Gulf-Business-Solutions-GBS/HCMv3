namespace HCMv3.TimeManagement;

/// <summary>
/// Where a <see cref="HCMv3.TimeManagement.TimeEvent"/> punch record originated from (ported from legacy
/// XAF <c>HCM10.Module.BusinessObjects.TimeManagement.TransSource</c>, Wave 4).
/// </summary>
public enum TransSource
{
    Machine = 0,
    Manual = 1,
    System = 2,
    ESS = 3,
    MobileAPI = 4,
    ExternalAPI = 5
}
