using System.Threading.Tasks;
using Abp;
using Abp.Dependency;
using HCMv3.Configuration.Dto;
using HCMv3.UiCustomization.Dto;

namespace HCMv3.UiCustomization;

public interface IUiCustomizer : ISingletonDependency
{
    Task<UiCustomizationSettingsDto> GetUiSettings();

    Task UpdateUserUiManagementSettingsAsync(UserIdentifier user, ThemeSettingsDto settings);

    Task UpdateTenantUiManagementSettingsAsync(int tenantId, ThemeSettingsDto settings, UserIdentifier changerUser);

    Task UpdateApplicationUiManagementSettingsAsync(ThemeSettingsDto settings, UserIdentifier changerUser);

    Task<ThemeSettingsDto> GetHostUiManagementSettings();

    Task<ThemeSettingsDto> GetTenantUiCustomizationSettings(int tenantId);

    Task UpdateDarkModeSettingsAsync(UserIdentifier user, bool isDarkModeEnabled);

    Task<string> GetBodyClass();

    Task<string> GetBodyStyle();
}
