namespace HCMv3.SystemSettings;

/// <summary>
/// Ported from legacy XAF <c>HCM10.Module.BusinessObjects.SysSettings.ManageAppraisalSettings</c>
/// (nested enum promoted to a top-level enum here).
/// </summary>
public enum AppraisalCycle
{
    Quarterly = 0,  // ALL
    HalfYearly = 1, // Plan+Mid+Annual
    Annually = 2,   // Plan+Annual
}
