using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.SystemSettings.Dto;

namespace HCMv3.SystemSettings;

/// <summary>
/// Read-only access, for every signed-in user, to the tenant settings that only shape screens. The
/// <c>Manage*Settings</c> screens themselves stay behind their own permissions.
/// </summary>
public interface ITenantUiSettingsAppService : IApplicationService
{
    Task<TenantUiSettingsDto> GetTenantUiSettings();
}
