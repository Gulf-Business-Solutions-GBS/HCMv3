using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Abp.Domain.Uow;
using Abp.Authorization;
using Abp.Configuration;
using Abp.Application.Services.Dto;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Dynamic.Core;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.Configuration;
using HCMv3.Navigation.Dto;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Navigation;

/// <summary>
/// Serves the screen catalogue, the caller's shortcuts and the tenant's shared quick links, and lets an
/// administrator re-map a Screen Code.
/// </summary>
[AbpAuthorize]
public class ScreensAppService : HCMv3AppServiceBase, IScreensAppService
{
    /// <summary>
    /// Six each, as agreed. A quick-links tile long enough to need scanning has become the menu again,
    /// which is the thing it exists to save you from.
    /// </summary>
    private const int QuickLinkCap = 6;

    /// <summary>
    /// Shown instead of favourites when someone has pinned nothing yet, so the tile is useful on the
    /// first day rather than an empty box asking to be configured.
    /// </summary>
    private const int FallbackMostVisitedCount = 3;

    private readonly IRepository<Screen> _screenRepository;
    private readonly IRepository<SharedQuickLink> _sharedQuickLinkRepository;
    private readonly IRepository<UserScreenUsage> _usageRepository;
    private readonly IScreenSeeder _screenSeeder;
    private readonly IPermissionManager _permissionManager;

    public ScreensAppService(
        IRepository<Screen> screenRepository,
        IRepository<SharedQuickLink> sharedQuickLinkRepository,
        IRepository<UserScreenUsage> usageRepository,
        IScreenSeeder screenSeeder,
        IPermissionManager permissionManager)
    {
        _screenRepository = screenRepository;
        _sharedQuickLinkRepository = sharedQuickLinkRepository;
        _usageRepository = usageRepository;
        _screenSeeder = screenSeeder;
        _permissionManager = permissionManager;
    }

    public async Task<NavigationBootstrapDto> GetNavigationBootstrap()
    {
        var userId = AbpSession.UserId ?? throw new UserFriendlyException(L("YouAreNotLoggedIn"));

        await EnsureCatalogSeededAsync();

        var allScreens = await _screenRepository.GetAll().ToListAsync();
        var visible = await FilterToPermittedAsync(allScreens);
        // Grouped rather than ToDictionary'd outright. The unique index should make a duplicate impossible, but
        // this call feeds the menu of every page: one stray row must cost a duplicate entry, not the whole shell.
        var visibleByKey = visible
            .GroupBy(s => s.ScreenKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var usage = await _usageRepository.GetAll().Where(u => u.UserId == userId).ToListAsync();

        // A usage row whose screen the user can no longer open -- permission withdrawn, screen retired --
        // is dropped from the lists rather than shown as a dead link. The ROW stays: restoring the
        // permission should restore their favourite, not make them pin it again.
        var favorites = usage
            .Where(u => u.IsFavorite && visibleByKey.ContainsKey(u.ScreenKey))
            .OrderByDescending(u => u.VisitCount)
            .ThenByDescending(u => u.LastVisitedAt)
            .Take(QuickLinkCap)
            .Select(MapUsage)
            .ToArray();

        var recent = usage
            .Where(u => u.LastVisitedAt.HasValue && visibleByKey.ContainsKey(u.ScreenKey))
            .OrderByDescending(u => u.LastVisitedAt)
            .Take(5)
            .Select(MapUsage)
            .ToArray();

        if (favorites.Length == 0)
        {
            // Nothing pinned yet: stand in the most-visited so the tile has something to offer. These are
            // NOT marked as favourites -- they are a suggestion, and claiming the user pinned them would
            // be a lie the star icon would then contradict.
            favorites = usage
                .Where(u => u.LastVisitedAt.HasValue && visibleByKey.ContainsKey(u.ScreenKey))
                .OrderByDescending(u => u.VisitCount)
                .Take(FallbackMostVisitedCount)
                .Select(MapUsage)
                .ToArray();
        }

        var sharedLinks = await _sharedQuickLinkRepository.GetAll()
            .OrderBy(l => l.DisplayOrder)
            .ToListAsync();

        var shared = sharedLinks
            .Where(l => visibleByKey.ContainsKey(l.ScreenKey))
            .Select(l => MapScreen(visibleByKey[l.ScreenKey]))
            .Take(QuickLinkCap)
            .ToArray();

        return new NavigationBootstrapDto
        {
            Screens = visible.Select(MapScreen).ToArray(),
            Favorites = favorites,
            Recent = recent,
            SharedQuickLinks = shared,
            ShowScreenCodes = await SettingManager.GetSettingValueAsync<bool>(AppSettings.Navigation.ShowScreenCodes),
        };
    }

    [AbpAuthorize(AppPermissions.Pages_Administration_Navigation_ManageScreenCodes)]
    public async Task UpdateScreenCode(UpdateScreenCodeInput input)
    {
        var code = input.Code?.Trim();
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new UserFriendlyException(L("ScreenCodeRequired"));
        }

        // Codes are how people refer to screens to each other; two screens answering to one code makes
        // every such reference ambiguous.
        var clash = await _screenRepository.GetAll()
            .AnyAsync(s => s.Id != input.Id && s.Code == code);

        if (clash)
        {
            throw new UserFriendlyException(L("ScreenCodeAlreadyUsed", code));
        }

        var screen = await _screenRepository.GetAsync(input.Id);
        screen.Code = code;
        await _screenRepository.UpdateAsync(screen);
    }

