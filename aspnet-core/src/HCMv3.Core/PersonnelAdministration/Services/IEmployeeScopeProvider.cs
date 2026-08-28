using System;
using System.Linq;
using System.Threading.Tasks;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// Resolves "which employees may the current user act on" for Employee Self Service and manager screens.
///
/// This is the single place that answers that question. ESS application services must not hand-write
/// <c>.Where(x =&gt; x.EmployeeId == currentEmployeeId)</c>: doing it per screen is exactly how a scoped list
/// query ends up paired with an unscoped update endpoint. Resolve a scope once per request, use
/// <see cref="EmployeeScopeFilter.Apply{T}"/> on the read, and <see cref="EmployeeScopeFilter.EnsureIncludes"/>
/// on every write.
///
/// Identity comes from <see cref="IEmployeeSessionService.GetCurrentEmployeeIdAsync"/>, i.e. from the
/// <c>Employee.UserId</c> link. A user with no linked employee resolves to
/// <see cref="EmployeeScopeFilter.Empty"/> (sees nothing), never to unrestricted.
/// </summary>
public interface IEmployeeScopeProvider
{
    /// <summary>The current user's own employee id, or <c>null</c> if the session has no user or the user is
    /// not linked to an employee.</summary>
    Task<int?> GetCurrentEmployeeIdAsync();

    /// <summary>
    /// Same as <see cref="GetCurrentEmployeeIdAsync"/> but throws a localized
    /// <c>UserFriendlyException</c> instead of returning <c>null</c>. Use this at the top of any ESS endpoint
    /// -- "you are signed in but not linked to an employee record" is an actionable message for the user and
    /// their HR admin, whereas an empty result set is not.
    /// </summary>
    Task<int> GetRequiredCurrentEmployeeIdAsync();

    /// <summary>
    /// Resolves <paramref name="scope"/> for the current user into a concrete filter.
    /// <paramref name="asOf"/> dates the employee-manager hierarchy lookup and defaults to today; pass an
    /// explicit date when scoping historical data so that a reorganisation does not retroactively change who
    /// could see what.
    /// </summary>
    Task<EmployeeScopeFilter> GetScopeAsync(EmployeeScope scope, DateTime? asOf = null);

    /// <summary>Convenience wrapper: resolve <paramref name="scope"/> and apply it to
    /// <paramref name="query"/> in one call.</summary>
    Task<IQueryable<T>> ApplyScopeAsync<T>(IQueryable<T> query, EmployeeScope scope, DateTime? asOf = null)
        where T : IHasEmployeeId;

    /// <summary>Whether <paramref name="employeeId"/> falls inside <paramref name="scope"/> for the current
    /// user.</summary>
    Task<bool> IsInScopeAsync(int employeeId, EmployeeScope scope, DateTime? asOf = null);

    /// <summary>Guard version of <see cref="IsInScopeAsync"/>; throws <c>AbpAuthorizationException</c> when
    /// out of scope.</summary>
    Task EnsureInScopeAsync(int employeeId, EmployeeScope scope, DateTime? asOf = null);
}
