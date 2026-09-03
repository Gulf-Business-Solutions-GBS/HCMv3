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

    public MyDashboardAlertsDto Alerts { get; set; }
}
