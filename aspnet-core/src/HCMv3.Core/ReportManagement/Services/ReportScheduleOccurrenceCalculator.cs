using System;

namespace HCMv3.ReportManagement.Services;

/// <summary>
/// Works out when a schedule should next fire.
///
/// Kept as a pure static so it can be reasoned about and tested without a database, a clock, or a
/// worker: "when does a monthly schedule that last ran on the 31st run next" is the kind of question
/// that is easy to get subtly wrong and impossible to notice afterwards, because a schedule that
/// silently stops firing looks identical to one nobody has triggered.
/// </summary>
public static class ReportScheduleOccurrenceCalculator
{
    /// <summary>
    /// The next fire time strictly AFTER <paramref name="after"/>, or null when the schedule is
    /// finished (a one-off that has run, or a recurring one past its end date).
    /// </summary>
    /// <param name="schedule">The schedule; its FirstOccurrence anchors the time of day.</param>
    /// <param name="after">Usually "now" -- the point to search forward from.</param>
    public static DateTime? GetNextRunTime(ReportSchedule schedule, DateTime after)
    {
        if (schedule == null || !schedule.IsActive)
        {
            return null;
        }

        if (schedule.OccurrenceType == ReportScheduleOccurrenceType.Once)
        {
            // A one-off is due until it has run, then never again. LastRunTime rather than a flag so
            // the run log stays the single source of truth about what happened.
            return schedule.LastRunTime.HasValue ? (DateTime?)null : schedule.FirstOccurrence;
        }

        if (!schedule.RecurringPattern.HasValue)
        {
            return null;
        }

        // Anchor on FirstOccurrence, not on the last run. Anchoring on the last run makes the schedule
        // DRIFT: a monthly job that ran an hour late once would keep sliding an hour later every month
        // until it fell into the next day.
        var next = schedule.FirstOccurrence;

        if (next > after)
        {
            return WithinEndDate(schedule, next);
        }

        // Advance from the anchor until strictly past `after`. Iterating rather than computing the
        // count directly keeps month-end behaviour correct: AddMonths clamps 31 Jan -> 28 Feb, and
        // multiplying an interval would lose that.
        var guard = 0;
        while (next <= after)
        {
            next = Advance(next, schedule.RecurringPattern.Value, ++guard, schedule.FirstOccurrence);

            // A schedule anchored far in the past should not spin. 4000 iterations covers ~11 years of
            // daily runs, well past any sane backlog, and stops a malformed row hanging the worker.
            if (guard > 4000)
            {
                return null;
            }
        }

        return WithinEndDate(schedule, next);
    }

    /// <summary>
    /// True when the schedule is due to run at <paramref name="now"/> -- i.e. its next fire time has
    /// arrived and it has not already run for that occurrence.
    /// </summary>
    public static bool IsDue(ReportSchedule schedule, DateTime now)
    {
        if (schedule == null || !schedule.IsActive)
        {
            return false;
        }

        var next = schedule.NextRunTime ?? GetNextRunTime(schedule, schedule.LastRunTime ?? DateTime.MinValue);

        return next.HasValue && next.Value <= now;
    }

    private static DateTime Advance(
        DateTime from,
        ReportRecurringPattern pattern,
        int occurrence,
        DateTime anchor)
    {
        switch (pattern)
        {
            case ReportRecurringPattern.Daily:
                return from.AddDays(1);
            case ReportRecurringPattern.Weekly:
                return from.AddDays(7);
            case ReportRecurringPattern.Monthly:
                // From the ANCHOR each time, so a schedule anchored on the 31st returns to the 31st in
                // months that have one. Stepping AddMonths(1) off the previous value would clamp to the
                // 28th in February and then stay on the 28th forever.
                return anchor.AddMonths(occurrence);
            case ReportRecurringPattern.Quarterly:
                return anchor.AddMonths(3 * occurrence);
            case ReportRecurringPattern.Yearly:
                return anchor.AddYears(occurrence);
            default:
                return from.AddDays(1);
        }
    }

    private static DateTime? WithinEndDate(ReportSchedule schedule, DateTime candidate)
    {
        if (!schedule.EndingOn.HasValue)
        {
            return candidate;
        }

        // EndingOn is a date; a run at any time on that day still counts as within it.
        var endOfDay = schedule.EndingOn.Value.Date.AddDays(1).AddTicks(-1);

        return candidate <= endOfDay ? candidate : (DateTime?)null;
    }
}
