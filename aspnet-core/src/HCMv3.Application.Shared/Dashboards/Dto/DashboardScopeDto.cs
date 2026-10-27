using System.Collections.Generic;

namespace HCMv3.Dashboards.Dto;

/// <summary>
/// Whose data a dashboard is showing and why, so the screen can say "Showing: Finance, Payroll + your team".
/// Every dashboard returns one of these; the scope itself is always decided by the server.
/// </summary>
public class DashboardScopeDto
{
    public DashboardScopeReason Reason { get; set; }

    /// <summary>Org units given to the user's roles in row-level security. Empty unless the reason is Restricted.</summary>
    public List<string> OrgUnitNames { get; set; } = new();

    /// <summary>The user's own team (themselves and everybody below them) is part of the scope.</summary>
    public bool IncludesOwnTeam { get; set; }

    /// <summary>Active employees the dashboard covers.</summary>
    public int EmployeesInScope { get; set; }
}
