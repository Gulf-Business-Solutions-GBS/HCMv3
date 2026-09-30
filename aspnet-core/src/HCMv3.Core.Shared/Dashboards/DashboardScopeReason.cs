namespace HCMv3.Dashboards;

/// <summary>Why a dashboard shows the employees it shows. Sent to the screen so it can say so.</summary>
public enum DashboardScopeReason
{
    /// <summary>The user holds the tenant's Admin role.</summary>
    Admin = 1,

    /// <summary>The org units given to the user's roles in row-level security, their own team, or both.</summary>
    Restricted = 2,

    /// <summary>No org units on the user's roles and no team under them (or no linked employee record). Shows
    /// nothing rather than everything.</summary>
    Nobody = 3
}
