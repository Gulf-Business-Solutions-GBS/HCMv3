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

    Task UpdateScreenCode(UpdateScreenCodeInput input);

    Task CreateOrEditSharedQuickLink(CreateOrEditSharedQuickLinkInput input);

    Task DeleteSharedQuickLink(int id);
}
