using HCMv3.Maui.Models.NavigationMenu;

namespace HCMv3.Maui.Services.Navigation;

public interface IMenuProvider
{
    List<NavigationMenuItem> GetAuthorizedMenuItems(Dictionary<string, string> grantedPermissions);
}