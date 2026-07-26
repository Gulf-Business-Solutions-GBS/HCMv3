using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.SystemSettings.Dto;

namespace HCMv3.SystemSettings;

public interface IManageGeneralSettingsAppService : IApplicationService
{
    Task<PagedResultDto<ManageGeneralSettingsListDto>> GetGeneralSettingsList(PagedAndSortedResultRequestDto input);

    Task<ManageGeneralSettingsEditDto> GetGeneralSettingsForEdit(EntityDto input);

    Task CreateOrEditGeneralSettings(ManageGeneralSettingsEditDto input);

    Task DeleteGeneralSettings(EntityDto input);
}
