using System;

namespace HCMv3.Common.Services;

/// <summary>
/// Calculates the Years/Months/Days difference between two dates (ported near-verbatim from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Services.DateTimeService</c>'s date-diff constructor logic). Pure
/// computation, no session/DB dependency -- kept as a plain class (not an <c>IDomainService</c>).
///
/// The legacy class also exposed 3 static session-dependent helpers
/// (<c>GetSysDateTimeFormat</c>/<c>GetSysDateFormat</c>/<c>GetSysTimeFormat</c>) that read
/// <c>ManageGeneralSettings.DateEditMask</c>/<c>TimeEditMask</c> via <c>SysService.GetGeneralSettings</c> --
/// those are ported as <c>HCMv3.SystemSettings.Services.SysSettingsAccessor.GetSysDateTimeFormatAsync</c>/
/// <c>GetSysDateFormatAsync</c>/<c>GetSysTimeFormatAsync</c> instead, since they require repository access.
/// </summary>
public class DateDiffCalculator
{
    /// <summary>Number of days in each month; index 1 (February) is -1, resolved for leap years below.</summary>
    private static readonly int[] MonthDays = { 31, -1, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

    private readonly DateTime _fromDate;
    private readonly DateTime _toDate;

    public int Years { get; }
    public int Months { get; }
    public int Days { get; }

    public DateDiffCalculator(DateTime d1, DateTime d2)
    {
        if (d1 > d2)
        {
            _fromDate = d2;
            _toDate = d1;
        }
        else
        {
            _fromDate = d1;
            _toDate = d2;
        }

        // Day calculation
        var increment = 0;

        if (_fromDate.Day > _toDate.Day)
        {
            increment = MonthDays[_fromDate.Month - 1];
        }

        // February
        if (increment == -1)
        {
            increment = DateTime.IsLeapYear(_fromDate.Year) ? 29 : 28;
        }

        if (increment != 0)
        {
            Days = (_toDate.Day + increment) - _fromDate.Day;
            increment = 1;
        }
        else
        {
            Days = _toDate.Day - _fromDate.Day;
        }

        // Month calculation
        if ((_fromDate.Month + increment) > _toDate.Month)
        {
            Months = (_toDate.Month + 12) - (_fromDate.Month + increment);
            increment = 1;
        }
        else
        {
            Months = _toDate.Month - (_fromDate.Month + increment);
            increment = 0;
        }

        // Year calculation
        Years = _toDate.Year - (_fromDate.Year + increment);
    }

    public override string ToString()
    {
        return $"{Years} Year(s), {Months} month(s), {Days} day(s)";
    }
}
