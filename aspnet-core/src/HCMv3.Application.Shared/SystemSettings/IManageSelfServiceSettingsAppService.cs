using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.SystemSettings.Dto;

namespace HCMv3.SystemSettings;

public interface IManageSelfServiceSettingsAppService : IApplicationService
{
    Task<PagedResultDto<SelfServiceSettingsListDto>> GetSelfServiceSettingsList(PagedAndSortedResultRequestDto input);

    Task<SelfServiceSettingsEditDto> GetSelfServiceSettingsForEdit(EntityDto input);

    Task CreateOrEditSelfServiceSettings(SelfServiceSettingsEditDto input);

    Task DeleteSelfServiceSettings(EntityDto input);
}
