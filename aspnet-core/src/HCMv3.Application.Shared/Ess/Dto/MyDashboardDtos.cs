using System;
using System.Collections.Generic;

namespace HCMv3.Ess.Dto;

/// <summary>Who the signed-in employee is, for the dashboard's identity card.</summary>
public class MyDashboardProfileDto
{
    public int EmployeeId { get; set; }

    public string Code { get; set; }

    public string FullName { get; set; }

    public string JobTitle { get; set; }

    /// <summary>
    /// Organisation placement. Every one of these can legitimately be null: they come from the current
    /// EmployeeOrgAssignment, and an employee may have none. The tile must render blanks rather than
    /// hide itself, so somebody can SEE that their placement is missing and go and fix it.
    /// </summary>
    public string Department { get; set; }

    public string Company { get; set; }

    public string WorkLocation { get; set; }

    public string CostCenter { get; set; }
}

/// <summary>One leave type and what is left of it.</summary>
public class MyDashboardLeaveBalanceDto
{
    public string LeaveType { get; set; }

    public decimal Balance { get; set; }
}

/// <summary>
/// The people-news counters.
///
/// Counts only -- the dashboard shows a number and links onward. Returning the names would put every
/// colleague's birth date on a screen that anybody can open, which is more disclosure than a counter
/// needs to justify.
/// </summary>
public class MyDashboardAlertsDto
{
    public int BirthdaysThisMonth { get; set; }

    public int WorkAnniversariesThisMonth { get; set; }
}

/// <summary>Everything the dashboard needs, in one call.</summary>
public class MyDashboardSummaryDto
{
    /// <summary>Null when the signed-in user has no employee record -- an administrator, typically.</summary>
    public MyDashboardProfileDto Profile { get; set; }

    public List<MyDashboardLeaveBalanceDto> LeaveBalances { get; set; }

    /// <summary>Null when the user has no employee record.</summary>
    public MyDashboardAttendanceDto TodayAttendance { get; set; }

    public MyDashboardAlertsDto Alerts { get; set; }

    /// <summary>Active announcements, newest first. Empty rather than null when there are none.</summary>
    public List<MyDashboardAnnouncementDto> Announcements { get; set; }
}

/// <summary>
/// Today's clockings, read from TimeEvents.
///
/// A DISPLAY CONVENIENCE, NOT THE PAYROLL FIGURE. WorkedHours here is the sum of paired in/out
/// intervals recorded so far. The Time Engine owns the real calculation -- shift rules, grace periods,
/// breaks, overtime -- and this tile deliberately does not attempt any of that. It answers "when did I
/// arrive and how long have I been here", which is what someone glancing at a dashboard wants.
/// </summary>
public class MyDashboardAttendanceDto
{
    public DateTime Date { get; set; }

    /// <summary>First clock-in of the day, or null before the first punch.</summary>
    public DateTime? FirstIn { get; set; }

    /// <summary>Last clock-out, null while still clocked in.</summary>
    public DateTime? LastOut { get; set; }

    /// <summary>Total punches today. Shown so a mid-day break is not mistaken for a missing punch.</summary>
    public int PunchCount { get; set; }

    /// <summary>Sum of completed in/out pairs, in hours. Excludes an open period still running.</summary>
    public decimal WorkedHours { get; set; }

    /// <summary>True when the last event was a clock-in: the person is currently on site.</summary>
    public bool IsCurrentlyIn { get; set; }
}

/// <summary>
/// One announcement, as the dashboard shows it.
///
/// Bilingual because the legacy screen was: Title/TitleAr and Comments/CommentAr are separate columns,
/// and picking one server-side would fix the language at fetch time rather than at render time.
/// </summary>
public class MyDashboardAnnouncementDto
{
    public int Id { get; set; }

    public string Title { get; set; }

    public string TitleAr { get; set; }

    public string Body { get; set; }

    public string BodyAr { get; set; }

    public DateTime IssueDate { get; set; }
}
