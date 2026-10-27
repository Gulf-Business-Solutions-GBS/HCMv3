using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.SystemSettings.Dto;

namespace HCMv3.SystemSettings;

public interface IManageWorkflowSettingsAppService : IApplicationService
{
    Task<PagedResultDto<WorkflowSettingsListDto>> GetWorkflowSettingsList(PagedAndSortedResultRequestDto input);

    Task<WorkflowSettingsEditDto> GetWorkflowSettingsForEdit(EntityDto input);

    Task CreateOrEditWorkflowSettings(WorkflowSettingsEditDto input);

    Task DeleteWorkflowSettings(EntityDto input);
}
