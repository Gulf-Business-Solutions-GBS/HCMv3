using System;

namespace HCMv3.TimeManagement;

/// <summary>
/// Shared time-of-day comparison helper reused by <see cref="BreakShiftsAppService"/>,
/// <see cref="DutiesAppService"/> and <see cref="DailyDutiesAppService"/>.
///
/// Ported from the near-duplicate <c>IsFirstGreaterThanSecond</c> private methods that existed in both
/// legacy XAF <c>Duty.cs</c> and <c>DailyDuty.cs</c> — consolidated here in one place instead of
/// duplicating the logic per entity.
/// </summary>
public static class TimeRangeValidationHelper
{
    /// <summary>
    /// Legacy parity: returns <c>true</c> when <paramref name="first"/> is greater than OR EQUAL TO
    /// <paramref name="second"/> (the legacy implementation treats a zero difference as "greater").
    /// </summary>
    public static bool IsFirstGreaterThanSecond(TimeSpan first, TimeSpan second)
    {
        return (first - second).TotalMinutes >= 0;
    }
}
