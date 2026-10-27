namespace HCMv3.TimeManagement;

/// <summary>How a geo punch's distance from its work location is judged.</summary>
public enum GeoPunchRangeVerdict
{
    InRange = 0,

    /// <summary>Outside the work location's radius, but the tenant allows punching from outside it.</summary>
    OutOfRangeAllowed = 1,

    /// <summary>Outside the radius and the tenant does not allow it: the punch is refused.</summary>
    OutOfRangeRefused = 2
}
