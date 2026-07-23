namespace HCMv3.TimeManagement;

/// <summary>
/// Which clock-in/clock-out punches are used when multiple punches exist within the same day
/// (ported from legacy XAF <c>HCM10.Module.BusinessObjects.TimeManagement.TimeEventRule</c>).
/// </summary>
public enum TimeEventRule
{
    NA = 0,
    FirstInFirstOut = 1,
    FirstInLastOut = 2,
    LastInFirstOut = 3,
    LastInLastOut = 4
}
