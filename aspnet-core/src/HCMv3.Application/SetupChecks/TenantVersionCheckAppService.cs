using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Configuration;
using Abp.Localization;
using Abp.MultiTenancy;
using Abp.Runtime.Session;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.Authorization.Roles;
using HCMv3.Dto;
using HCMv3.Navigation;
using HCMv3.SetupChecks.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.SetupChecks;

/// <inheritdoc cref="ITenantVersionCheckAppService"/>
[AbpAuthorize(AppPermissions.Pages_Administration_TenantVersionCheck)]
public class TenantVersionCheckAppService : HCMv3AppServiceBase, ITenantVersionCheckAppService
{
    /// <summary>How many earlier builds the screen offers to compare from.</summary>
    public const int MaxBuildsOffered = 400;

    private const string RolesScreenCode = "SAD014";

    private readonly IReleaseLog _log;
    private readonly ReleaseVersionTracker _versionTracker;
    private readonly IPermissionManager _permissionManager;
    private readonly RoleManager _roleManager;
    private readonly ISettingDefinitionManager _settingDefinitions;
    private readonly IAppliedMigrationsReader _appliedMigrations;
    private readonly ILocalizationContext _localizationContext;
    private readonly TenantVersionCheckExcelExporter _excelExporter;

    public TenantVersionCheckAppService(
        IReleaseLog log,
        ReleaseVersionTracker versionTracker,
        IPermissionManager permissionManager,
        RoleManager roleManager,
        ISettingDefinitionManager settingDefinitions,
        IAppliedMigrationsReader appliedMigrations,
        ILocalizationContext localizationContext,
        TenantVersionCheckExcelExporter excelExporter)
    {
        _log = log;
        _versionTracker = versionTracker;
        _permissionManager = permissionManager;
        _roleManager = roleManager;
        _settingDefinitions = settingDefinitions;
        _appliedMigrations = appliedMigrations;
        _localizationContext = localizationContext;
        _excelExporter = excelExporter;
    }

    public async Task<TenantVersionCheckStatusDto> GetStatus()
    {
        // Always the signed-in tenant: there is no way to ask about another one.
        var state = await _versionTracker.RecordAsync(AbpSession.GetTenantId());
        var previousNumber = _log.NumberOf(state.PreviousVersion);

        return new TenantVersionCheckStatusDto
        {
            CurrentVersion = state.CurrentVersion,
            BuiltOn = _log.BuiltOn,
            PreviousVersion = state.PreviousVersion,
            UpdatedOn = state.UpdatedOn,
            CanComparePrevious = previousNumber.HasValue && previousNumber.Value < _log.Number,
            Builds = _log.Changes
                .Where(c => c.Number < _log.Number)
                .GroupBy(c => c.Number)
                .OrderByDescending(g => g.Key)
                .Take(MaxBuildsOffered)
                .Select(g => g.OrderByDescending(c => c.Date).First())
                .Select(c => new BuildSummaryDto { Version = _log.VersionOf(c.Number), Date = c.Date, Subject = c.Subject })
                .ToList(),
        };
    }

    [HttpPost]
    public async Task<BuildComparisonDto> Compare(CompareBuildsInput input)
    {
        var from = _log.NumberOf(input.FromVersion?.Trim())
                   ?? throw new UserFriendlyException(L("SetupCheckUnknownVersion", input.FromVersion));
        var changes = _log.Changes.Where(c => c.Number > from).ToList();

        return new BuildComparisonDto
        {
            FromVersion = _log.VersionOf(from),
            ToVersion = _log.Version,
            SetupItems = await SetupItemsAsync(changes),
            Changes = changes.Select(c => new BuildChangeDto
            {
                Version = _log.VersionOf(c.Number),
                Date = c.Date,
                Subject = c.Subject,
                ModuleName = ModuleName(c.Module),
                SetupItemCount = c.Setup.Count,
            }).ToList(),
        };
    }

    [HttpPost]
    public async Task<FileDto> ExportToExcel(CompareBuildsInput input)
    {
        return _excelExporter.Export(await Compare(input));
    }

    /// <summary>
    /// The setup the changes ask of this tenant, most urgent first. Each thing is listed once, by the latest change
    /// that touched it, and only if it still exists and concerns tenants.
    /// </summary>
    private async Task<List<SetupItemDto>> SetupItemsAsync(IReadOnlyList<ReleaseChange> changes)
    {
        var latest = new Dictionary<(SetupItemKind, string), ReleaseChange>();
        foreach (var change in changes.OrderBy(c => c.Number))
        {
            foreach (var note in change.Setup)
            {
                latest[(note.Kind, note.Text)] = change;
            }
        }

        var roles = latest.Keys.Any(k => k.Item1 == SetupItemKind.Permission)
            ? await _roleManager.Roles.OrderBy(r => r.DisplayName).ToListAsync()
            : new List<Role>();
        var settings = _settingDefinitions.GetAllSettingDefinitions().ToDictionary(s => s.Name);
        var applied = latest.Keys.Any(k => k.Item1 == SetupItemKind.Database) ? await _appliedMigrations.GetAppliedAsync() : null;

        var items = new List<SetupItemDto>();
        foreach (var ((kind, text), change) in latest)
        {
            var item = kind switch
            {
                SetupItemKind.Permission => await PermissionItemAsync(text, roles),
                SetupItemKind.Screen => ScreenItem(text),
                SetupItemKind.Setting => SettingItem(text, settings),
                SetupItemKind.Database => DatabaseItem(text, applied),
                _ => new SetupItemDto { Status = SetupItemStatus.Review, Title = text, Detail = L("SetupItemManualDetail") },
            };

            if (item == null)
            {
                continue;
            }

            item.Kind = kind;
            item.TechnicalName ??= text;
            item.Version = _log.VersionOf(change.Number);
            item.ChangeSubject = change.Subject;
            items.Add(item);
        }

        return items
            .OrderBy(i => i.Status switch
            {
                SetupItemStatus.ActionRequired => 0,
                SetupItemStatus.Review => 1,
                SetupItemStatus.Info => 2,
                _ => 3,
            })
            .ThenBy(i => i.Kind)
            .ThenBy(i => i.Title)
            .ToList();
    }

