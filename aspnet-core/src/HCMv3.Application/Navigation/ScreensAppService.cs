using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

    public ScreensAppService(
        IRepository<Screen> screenRepository,
        IRepository<SharedQuickLink> sharedQuickLinkRepository,
        IRepository<UserScreenUsage> usageRepository,
        IScreenSeeder screenSeeder)
    {
        _screenRepository = screenRepository;
        _sharedQuickLinkRepository = sharedQuickLinkRepository;
        _usageRepository = usageRepository;
        _screenSeeder = screenSeeder;
    }

    public async Task<NavigationBootstrapDto> GetNavigationBootstrap()
    {
        var userId = AbpSession.UserId ?? throw new UserFriendlyException(L("YouAreNotLoggedIn"));

        await EnsureCatalogSeededAsync();

        var allScreens = await _screenRepository.GetAll().ToListAsync();
        var visible = await FilterToPermittedAsync(allScreens);
        var visibleByKey = visible.ToDictionary(s => s.ScreenKey, StringComparer.OrdinalIgnoreCase);

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
    /// The count check keeps the common path to one cheap query -- the seeder is only entered when the
    /// tenant is genuinely short of screens, which is once per tenant per release at most. Duplicates
    /// are impossible regardless: (TenantId, ScreenKey) is unique in the database.
    /// </summary>
    private async Task EnsureCatalogSeededAsync()
    {
        var existing = await _screenRepository.CountAsync();
        if (existing >= ScreenCatalog.All.Count)
        {
            return;
        }

        _screenSeeder.SeedForTenant(AbpSession.TenantId);

        // Flush before reading them back. ABP saves at the END of the unit of work, so without this the
        // GetAll() immediately below queries the database and sees none of what was just inserted -- the
        // first caller after a deployment would get an empty catalogue and the second would get a full
        // one, which is the kind of bug that never reproduces when you go looking for it.
        await CurrentUnitOfWork.SaveChangesAsync();
    }

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