    [AbpAuthorize(AppPermissions.Pages_Administration_Navigation_ManageQuickLinks)]
    public async Task CreateOrEditSharedQuickLink(CreateOrEditSharedQuickLinkInput input)
    {
        var exists = await _screenRepository.GetAll().AnyAsync(s => s.ScreenKey == input.ScreenKey);
        if (!exists)
        {
            throw new UserFriendlyException(L("ScreenNotFound"));
        }

        if (input.Id.HasValue)
        {
            var link = await _sharedQuickLinkRepository.GetAsync(input.Id.Value);
            link.ScreenKey = input.ScreenKey;
            link.DisplayOrder = input.DisplayOrder;
            await _sharedQuickLinkRepository.UpdateAsync(link);
            return;
        }

        await _sharedQuickLinkRepository.InsertAsync(new SharedQuickLink
        {
            TenantId = AbpSession.TenantId,
            ScreenKey = input.ScreenKey,
            DisplayOrder = input.DisplayOrder,
        });
    }

    [AbpAuthorize(AppPermissions.Pages_Administration_Navigation_ManageQuickLinks)]
    public async Task DeleteSharedQuickLink(int id)
    {
        await _sharedQuickLinkRepository.DeleteAsync(id);
    }

    /// <summary>
    /// Every screen in the catalogue, paged -- NOT filtered by the caller's permissions, unlike
    /// GetNavigationBootstrap. An administrator assigning codes has to see screens they will never open
    /// themselves; a payroll code cannot be maintained by someone who must first be granted payroll.
    /// The [AbpAuthorize] on the method is what makes that safe.
    /// </summary>
    [HttpPost]
    [AbpAuthorize(AppPermissions.Pages_Administration_Navigation_ManageScreenCodes)]
    public async Task<PagedResultDto<ScreenDto>> GetAllScreens(GetAllScreensInput input)
    {
        var filter = input.Filter?.Trim();

        var query = _screenRepository.GetAll()
            .WhereIf(!string.IsNullOrWhiteSpace(input.Module), s => s.Module == input.Module)
            .WhereIf(
                !string.IsNullOrWhiteSpace(filter),
                s => s.Code.Contains(filter) || s.NameKey.Contains(filter) || s.Route.Contains(filter));

        var total = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<ScreenDto>(total, items.Select(MapScreen).ToList());
    }

    /// <summary>The modules present in the catalogue, for the listing's filter.</summary>
    [AbpAuthorize(AppPermissions.Pages_Administration_Navigation_ManageScreenCodes)]
    public async Task<List<string>> GetScreenModules()
    {
        return await _screenRepository.GetAll()
            .Select(s => s.Module)
            .Distinct()
            .OrderBy(m => m)
            .ToListAsync();
    }

