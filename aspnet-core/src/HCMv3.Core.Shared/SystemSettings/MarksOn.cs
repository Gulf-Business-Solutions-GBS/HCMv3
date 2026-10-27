namespace HCMv3.SystemSettings;

/// <summary>
/// Ported from legacy XAF <c>HCM10.Module.BusinessObjects.SysSettings.ManageAppraisalSettings</c>
/// (nested enum promoted to a top-level enum here).
/// </summary>
public enum MarksOn
{
    Ratings = 0, // Marks should not exceed Max Rating Code value
    Weight = 1,  // Marks should not exceed Weight of KPI
}
