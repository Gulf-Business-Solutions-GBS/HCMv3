using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.Navigation.Dto;

namespace HCMv3.Navigation;

/// <summary>
/// The screen catalogue, the caller's shortcuts, and the tenant's shared quick links.
/// </summary>
public interface IScreensAppService : IApplicationService
{
    Task<NavigationBootstrapDto> GetNavigationBootstrap();

    Task<Abp.Application.Services.Dto.PagedResultDto<ScreenDto>> GetAllScreens(GetAllScreensInput input);

    Task<System.Collections.Generic.List<string>> GetScreenModules();

    Task<System.Collections.Generic.List<SharedQuickLinkDto>> GetSharedQuickLinks();

    Task UpdateScreenCode(UpdateScreenCodeInput input);

    Task CreateOrEditSharedQuickLink(CreateOrEditSharedQuickLinkInput input);

    Task DeleteSharedQuickLink(int id);
}
