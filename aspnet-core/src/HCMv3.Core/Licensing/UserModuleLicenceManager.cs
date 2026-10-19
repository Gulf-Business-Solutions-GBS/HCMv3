using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Features;
using Abp.Authorization.Users;
using Abp.Domain.Repositories;
using Abp.Runtime.Session;
using Abp.Threading;
using Abp.UI;
using HCMv3.Authorization.Roles;
using HCMv3.Authorization.Users;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Licensing;

/// <inheritdoc cref="IUserModuleLicenceManager"/>
/// <remarks>
/// <para><b>What counts against a seat</b> (legacy <c>LicenseManagerService.GetAssignedUserLicenseCount</c>): a licence
/// held by a user who is active and not an administrator. Deactivating a user frees the seat without unassigning it,
/// as in legacy; reactivating them takes it back, and can therefore leave a module over its count -- the assignment
/// screen shows that in red. Zero's own Max User Count, by contrast, counts every user; it is left as Zero has it.</para>
/// </remarks>
public class UserModuleLicenceManager : HCMv3DomainServiceBase, IUserModuleLicenceManager
{
    private readonly IRepository<UserModuleLicence> _licenceRepository;
    private readonly IRepository<User, long> _userRepository;
    private readonly IRepository<UserRole, long> _userRoleRepository;
    private readonly IRepository<Role> _roleRepository;
    private readonly IFeatureChecker _featureChecker;
    private readonly IFeatureManager _featureManager;
    private readonly IUserModuleLicenceChecker _licenceChecker;
    private readonly IAbpSession _abpSession;

    public UserModuleLicenceManager(
        IRepository<UserModuleLicence> licenceRepository,
        IRepository<User, long> userRepository,
        IRepository<UserRole, long> userRoleRepository,
        IRepository<Role> roleRepository,
        IFeatureChecker featureChecker,
        IFeatureManager featureManager,
        IUserModuleLicenceChecker licenceChecker,
        IAbpSession abpSession)
    {
        _licenceRepository = licenceRepository;
        _userRepository = userRepository;
        _userRoleRepository = userRoleRepository;
        _roleRepository = roleRepository;
        _featureChecker = featureChecker;
        _featureManager = featureManager;
        _licenceChecker = licenceChecker;
        _abpSession = abpSession;
    }

    public async Task<List<LicenceModuleUsage>> GetUsageAsync()
    {
        var result = new List<LicenceModuleUsage>();
        foreach (var definition in LicenceModules.All)
        {
            result.Add(await GetUsageAsync(definition.Module));
        }

        return result;
    }

    public async Task<LicenceModuleUsage> GetUsageAsync(LicenceModule module)
    {
        var tenantId = _abpSession.GetTenantId();
        var definition = LicenceModules.Get(module);
        var adminRoleId = await GetAdminRoleIdAsync();

        return new LicenceModuleUsage
        {
            Module = module,
            IsModuleEnabled = await IsModuleEnabledAsync(tenantId, definition),
            SeatCount = await GetSeatCountAsync(tenantId, definition),
            UsedCount = await CountedLicences(module, adminRoleId).CountAsync()
        };
    }

    public async Task AssignAsync(long userId, LicenceModule module)
    {
        var tenantId = _abpSession.GetTenantId();
        var definition = LicenceModules.Get(module);

        var user = await _userRepository.FirstOrDefaultAsync(userId);
        if (user == null)
        {
            throw new UserFriendlyException(L("UserNotFound"));
        }

        if (await IsAdministratorAsync(userId))
        {
            throw new UserFriendlyException(L("LicenceNotNeededForAdministrator", user.UserName));
        }

        if (!await IsModuleEnabledAsync(tenantId, definition))
        {
            throw new UserFriendlyException(L("LicenceModuleNotInEdition", L(definition.DisplayNameKey)));
        }

        if (await HoldsAsync(userId, module))
        {
            return;
        }

        var usage = await GetUsageAsync(module);
        if (!usage.HasFreeSeat)
        {
            throw new UserFriendlyException(L("LicenceSeatLimitReached", L(definition.DisplayNameKey), usage.SeatCount));
        }

        await InsertAsync(tenantId, userId, module);
    }

