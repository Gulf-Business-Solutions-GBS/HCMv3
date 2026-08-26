using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Authorization.Users;
using Abp.Domain.Repositories;
using Abp.Runtime.Session;
using HCMv3.Authorization;
using HCMv3.ReportManagement.Dto;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.ReportManagement;

/// <summary>
/// Feeds the "Print Reports" launcher screen: the list of report catalog entries the current user
/// may run, each flagged with whether a ported report class actually exists for it.
///
/// PERMISSION MODEL (ported from legacy HCM10, which gated the Print Reports list per role via
/// PrintReportPermission / PrintReportPermissionDetail):
///   * host users and tenant admins see every active catalog row -- they are the users who maintain
///     the catalog, and hiding rows from them would make the admin screen and the launcher disagree;
///   * every other user sees only rows reachable through a <see cref="PrintReportPermission"/> whose
///     RoleId is one of their roles.
///
/// This is FAIL-CLOSED: a non-admin user with no matching PrintReportPermission row sees an EMPTY
/// list, not the whole catalog. That mirrors the legacy grant model (access is granted, never
/// assumed) and is the safe direction to be wrong in; the alternative would silently widen access
/// for every tenant that has not configured grants yet.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_PrintReports)]
public class PrintReportLauncherAppService : HCMv3AppServiceBase, IPrintReportLauncherAppService
{
    private readonly IRepository<PrintReport> _printReportRepository;
    private readonly IRepository<PrintReportPermission> _permissionRepository;
    private readonly IRepository<PrintReportPermissionDetail> _permissionDetailRepository;
    private readonly IRepository<UserRole, long> _userRoleRepository;

    public PrintReportLauncherAppService(
        IRepository<PrintReport> printReportRepository,
        IRepository<PrintReportPermission> permissionRepository,
        IRepository<PrintReportPermissionDetail> permissionDetailRepository,
        IRepository<UserRole, long> userRoleRepository)
    {
        _printReportRepository = printReportRepository;
        _permissionRepository = permissionRepository;
        _permissionDetailRepository = permissionDetailRepository;
        _userRoleRepository = userRoleRepository;
    }

    public async Task<List<RunnableReportDto>> GetRunnableReports()
    {
        var query = _printReportRepository.GetAll().Where(r => r.IsActive);

        var allowedIds = await GetAllowedPrintReportIdsOrNull();
        if (allowedIds != null)
        {
            query = query.Where(r => allowedIds.Contains(r.Id));
        }

        var rows = await query
            .OrderBy(r => r.Code)
            .ThenBy(r => r.NameEn)
            .ToListAsync();

        return rows.Select(ToDto).ToList();
    }

    /// <summary>
    /// The set of <see cref="PrintReport"/> ids the current user is granted, or NULL meaning
    /// "unrestricted" (host user or tenant admin). Null and an empty set mean opposite things --
    /// do not collapse them.
    /// </summary>
    private async Task<List<int>> GetAllowedPrintReportIdsOrNull()
    {
        if (await IsCurrentUserUnrestrictedAsync())
        {
            return null;
        }

        var roleIds = await GetCurrentUserRoleIdsAsync();

        if (!roleIds.Any())
        {
            return new List<int>();
        }

        var permissionIds = await _permissionRepository.GetAll()
            .Where(p => roleIds.Contains(p.RoleId))
            .Select(p => p.Id)
            .ToListAsync();

        if (!permissionIds.Any())
        {
            return new List<int>();
        }

        return await _permissionDetailRepository.GetAll()
            .Where(d => permissionIds.Contains(d.PrintReportPermissionId))
            .Select(d => d.PrintReportId)
            .Distinct()
            .ToListAsync();
    }

    /// <summary>
    /// True for users the per-role grant model does not apply to: host users (no tenant catalog to
    /// scope to) and tenant admins. Virtual so unit tests can drive the branch without standing up
    /// ASP.NET Zero's concrete <c>UserManager</c>.
    /// </summary>
    protected virtual async Task<bool> IsCurrentUserUnrestrictedAsync()
    {
        if (!AbpSession.TenantId.HasValue || !AbpSession.UserId.HasValue)
        {
            return true;
        }

        var user = await GetCurrentUserAsync();
        return await UserManager.IsInRoleAsync(user, Authorization.Roles.StaticRoleNames.Tenants.Admin);
    }

    /// <summary>Role ids of the current user. Virtual for the same testability reason as above.</summary>
    protected virtual Task<List<int>> GetCurrentUserRoleIdsAsync()
    {
        var userId = AbpSession.GetUserId();

        return _userRoleRepository.GetAll()
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync();
    }

    private static RunnableReportDto ToDto(PrintReport entity) => new RunnableReportDto
    {
        Id = entity.Id,
        Code = entity.Code,
        NameEn = entity.NameEn,
        NameAr = entity.NameAr,
        ReportName = entity.ReportName,
        ReportCategory = entity.ReportCategory.ToString(),

        // Computed, never stored: a catalog row is only runnable if a report class was actually
        // ported for it. See PortedReportCatalog for why this crosses the layer boundary as a
        // process-wide set of names rather than a DI service.
        IsAvailable = PortedReportCatalog.IsAvailable(entity.ReportName)
    };
}
