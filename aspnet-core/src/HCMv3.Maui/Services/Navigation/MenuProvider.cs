using Abp.Dependency;
using Abp.Extensions;
using HCMv3.ApiClient;
using HCMv3.Authorization;
using HCMv3.Maui.Core.Localization;
using HCMv3.Maui.Models.NavigationMenu;

namespace HCMv3.Maui.Services.Navigation;

public class MenuProvider : ISingletonDependency, IMenuProvider
{
    private readonly IApplicationContext _context;

    /* For more icons:
        https://fontawesome.com/icons
    */
    private List<NavigationMenuItem> _menuItems;

    public MenuProvider(IApplicationContext context)
    {
        _context = context;
    }

    // Employee self-service only: the app deliberately has no admin screens. Each item carries the same
    // permission as the web ESS menu, and the server enforces it again on every call.
    public void InitializeMenuItems()
    {
        _menuItems = new List<NavigationMenuItem>
        {
            new NavigationMenuItem
            {
                Title = L.Localize("EmployeeSelfService"),
                Icon = "fa-solid fa-house",
                NavigationUrl = NavigationUrlConsts.EssHome,
                RequiredPermissionName = AppPermissions.Ess,
            },
            new NavigationMenuItem
            {
                Title = L.Localize("EssMyLeave"),
                Icon = "fa-solid fa-calendar-days",
                NavigationUrl = NavigationUrlConsts.MyLeave,
                RequiredPermissionName = AppPermissions.Ess_MyLeave,
            },
            new NavigationMenuItem
            {
                Title = L.Localize("EssGeoPunch"),
                Icon = "fa-solid fa-location-dot",
                NavigationUrl = NavigationUrlConsts.GeoPunch,
                RequiredPermissionName = AppPermissions.Ess_GeoPunch,
            },
            new NavigationMenuItem
            {
                Title = L.Localize("EssMyExitPermits"),
                Icon = "fa-solid fa-plane-departure",
                NavigationUrl = NavigationUrlConsts.ExitPermit,
                RequiredPermissionName = AppPermissions.Ess_MyExitPermits,
            },
            new NavigationMenuItem
            {
                Title = L.Localize("EssInbox"),
                Icon = "fa-solid fa-inbox",
                NavigationUrl = NavigationUrlConsts.Inbox,
                RequiredPermissionName = AppPermissions.Ess_Inbox,
            },
            new NavigationMenuItem
            {
                Title = L.Localize("MySettings"),
                Icon = "fa-solid fa-cog",
                NavigationUrl = NavigationUrlConsts.Settings
            },
        };

        InsertQrLoginMenuItemBeforeSettings();
    }

    public List<NavigationMenuItem> GetAuthorizedMenuItems(Dictionary<string, string> grantedPermissions)
    {
        InitializeMenuItems();
        return FilterAuthorizedMenuItems(_menuItems, grantedPermissions);
    }

    private List<NavigationMenuItem> FilterAuthorizedMenuItems(List<NavigationMenuItem> menuItems, Dictionary<string, string> grantedPermissions)
    {
        var authorizedMenuItems = new List<NavigationMenuItem>();
        foreach (var menuItem in menuItems)
        {
            var authorizedMenuItem = new NavigationMenuItem()
            {
                Title = menuItem.Title,
                Icon = menuItem.Icon,
                IsSelected = menuItem.IsSelected,
                NavigationParameter = menuItem.NavigationParameter,
                NavigationUrl = menuItem.NavigationUrl,
                RequiredPermissionName = menuItem.RequiredPermissionName
            };

            if (menuItem.Items.Any())
            {
                var authorizedSubMenuItems = FilterAuthorizedMenuItems(menuItem.Items, grantedPermissions);
                if (authorizedMenuItem.NavigationUrl.IsNullOrEmpty() && !authorizedSubMenuItems.Any())
                {
                    continue;
                }

                authorizedMenuItem.Items.AddRange(authorizedSubMenuItems);
            }

            if (menuItem.RequiredPermissionName == null)
            {
                authorizedMenuItems.Add(authorizedMenuItem);
                continue;
            }

            if (grantedPermissions != null &&
                grantedPermissions.ContainsKey(menuItem.RequiredPermissionName))
            {
                authorizedMenuItems.Add(authorizedMenuItem);
            }
        }

        return authorizedMenuItems;
    }

    private void InsertQrLoginMenuItemBeforeSettings()
    {
        if (!_context.LoginInfo.Application.IsQrLoginEnabled)
        {
            return;
        }

        var qrLoginMenuItem = new NavigationMenuItem
        {
            Title = L.Localize("QrLogin"),
            Icon = "fa-solid fa-qrcode",
            NavigationUrl = NavigationUrlConsts.QrLogin,
        };

        var settingsMenuItemIndex = _menuItems.FindIndex(item => item.NavigationUrl == NavigationUrlConsts.Settings);

        if (settingsMenuItemIndex >= 0)
        {
            _menuItems.Insert(settingsMenuItemIndex, qrLoginMenuItem);
        }
        else
        {
            _menuItems.Add(qrLoginMenuItem);
        }
    }
}