    public async Task UnassignAsync(long userId, LicenceModule module)
    {
        var tenantId = _abpSession.GetTenantId();

        var rows = await _licenceRepository.GetAll()
            .Where(l => l.UserId == userId && l.Module == module)
            .ToListAsync();

        foreach (var row in rows)
        {
            await _licenceRepository.DeleteAsync(row);
        }

        await InvalidateAsync(tenantId, userId);
    }

    public async Task<List<LicenceModule>> AssignOnEmployeeLinkAsync(long userId)
    {
        var assigned = new List<LicenceModule>();

        // Without HcmFeatureProvider registered there is nothing to assign against -- and the employee link that
        // calls this must not fail because of licensing.
        if (_featureManager.GetOrNull(HcmFeatures.Seats) == null || !_abpSession.TenantId.HasValue)
        {
            return assigned;
        }

        if (await IsAdministratorAsync(userId))
        {
            return assigned;
        }

        var tenantId = _abpSession.TenantId.Value;

        foreach (var module in LicenceModules.AssignedOnEmployeeLink)
        {
            var definition = LicenceModules.Get(module);

            if (!await IsModuleEnabledAsync(tenantId, definition) || await HoldsAsync(userId, module))
            {
                continue;
            }

            if (!(await GetUsageAsync(module)).HasFreeSeat)
            {
                continue;
            }

            await InsertAsync(tenantId, userId, module);
            assigned.Add(module);
        }

        return assigned;
    }

    private async Task InsertAsync(int tenantId, long userId, LicenceModule module)
    {
        var licence = new UserModuleLicence
        {
            UserId = userId,
            Module = module
        };
        licence.TenantId = tenantId;

        await _licenceRepository.InsertAsync(licence);
        await CurrentUnitOfWork.SaveChangesAsync();

        await InvalidateAsync(tenantId, userId);
    }

    /// <summary>
    /// Cleared at once and again when the unit of work commits, so a check made in between cannot re-cache the
    /// state from before this change.
    /// </summary>
    private async Task InvalidateAsync(int tenantId, long userId)
    {
        await _licenceChecker.InvalidateAsync(tenantId, userId);

        var unitOfWork = CurrentUnitOfWork;
        if (unitOfWork != null)
        {
            var checker = _licenceChecker;
            unitOfWork.Completed += (_, _) => AsyncHelper.RunSync(() => checker.InvalidateAsync(tenantId, userId));
        }
    }

    private Task<bool> HoldsAsync(long userId, LicenceModule module)
    {
        return _licenceRepository.GetAll().AnyAsync(l => l.UserId == userId && l.Module == module);
    }

    private IQueryable<UserModuleLicence> CountedLicences(LicenceModule module, int? adminRoleId)
    {
        var activeUsers = _userRepository.GetAll()
            .Where(u => u.IsActive)
            .Where(u => !adminRoleId.HasValue
                        || !_userRoleRepository.GetAll().Any(ur => ur.UserId == u.Id && ur.RoleId == adminRoleId.Value));

        return _licenceRepository.GetAll()
            .Where(l => l.Module == module)
            .Where(l => activeUsers.Any(u => u.Id == l.UserId));
    }

    private async Task<bool> IsAdministratorAsync(long userId)
    {
        var adminRoleId = await GetAdminRoleIdAsync();
        return adminRoleId.HasValue
               && await _userRoleRepository.GetAll().AnyAsync(ur => ur.UserId == userId && ur.RoleId == adminRoleId.Value);
    }

    private Task<int?> GetAdminRoleIdAsync()
    {
        return _roleRepository.GetAll()
            .Where(r => r.Name == StaticRoleNames.Tenants.Admin)
            .Select(r => (int?)r.Id)
            .FirstOrDefaultAsync();
    }

    private async Task<bool> IsModuleEnabledAsync(int tenantId, LicenceModules.Definition definition)
    {
        return definition.ModuleFeature == null || await _featureChecker.IsEnabledAsync(tenantId, definition.ModuleFeature);
    }

    private async Task<int> GetSeatCountAsync(int tenantId, LicenceModules.Definition definition)
    {
        var value = await _featureChecker.GetValueAsync(tenantId, definition.SeatFeature);
        return int.TryParse(value, out var count) && count > 0 ? count : 0;
    }
}
