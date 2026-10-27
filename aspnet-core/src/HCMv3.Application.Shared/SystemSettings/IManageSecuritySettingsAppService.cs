using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.SystemSettings.Dto;

namespace HCMv3.SystemSettings;

public interface IManageSecuritySettingsAppService : IApplicationService
{
    Task<PagedResultDto<ManageSecuritySettingsListDto>> GetSecuritySettingsList(PagedAndSortedResultRequestDto input);

    Task<ManageSecuritySettingsEditDto> GetSecuritySettingsForEdit(EntityDto input);

    Task CreateOrEditSecuritySettings(ManageSecuritySettingsEditDto input);

    Task DeleteSecuritySettings(EntityDto input);
}
