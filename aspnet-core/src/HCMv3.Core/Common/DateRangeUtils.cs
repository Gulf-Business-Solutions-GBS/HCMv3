using System;
using System.Collections.Generic;

namespace HCMv3.Common;

/// <summary>
/// Pure date-range utilities (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Services.SysService</c>'s <c>GetMonthYearList</c>). No session/DB
/// dependency -- kept as a plain static class.
/// </summary>
public static class DateRangeUtils
{
    /// <summary>
    /// Returns the list of month-end dates ("recovery periods") from <paramref name="startDate"/>'s
    /// month-end through <paramref name="endDate"/>, ported verbatim from legacy
    /// <c>SysService.GetMonthYearList</c>.
    /// </summary>
    public static List<DateTime> GetMonthYearList(DateTime startDate, DateTime endDate)
    {
        var recoveryPeriods = new List<DateTime>();

        var recoveryStartDate = new DateTime(startDate.Year, startDate.Month, DateTime.DaysInMonth(startDate.Year, startDate.Month));
        while (recoveryStartDate <= endDate)
        {
            if (recoveryPeriods.Count == 0 || !recoveryPeriods.Contains(recoveryStartDate))
            {
                recoveryPeriods.Add(recoveryStartDate);
            }

            recoveryStartDate = recoveryStartDate.AddDays(1);
            var daysInMonth = DateTime.DaysInMonth(recoveryStartDate.Year, recoveryStartDate.Month);
            recoveryStartDate = new DateTime(recoveryStartDate.Year, recoveryStartDate.Month, daysInMonth);
        }

        return recoveryPeriods;
    }
}
