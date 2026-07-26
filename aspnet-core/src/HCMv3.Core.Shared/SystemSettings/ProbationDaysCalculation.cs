namespace HCMv3.SystemSettings;

/// <summary>
/// Ported from legacy XAF <c>HCM10.Module.BusinessObjects.SysSettings.ManagePersonnelSettings</c>
/// (nested enum promoted to a top-level enum here).
/// </summary>
public enum ProbationDaysCalculation
{
    CalendarDays = 0, // Count Calendar days as ProbationDays from Joining Days
    WorkingDays = 1,  // Count Working days as ProbationDays from Joining Days
}
