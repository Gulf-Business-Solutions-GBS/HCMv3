using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.SystemSettings.Dto;

namespace HCMv3.SystemSettings;

public interface IManageProjectCostingSettingsAppService : IApplicationService
{
    Task<PagedResultDto<ProjectCostingSettingsListDto>> GetProjectCostingSettingsList(PagedAndSortedResultRequestDto input);

    Task<ProjectCostingSettingsEditDto> GetProjectCostingSettingsForEdit(EntityDto input);

    Task CreateOrEditProjectCostingSettings(ProjectCostingSettingsEditDto input);

    Task DeleteProjectCostingSettings(EntityDto input);
}
