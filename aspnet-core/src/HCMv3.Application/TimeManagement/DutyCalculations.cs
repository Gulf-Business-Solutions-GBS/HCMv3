namespace HCMv3.TimeManagement;

/// <summary>
/// Ported from the legacy XAF <c>Duty.GetPlannedMinutes(Duty)</c> static helper. Computes the planned
/// duty minutes accounting for break duration and the WeeklyOff/RestDay flags (0 minutes for those).
/// Kept as a standalone static helper (rather than a method on the entity) so it stays easily unit
/// testable and reusable by future consumers (e.g. payroll/attendance calculations in later waves).
/// </summary>
public static class DutyCalculations
{
    public static double GetPlannedMinutes(Duty duty)
    {
        if (duty.IsWeeklyOff || duty.IsRestDay)
        {
            return 0;
        }

        if (duty.BreakShift == null)
        {
            return (duty.EndTime - duty.StartTime).TotalMinutes;
        }

        return (duty.BreakShift.StartTime - duty.StartTime).TotalMinutes +
               (duty.EndTime - duty.BreakShift.EndTime).TotalMinutes;
    }
}
