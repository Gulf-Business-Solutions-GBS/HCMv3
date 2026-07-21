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
        https://material.io/icons/
    */
    private List<NavigationMenuItem> _menuItems;

    public MenuProvider(IApplicationContext context)
    {
        _context = context;
    }

    public void InitializeMenuItems()
    {
        _menuItems = new List<NavigationMenuItem>
        {
            GetDashboardMenuItem(),
            new NavigationMenuItem
            {
                Title = L.Localize("Tenants"),
                Icon = "fa-solid fa-list",
                NavigationUrl = NavigationUrlConsts.Tenants,
                RequiredPermissionName = AppPermissions.Pages_Tenants,
            },
            new NavigationMenuItem
            {
                Title = L.Localize("Users"),
                Icon = "fa-solid fa-filter",
                NavigationUrl= NavigationUrlConsts.Users,
                RequiredPermissionName = AppPermissions.Pages_Administration_Users,
            },
            new NavigationMenuItem
            {
                Title = L.Localize("MySettings"),
                Icon = "fa-solid fa-cog",
                NavigationUrl  = NavigationUrlConsts.Settings
            },
            
            /*This is a sample menu item to guide how to add a new item.
                ,new NavigationMenuItem
                {
                    Title = "Sample View",
                    Icon = "MyIcon.png",
                    TargetType = typeof(_SampleView),
                    Order = 10
                }
            */
        };

        InsertQrLoginMenuItemBeforeSettings();
    }

    public List<NavigationMenuItem> GetAuthorizedMenuItems(Dictionary<string, string> grantedPermissions)
    {
        InitializeMenuItems();
        return FilterAuthorizedMenuItems(_menuItems, grantedPermissions);
    }

    private NavigationMenuItem GetDashboardMenuItem()
    {
        var isHost = _context.LoginInfo.Tenant == null;

        var permission = isHost
            ? AppPermissions.Pages_Administration_Host_Dashboard
            : AppPermissions.Pages_Tenant_Dashboard;

        var navigationUrl = isHost
            ? NavigationUrlConsts.HostDashboard
            : NavigationUrlConsts.TenantDashboard;

        return new NavigationMenuItem
        {
            Title = L.Localize("Dashboard"),
            Icon = "fa-solid fa-chart-line",
            NavigationUrl = navigationUrl,
            RequiredPermissionName = permission
        };
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