    /// <summary>
    /// Every published quick link, in display order, with its screen resolved.
    ///
    /// Unlike the dashboard's copy this is NOT permission-filtered and NOT capped: an administrator
    /// curating the list must see what they published, including a link to a screen they personally
    /// cannot open, or they would delete rows they simply could not see.
    /// </summary>
    [AbpAuthorize(AppPermissions.Pages_Administration_Navigation_ManageQuickLinks)]
    public async Task<List<SharedQuickLinkDto>> GetSharedQuickLinks()
    {
        var links = await _sharedQuickLinkRepository.GetAll().OrderBy(l => l.DisplayOrder).ToListAsync();
        var screens = await _screenRepository.GetAll().ToListAsync();
        var byKey = screens.ToDictionary(s => s.ScreenKey, StringComparer.OrdinalIgnoreCase);

        return links.Select(l => new SharedQuickLinkDto
        {
            Id = l.Id,
            ScreenKey = l.ScreenKey,
            DisplayOrder = l.DisplayOrder,
            // Left null rather than dropped: a link pointing at a retired screen is exactly what an
            // administrator needs to see in order to remove it.
            Screen = byKey.TryGetValue(l.ScreenKey, out var screen) ? MapScreen(screen) : null,
        }).ToList();
    }

    /// <summary>
    /// Fills the tenant's catalogue with any screens it does not yet have.
    ///
    /// SEEDED LAZILY RATHER THAN AT DEPLOY TIME, for two reasons. A tenant created after the deployment
    /// would otherwise have no catalogue at all, and a release that adds screens would need someone to
    /// remember to run something. Doing it on read means both cases heal themselves.
    ///
    /// A COUNT CANNOT ANSWER THIS, which is what the first version of this check got wrong. Rows are
    /// never deleted, so a screen retired from the catalogue leaves its row behind and pads the total: a
    /// tenant can hold exactly as many rows as the catalogue has entries while still missing the newest
    /// screen. The old guard read those equal numbers as "nothing to do" and skipped seeding for ever,
    /// so a screen added in a release never appeared and a screen that MOVED kept serving its old route.
    ///
    /// Compare what is actually there against what the application declares instead -- both the keys and
    /// the fields the application owns, since a rename or a moved route has to reach people too. The
    /// projection costs one query over small columns, on a path that loads every screen a line later
    /// anyway.
    ///
    /// <para>
    /// <b>One seeder per company at a time.</b> This used to seed inside whichever request found the catalogue out of
    /// date, and the first page after signing in sends two such requests at once -- the side menu and the Home quick
    /// links each ask for the navigation. On a new company, or on any company after a deployment added a screen, both
    /// requests inserted the same rows and the second failed on IX_Screens_TenantId_Code: an "internal error" dialog
    /// on a new customer's very first screen, gone on refresh because by then the first request had finished.
    /// </para>
    ///
    /// <para>
    /// Now the seeding runs under a lock held per company, in its own unit of work that commits before the lock is
    /// released. A request that waited re-checks, finds the finished catalogue, and seeds nothing. The lock is per
    /// process, so a second server could still race; for that the unique index stays the backstop, and losing that
    /// race is treated as the success it is -- the other server wrote the same rows.
    /// </para>
    /// </summary>
    private async Task EnsureCatalogSeededAsync()
    {
        // The common case, and lock-free: the catalogue is already current.
        if (await IsCatalogueCurrentAsync())
        {
            return;
        }

        var gate = SeedGates.GetOrAdd(AbpSession.TenantId ?? HostGateKey, _ => new SemaphoreSlim(1, 1));

        await gate.WaitAsync();

        try
        {
            using var uow = UnitOfWorkManager.Begin(TransactionScopeOption.RequiresNew);

            using (UnitOfWorkManager.Current.SetTenantId(AbpSession.TenantId))
            {
                // Checked again inside the lock, on this unit of work's own fresh context: a request that waited
                // for another to finish seeding finds it done here.
                if (!await IsCatalogueCurrentAsync())
                {
                    _screenSeeder.SeedForTenant(AbpSession.TenantId);
                }

                try
                {
                    // Committed here, before the lock is released and before the caller reads the catalogue back,
                    // so neither a waiting request nor this one's own read can see a half-written list.
                    await uow.CompleteAsync();
                }
                catch (DbUpdateException ex)
                {
                    // Another server seeded the same rows at the same moment. Its rows are the ones wanted.
                    Logger.Warn("Screen catalogue seeding lost a race for tenant " + AbpSession.TenantId
                                + "; another writer seeded it first.", ex);
                }
            }
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>
    /// Whether every screen the application declares exists with the fields the application owns. A rename or a moved
    /// route has to reach people too, so the keys alone are not enough.
    /// </summary>
    private async Task<bool> IsCatalogueCurrentAsync()
    {
        var existing = await _screenRepository.GetAll()
            .Select(s => new { s.ScreenKey, s.Module, s.NameKey, s.Route, s.PermissionName })
            .ToListAsync();

        var existingByKey = existing
            .GroupBy(s => s.ScreenKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        return ScreenCatalog.All.All(seed =>
            existingByKey.TryGetValue(seed.Key, out var row)
            && row.Module == seed.Module
            && row.NameKey == seed.NameKey
            && row.Route == seed.Route
            && row.PermissionName == seed.PermissionName);
    }

    /// <summary>One gate per company, shared by every request in this process. The host uses <see cref="HostGateKey"/>.</summary>
    private static readonly ConcurrentDictionary<int, SemaphoreSlim> SeedGates = new();

    private const int HostGateKey = 0;

    /// <summary>
    /// Drops screens the caller has no permission for.
    ///
    /// This is not decoration. A shared quick link to a screen someone cannot open would be a dead end,
    /// and worse, it would advertise that the screen exists -- "Termination Processing" appearing on an
    /// ordinary user's dashboard tells them something they were not meant to know.
    /// </summary>
    private async Task<List<Screen>> FilterToPermittedAsync(List<Screen> screens)
    {
        var result = new List<Screen>(screens.Count);

        // Distinct permissions first: 545 screens share far fewer permissions, and each check is a call
        // into the permission manager.
        var permissions = screens
            .Select(s => s.PermissionName)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var granted = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        foreach (var permission in permissions)
        {
            // A screen whose permission no longer exists is HIDDEN, not an error.
            //
            // The seeder is additive by design -- a screen removed from the application keeps its row so
            // that favourites and shared links pointing at it do not silently vanish. That leaves rows
            // naming permissions the code no longer defines, and IsGrantedAsync THROWS on an unknown
            // name rather than returning false. One such row therefore failed the entire navigation
            // bootstrap, so removing a single feature took the whole menu down with it.
            //
            // Not granted is the right answer anyway: nobody can hold a permission that does not exist.
            if (_permissionManager.GetPermissionOrNull(permission) == null)
            {
                granted[permission] = false;
                continue;
            }

            granted[permission] = await PermissionChecker.IsGrantedAsync(permission);
        }

        foreach (var screen in screens)
        {
            // No permission recorded means the screen is open to any signed-in user -- the dashboard, for
            // instance -- not that it is open to nobody.
            if (string.IsNullOrWhiteSpace(screen.PermissionName) ||
                (granted.TryGetValue(screen.PermissionName, out var isGranted) && isGranted))
            {
                result.Add(screen);
            }
        }

        return result;
    }

    private static ScreenDto MapScreen(Screen s) => new ScreenDto
    {
        Id = s.Id,
        ScreenKey = s.ScreenKey,
        Code = s.Code,
        Module = s.Module,
        NameKey = s.NameKey,
        Route = s.Route,
        PermissionName = s.PermissionName,
    };

    private static ScreenUsageDto MapUsage(UserScreenUsage u) => new ScreenUsageDto
    {
        ScreenKey = u.ScreenKey,
        IsFavorite = u.IsFavorite,
        LastVisitedAt = u.LastVisitedAt,
        VisitCount = u.VisitCount,
    };
}
