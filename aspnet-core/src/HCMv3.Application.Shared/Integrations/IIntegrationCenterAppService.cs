using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Integrations.Dto;

namespace HCMv3.Integrations;

/// <summary>
/// The Integration Center: every third-party connection in one place, whatever storage each one actually
/// uses.
/// </summary>
public interface IIntegrationCenterAppService : IApplicationService
{
    /// <summary>Every registered integration, as cards.</summary>
    Task<ListResultDto<IntegrationTileDto>> GetIntegrations();

    /// <summary>One integration with its configuration form and recent attempts.</summary>
    Task<IntegrationDetailDto> GetIntegrationDetail(string key);

    Task SaveSettings(SaveIntegrationSettingsInput input);

    /// <summary>
    /// Switches an integration on or off. Enabling a member of an exclusive group switches its siblings
    /// off in the same transaction.
    /// </summary>
    Task SetEnabled(SetIntegrationEnabledInput input);

    /// <summary>Reaches the external system for real and records the outcome.</summary>
    Task<IntegrationTestResultDto> TestConnection(string key);
}
