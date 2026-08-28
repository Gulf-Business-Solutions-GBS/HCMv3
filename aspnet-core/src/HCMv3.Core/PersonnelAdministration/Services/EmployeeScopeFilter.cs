using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Abp.Authorization;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// The resolved outcome of an <see cref="EmployeeScope"/> for the current user: either "no restriction" or a
/// concrete set of employee ids. Produced by <see cref="IEmployeeScopeProvider.GetScopeAsync"/>.
///
/// Immutable and cheap to pass around, so an application service can resolve the scope once per request and
/// then reuse it for the list query, the per-row authorization check on edit/delete, and the DTO projection.
/// </summary>
public sealed class EmployeeScopeFilter
{
    private static readonly List<int> NoIds = new List<int>();

    private readonly List<int> _employeeIds;

    private EmployeeScopeFilter(bool isUnrestricted, List<int> employeeIds)
    {
        IsUnrestricted = isUnrestricted;
        _employeeIds = employeeIds;
    }

    /// <summary>Matches every employee. Corresponds to <see cref="EmployeeScope.All"/>.</summary>
    public static EmployeeScopeFilter Unrestricted { get; } = new EmployeeScopeFilter(true, NoIds);

    /// <summary>
    /// Matches no employee at all. This is what a scope resolves to when the current user has no linked
    /// <see cref="Employee"/> -- deliberately "sees nothing" rather than "sees everything", so that a
    /// missing user-employee link can never silently widen access.
    /// </summary>
    public static EmployeeScopeFilter Empty { get; } = new EmployeeScopeFilter(false, NoIds);

    public static EmployeeScopeFilter For(IEnumerable<int> employeeIds)
    {
        var ids = employeeIds?.Distinct().ToList() ?? new List<int>();

        return ids.Count == 0 ? Empty : new EmployeeScopeFilter(false, ids);
    }

    public bool IsUnrestricted { get; }

    /// <summary>The employees in scope. Always empty when <see cref="IsUnrestricted"/> is true -- check that
    /// flag first rather than reading this as "nobody".</summary>
    public IReadOnlyList<int> EmployeeIds => _employeeIds;

    /// <summary>True when this filter matches nothing (as opposed to matching everything).</summary>
    public bool IsEmpty => !IsUnrestricted && _employeeIds.Count == 0;

    public bool Includes(int employeeId)
    {
        return IsUnrestricted || _employeeIds.Contains(employeeId);
    }

    /// <summary>
    /// Guard for write paths: throws when <paramref name="employeeId"/> is outside the scope. Use this on
    /// every create/update/delete that takes an employee id from the client, otherwise a scoped list query
    /// hides other employees' rows while the write endpoint still accepts their ids.
    /// </summary>
    public void EnsureIncludes(int employeeId)
    {
        if (!Includes(employeeId))
        {
            throw new AbpAuthorizationException("Employee " + employeeId + " is outside the caller's employee scope.");
        }
    }

    /// <summary>
    /// Narrows a query of employee-owned rows to this scope.
    ///
    /// The predicate is built with <see cref="Expression"/> against <c>typeof(T)</c>'s own declared
    /// <c>EmployeeId</c> property rather than written as a plain <c>e =&gt; ids.Contains(e.EmployeeId)</c>
    /// lambda. That matters: because <typeparamref name="T"/> is constrained to
    /// <see cref="IHasEmployeeId"/>, C# would bind the member access to the *interface* property and emit
    /// an expression tree EF Core cannot translate to SQL, silently falling back to client evaluation or
    /// throwing. Binding to the concrete property keeps it a real SQL <c>IN</c>.
    /// </summary>
    public IQueryable<T> Apply<T>(IQueryable<T> query) where T : IHasEmployeeId
    {
        if (IsUnrestricted)
        {
            return query;
        }

        return query.Where(BuildPredicate<T>(nameof(IHasEmployeeId.EmployeeId)));
    }

    /// <summary>
    /// Narrows a query over <see cref="Employee"/> itself to this scope. Separate from <see cref="Apply{T}"/>
    /// because Employee is scoped by its own primary key, not by an <c>EmployeeId</c> FK.
    /// </summary>
    public IQueryable<Employee> ApplyToEmployees(IQueryable<Employee> query)
    {
        if (IsUnrestricted)
        {
            return query;
        }

        var ids = _employeeIds;

        return query.Where(e => ids.Contains(e.Id));
    }

    private Expression<Func<T, bool>> BuildPredicate<T>(string propertyName)
    {
        var parameter = Expression.Parameter(typeof(T), "e");
        var property = Expression.Property(parameter, propertyName);

        var containsCall = Expression.Call(
            Expression.Constant(_employeeIds),
            typeof(List<int>).GetMethod(nameof(List<int>.Contains), new[] { typeof(int) }),
            property);

        return Expression.Lambda<Func<T, bool>>(containsCall, parameter);
    }
}
