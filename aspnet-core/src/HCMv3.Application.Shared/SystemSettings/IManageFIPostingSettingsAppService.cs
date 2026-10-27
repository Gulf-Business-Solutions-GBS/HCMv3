using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.SystemSettings.Dto;

namespace HCMv3.SystemSettings;

public interface IManageFIPostingSettingsAppService : IApplicationService
{
    Task<PagedResultDto<FIPostingSettingsListDto>> GetFIPostingSettingsList(PagedAndSortedResultRequestDto input);

    Task<FIPostingSettingsEditDto> GetFIPostingSettingsForEdit(EntityDto input);

    Task CreateOrEditFIPostingSettings(FIPostingSettingsEditDto input);

    Task DeleteFIPostingSettings(EntityDto input);
}
