namespace HCMv3.SystemSettings;

/// <summary>
/// Ported from legacy XAF <c>HCM10.Module.BusinessObjects.SysSettings.ManagePayrollSettings</c>
/// (nested enum promoted to a top-level enum here).
/// </summary>
public enum OvertimeHoursBasis
{
    FixedHours = 0, // Count DefaultWorkingHours will be picked
    DutyHours = 1,  // Count Duty Hours of employee will be picked
}
