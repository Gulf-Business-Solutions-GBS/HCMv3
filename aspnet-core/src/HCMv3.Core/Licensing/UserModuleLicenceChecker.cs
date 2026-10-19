using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Features;
using Abp.Authorization.Users;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Runtime.Caching;
using Abp.Runtime.Session;
using Abp.UI;
using HCMv3.Authorization.Roles;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Licensing;

/// <inheritdoc cref="IUserModuleLicenceChecker"/>
public class UserModuleLicenceChecker : HCMv3DomainServiceBase, IUserModuleLicenceChecker
{
    /// <summary>
    /// How long a user's licences are cached. Assignment changes clear the entry at once; this bounds how late a
    /// role change (becoming or ceasing to be an administrator) is noticed.
    /// </summary>
    public static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(5);

    private static readonly LicenceModule[] SignInModules = { LicenceModule.Admin, LicenceModule.Ess };

    private readonly IRepository<UserModuleLicence> _licenceRepository;
    private readonly IRepository<UserRole, long> _userRoleRepository;
    private readonly IRepository<Role> _roleRepository;
    private readonly IFeatureChecker _featureChecker;
    private readonly IFeatureManager _featureManager;
    private readonly ICacheManager _cacheManager;
    private readonly IAbpSession _abpSession;

    public UserModuleLicenceChecker(
        IRepository<UserModuleLicence> licenceRepository,
        IRepository<UserRole, long> userRoleRepository,
        IRepository<Role> roleRepository,
        IFeatureChecker featureChecker,
        IFeatureManager featureManager,
        ICacheManager cacheManager,
        IAbpSession abpSession)
    {
        _licenceRepository = licenceRepository;
        _userRoleRepository = userRoleRepository;
        _roleRepository = roleRepository;
        _featureChecker = featureChecker;
        _featureManager = featureManager;
        _cacheManager = cacheManager;
        _abpSession = abpSession;
    }

    private ITypedCache<string, UserLicenceCacheItem> Cache =>
        _cacheManager.GetCache<string, UserLicenceCacheItem>(UserLicenceCacheItem.CacheName);

    private static string CacheKey(int tenantId, long userId) => tenantId + "@" + userId;

    public async Task<bool> IsEnforcedAsync(int tenantId)
    {
        // Defensive: if HcmFeatureProvider were not registered, asking for the feature would throw on every
        // permission check and take the whole application down. Unregistered means "not enforced".
        if (_featureManager.GetOrNull(HcmFeatures.Seats) == null)
        {
            return false;
        }

        return await _featureChecker.IsEnabledAsync(tenantId, HcmFeatures.Seats);
    }

    public bool IsEnforced(int tenantId)
    {
        if (_featureManager.GetOrNull(HcmFeatures.Seats) == null)
        {
            return false;
        }

        return _featureChecker.IsEnabled(tenantId, HcmFeatures.Seats);
    }

    public async Task<bool> SessionUserHoldsAsync(int tenantId, IReadOnlyCollection<LicenceModule> modules)
    {
        if (!IsSessionUserChecked(tenantId))
        {
            return true;
        }

        return await UserHoldsAsync(tenantId, _abpSession.UserId!.Value, modules);
    }

    public bool SessionUserHolds(int tenantId, IReadOnlyCollection<LicenceModule> modules)
    {
        if (!IsSessionUserChecked(tenantId))
        {
            return true;
        }

        return UserHolds(tenantId, _abpSession.UserId!.Value, modules);
    }

    private bool IsSessionUserChecked(int tenantId)
    {
        return _abpSession.UserId.HasValue
               && _abpSession.TenantId == tenantId
               && !_abpSession.ImpersonatorUserId.HasValue;
    }

    public async Task<bool> UserHoldsAsync(int tenantId, long userId, IReadOnlyCollection<LicenceModule> modules)
    {
        if (modules.Count == 0 || !await IsEnforcedAsync(tenantId))
        {
            return true;
        }

        return Holds(await GetAsync(tenantId, userId), modules);
    }

    public bool UserHolds(int tenantId, long userId, IReadOnlyCollection<LicenceModule> modules)
    {
        if (modules.Count == 0 || !IsEnforced(tenantId))
        {
            return true;
        }

        return Holds(Get(tenantId, userId), modules);
    }

    private static bool Holds(UserLicenceCacheItem item, IReadOnlyCollection<LicenceModule> modules)
    {
        return item.IsExempt || modules.All(m => item.Modules.Contains(m));
    }

    public async Task EnsureCanSignInAsync(int? tenantId, long userId)
    {
        if (!tenantId.HasValue || !await IsEnforcedAsync(tenantId.Value))
        {
            return;
        }

        var item = await GetAsync(tenantId.Value, userId);
        if (item.IsExempt || item.Modules.Any(m => SignInModules.Contains(m)))
        {
            return;
        }

        throw new UserFriendlyException(L("NoLicenceAssignedCannotSignIn"));
    }

    public async Task<UserLicenceCacheItem> GetAsync(int tenantId, long userId)
    {
        var key = CacheKey(tenantId, userId);

        var cached = await Cache.GetOrDefaultAsync(key);
        if (cached != null)
        {
            return cached;
        }

        UserLicenceCacheItem item;
        using (var uow = UnitOfWorkManager.Begin())
        {
            using (UnitOfWorkManager.Current.SetTenantId(tenantId))
            {
                var adminRoleId = await _roleRepository.GetAll()
                    .Where(r => r.Name == StaticRoleNames.Tenants.Admin)
                    .Select(r => (int?)r.Id)
                    .FirstOrDefaultAsync();

                item = new UserLicenceCacheItem
                {
                    IsExempt = adminRoleId.HasValue && await _userRoleRepository.GetAll()
                        .AnyAsync(ur => ur.UserId == userId && ur.RoleId == adminRoleId.Value),
                    Modules = await _licenceRepository.GetAll()
                        .Where(l => l.UserId == userId)
                        .Select(l => l.Module)
                        .Distinct()
                        .ToListAsync()
                };
            }

            await uow.CompleteAsync();
        }

        await Cache.SetAsync(key, item, absoluteExpireTime: DateTimeOffset.UtcNow.Add(CacheLifetime));
        return item;
    }

    private UserLicenceCacheItem Get(int tenantId, long userId)
    {
        var key = CacheKey(tenantId, userId);

        var cached = Cache.GetOrDefault(key);
        if (cached != null)
        {
            return cached;
        }

        UserLicenceCacheItem item;
        using (var uow = UnitOfWorkManager.Begin())
        {
            using (UnitOfWorkManager.Current.SetTenantId(tenantId))
            {
                var adminRoleId = _roleRepository.GetAll()
                    .Where(r => r.Name == StaticRoleNames.Tenants.Admin)
                    .Select(r => (int?)r.Id)
                    .FirstOrDefault();

                item = new UserLicenceCacheItem
                {
                    IsExempt = adminRoleId.HasValue && _userRoleRepository.GetAll()
                        .Any(ur => ur.UserId == userId && ur.RoleId == adminRoleId.Value),
                    Modules = _licenceRepository.GetAll()
                        .Where(l => l.UserId == userId)
                        .Select(l => l.Module)
                        .Distinct()
                        .ToList()
                };
            }

            uow.Complete();
        }

        Cache.Set(key, item, absoluteExpireTime: DateTimeOffset.UtcNow.Add(CacheLifetime));
        return item;
    }

    public async Task InvalidateAsync(int tenantId, long userId)
    {
        await Cache.RemoveAsync(CacheKey(tenantId, userId));
    }
}
