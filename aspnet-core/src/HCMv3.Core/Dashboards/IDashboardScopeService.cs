using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;

namespace HCMv3.Dashboards;

/// <summary>
/// The one rule every dashboard uses for "whose data may the signed-in user see". A dashboard never asks the
/// user to pick; it calls this and shows what comes back.
///
/// <list type="number">
/// <item>The tenant's Admin role: every employee.</item>
/// <item>Otherwise, the union of: the employees in the org units given to the user's roles in row-level security
/// (with every unit below them; the top-level unit also brings in employees with no assignment), and the user's
/// own team -- themselves and everybody below them in the manager hierarchy.</item>
/// <item>If that union is empty: nobody. Never everybody.</item>
/// </list>
///
/// The row-level security on/off switch in Security Settings is NOT consulted: nothing assigned means nothing
/// shown. That is the difference from <c>IRlsService.GetRlsEmployeeIdsAsync</c>, which returns an empty list both
/// when the switch is off and when the user has nothing assigned, and whose callers read empty as "no restriction".
/// </summary>
public interface IDashboardScopeService : ITransientDependency
{
    Task<DashboardScope> GetScopeAsync();
}

/// <summary>The resolved scope: either every employee, or a concrete (possibly empty) set.</summary>
public sealed class DashboardScope
{
    private DashboardScope(
        DashboardScopeReason reason,
        IReadOnlyCollection<int> employeeIds,
        IReadOnlyList<string> orgUnitNames,
        bool includesOwnTeam)
    {
        Reason = reason;
        EmployeeIds = employeeIds;
        OrgUnitNames = orgUnitNames;
        IncludesOwnTeam = includesOwnTeam;
    }

    public static DashboardScope Everyone(DashboardScopeReason reason) =>
        new(reason, new List<int>(), new List<string>(), false);

    public static DashboardScope Restricted(IEnumerable<int> employeeIds, IReadOnlyList<string> orgUnitNames, bool includesOwnTeam)
    {
        var ids = employeeIds.Distinct().ToList();
        return ids.Count == 0
            ? new DashboardScope(DashboardScopeReason.Nobody, ids, new List<string>(), false)
            : new DashboardScope(DashboardScopeReason.Restricted, ids, orgUnitNames, includesOwnTeam);
    }

    public DashboardScopeReason Reason { get; }

    /// <summary>True for every employee; <see cref="EmployeeIds"/> is then empty and must not be read as "nobody".</summary>
    public bool IsEveryone => Reason == DashboardScopeReason.Admin;

    public IReadOnlyCollection<int> EmployeeIds { get; }

    /// <summary>Names of the org units the user's roles are given, for "Showing: Finance, Payroll + your team".</summary>
    public IReadOnlyList<string> OrgUnitNames { get; }

    public bool IncludesOwnTeam { get; }

    public bool Includes(int employeeId) => IsEveryone || EmployeeIds.Contains(employeeId);
}
