using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.Configuration.Tenants.Dto;

namespace HCMv3.Configuration.Tenants;

public interface ITenantSettingsAppService : IApplicationService
{
    Task<TenantSettingsEditDto> GetAllSettings();

    Task UpdateAllSettings(TenantSettingsEditDto input);

    Task ClearDarkLogo();

    Task ClearDarkLogoMinimal();

    Task ClearLightLogo();

    Task ClearLightLogoMinimal();

    Task ClearCustomCss();
}

