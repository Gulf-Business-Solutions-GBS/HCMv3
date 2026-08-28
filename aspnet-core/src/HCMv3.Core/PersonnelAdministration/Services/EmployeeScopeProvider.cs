using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.SystemSecurity.Services;

namespace HCMv3.PersonnelAdministration.Services;

/// <inheritdoc cref="IEmployeeScopeProvider"/>
/// <remarks>
/// <para>
/// <b>Reuse of <see cref="IRlsService"/>.</b> <see cref="EmployeeScope.Subordinates"/> delegates to
/// <see cref="IRlsService.GetEmployeeIdsUnderManagerAsync"/> rather than re-implementing the recursive
/// manager walk. That method is a pure, cycle-safe hierarchy traversal and -- unlike
/// <c>GetRlsEmployeeIdsAsync</c> -- is NOT gated on the tenant's <c>EnableRowLevelSecurity</c> setting, so
/// it is safe to depend on for ESS, which must work whether or not admin RLS is switched on. There is no
/// dependency cycle: <c>RlsService</c> takes <see cref="IEmployeeSessionService"/>, not this service.
/// </para>
/// <para>
/// <b>Layer note.</b> <c>HCMv3.Core</c> has no reference to EntityFrameworkCore, so this service uses the
/// synchronous <c>IQueryable</c> from <c>IRepository&lt;T&gt;.GetAll()</c> -- the same pattern as
/// <see cref="RlsService"/> and <c>OrgHierarchyService</c>.
/// </para>
/// </remarks>
[UnitOfWork]
public class EmployeeScopeProvider : HCMv3DomainServiceBase, IEmployeeScopeProvider
{
    private readonly IEmployeeSessionService _employeeSessionService;
    private readonly IRepository<EmployeeManager> _employeeManagerRepository;
    private readonly IRlsService _rlsService;

    public EmployeeScopeProvider(
        IEmployeeSessionService employeeSessionService,
        IRepository<EmployeeManager> employeeManagerRepository,
        IRlsService rlsService)
    {
        _employeeSessionService = employeeSessionService;
        _employeeManagerRepository = employeeManagerRepository;
        _rlsService = rlsService;
    }

    public Task<int?> GetCurrentEmployeeIdAsync()
    {
        return _employeeSessionService.GetCurrentEmployeeIdAsync();
    }

    public async Task<int> GetRequiredCurrentEmployeeIdAsync()
    {
        var employeeId = await _employeeSessionService.GetCurrentEmployeeIdAsync();
        if (!employeeId.HasValue)
        {
            throw new UserFriendlyException(L("CurrentUserIsNotLinkedToAnEmployee"));
        }

        return employeeId.Value;
    }

    public async Task<EmployeeScopeFilter> GetScopeAsync(EmployeeScope scope, DateTime? asOf = null)
    {
        if (scope == EmployeeScope.All)
        {
            return EmployeeScopeFilter.Unrestricted;
        }

        var currentEmployeeId = await _employeeSessionService.GetCurrentEmployeeIdAsync();
        if (!currentEmployeeId.HasValue)
        {
            // Signed in but not linked to an employee: match nothing rather than everything.
            return EmployeeScopeFilter.Empty;
        }

        var effectiveDate = (asOf ?? DateTime.Today).Date;

        switch (scope)
        {
            case EmployeeScope.Self:
                return EmployeeScopeFilter.For(new[] { currentEmployeeId.Value });

            case EmployeeScope.DirectReports:
                return EmployeeScopeFilter.For(GetDirectReportIds(currentEmployeeId.Value, effectiveDate));

            case EmployeeScope.Team:
                var team = new List<int> { currentEmployeeId.Value };
                team.AddRange(GetDirectReportIds(currentEmployeeId.Value, effectiveDate));
                return EmployeeScopeFilter.For(team);

            case EmployeeScope.Subordinates:
                // Includes the root employee itself -- see IRlsService.GetEmployeeIdsUnderManagerAsync.
                return EmployeeScopeFilter.For(await _rlsService.GetEmployeeIdsUnderManagerAsync(currentEmployeeId.Value));

            default:
                throw new ArgumentOutOfRangeException(nameof(scope), scope, "Unsupported employee scope.");
        }
    }

    public async Task<IQueryable<T>> ApplyScopeAsync<T>(IQueryable<T> query, EmployeeScope scope, DateTime? asOf = null)
        where T : IHasEmployeeId
    {
        var filter = await GetScopeAsync(scope, asOf);

        return filter.Apply(query);
    }

    public async Task<bool> IsInScopeAsync(int employeeId, EmployeeScope scope, DateTime? asOf = null)
    {
        var filter = await GetScopeAsync(scope, asOf);

        return filter.Includes(employeeId);
    }

    public async Task EnsureInScopeAsync(int employeeId, EmployeeScope scope, DateTime? asOf = null)
    {
        var filter = await GetScopeAsync(scope, asOf);

        filter.EnsureIncludes(employeeId);
    }

    /// <summary>
    /// One level down only. <c>ManagerId != EmployeeId</c> mirrors the same self-reference guard
    /// <see cref="RlsService"/> applies -- a legacy data set can contain rows where an employee is recorded
    /// as their own manager, which would otherwise put them in their own "direct reports".
    /// </summary>
    private List<int> GetDirectReportIds(int managerEmployeeId, DateTime asOf)
    {
        return _employeeManagerRepository.GetAll()
            .Where(m => m.ManagerId == managerEmployeeId
                        && m.ManagerId != m.EmployeeId
                        && m.StartDate <= asOf
                        && m.EndDate >= asOf)
            .Select(m => m.EmployeeId)
            .Distinct()
            .ToList();
    }
}
