namespace HCMv3.TrainingManagement;

/// <summary>
/// Whether a <c>TrainingCourse</c> is run in-house or by an outside provider (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.TrainingManagement.Source</c>, same values). Renamed from the legacy bare
/// <c>Source</c>, which is far too generic a name for a shared enum and the generated client proxies.
/// </summary>
public enum TrainingCourseSource
{
    Internal = 0,
    External = 1
}