    /// <summary>A new permission is held by the administrator only until somebody grants it: that is the setup.</summary>
    private async Task<SetupItemDto> PermissionItemAsync(string name, IReadOnlyList<Role> roles)
    {
        var permission = _permissionManager.GetPermissionOrNull(name);
        if (permission == null || !permission.MultiTenancySides.HasFlag(MultiTenancySides.Tenant))
        {
            return null;
        }

        var granted = new List<Role>();
        foreach (var role in roles)
        {
            if (await _roleManager.IsGrantedAsync(role.Id, permission))
            {
                granted.Add(role);
            }
        }

        var beyondAdmin = granted.Any(r => r.Name != StaticRoleNames.Tenants.Admin);
        var roleNames = string.Join(", ", granted.Select(r => r.DisplayName));
        var rolesScreen = ScreenCatalog.All.FirstOrDefault(s => s.Code == RolesScreenCode);

        return new SetupItemDto
        {
            Status = beyondAdmin ? SetupItemStatus.Done : SetupItemStatus.Review,
            Title = permission.DisplayName?.Localize(_localizationContext) ?? name,
            Detail = granted.Count == 0
                ? L("SetupItemPermissionNobody")
                : beyondAdmin
                    ? L("SetupItemPermissionGrantedTo", roleNames)
                    : L("SetupItemPermissionOnlyAdmin", roleNames),
            FixScreenName = rolesScreen == null ? null : L(rolesScreen.NameKey),
            FixScreenRoute = rolesScreen?.Route,
        };
    }

    private SetupItemDto ScreenItem(string code)
    {
        var screen = ScreenCatalog.All.FirstOrDefault(s => s.Code == code);
        if (screen == null)
        {
            return null;
        }

        // A screen with no permission of its own is open to everybody, tenants included.
        if (!string.IsNullOrEmpty(screen.PermissionName))
        {
            var permission = _permissionManager.GetPermissionOrNull(screen.PermissionName);
            if (permission == null || !permission.MultiTenancySides.HasFlag(MultiTenancySides.Tenant))
            {
                return null;
            }
        }

        return new SetupItemDto
        {
            Status = SetupItemStatus.Info,
            Title = L(screen.NameKey),
            Detail = L("SetupItemScreenDetail"),
            FixScreenName = L(screen.NameKey),
            FixScreenRoute = screen.Route,
        };
    }

    private SetupItemDto SettingItem(string name, IReadOnlyDictionary<string, SettingDefinition> settings)
    {
        if (!settings.TryGetValue(name, out var setting) || !setting.Scopes.HasFlag(SettingScopes.Tenant))
        {
            return null;
        }

        return new SetupItemDto
        {
            Status = SetupItemStatus.Info,
            Title = setting.DisplayName?.Localize(_localizationContext) ?? name,
            Detail = L("SetupItemSettingDetail"),
        };
    }

    /// <summary>A database change the update did not apply is the one setup item that breaks screens outright.</summary>
    private SetupItemDto DatabaseItem(string migrationId, IReadOnlyCollection<string> applied)
    {
        var title = Regex.Replace(migrationId, @"^\d+_", string.Empty).Replace('_', ' ');
        if (applied == null)
        {
            return new SetupItemDto { Status = SetupItemStatus.Info, Title = title, Detail = L("SetupItemDatabaseUnknown") };
        }

        return applied.Contains(migrationId)
            ? new SetupItemDto { Status = SetupItemStatus.Done, Title = title, Detail = L("SetupItemDatabaseApplied") }
            : new SetupItemDto { Status = SetupItemStatus.ActionRequired, Title = title, Detail = L("SetupItemDatabaseNotApplied") };
    }

    /// <summary>The module's name as the menu shows it when there is one; otherwise its folder name spaced out.</summary>
    private string ModuleName(string module)
    {
        if (string.IsNullOrEmpty(module))
        {
            return L("SetupCheckGeneralModule");
        }

        return LocalizationSource.GetStringOrNull(module) ?? Regex.Replace(module, "(?<=[a-z0-9])(?=[A-Z])", " ");
    }
}
