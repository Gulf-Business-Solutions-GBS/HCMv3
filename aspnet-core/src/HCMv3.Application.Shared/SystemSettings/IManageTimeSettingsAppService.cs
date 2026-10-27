using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.SystemSettings.Dto;

namespace HCMv3.SystemSettings;

public interface IManageTimeSettingsAppService : IApplicationService
{
    Task<PagedResultDto<TimeSettingsListDto>> GetTimeSettingsList(PagedAndSortedResultRequestDto input);

    Task<TimeSettingsEditDto> GetTimeSettingsForEdit(EntityDto input);

    Task CreateOrEditTimeSettings(TimeSettingsEditDto input);

    Task DeleteTimeSettings(EntityDto input);
}
