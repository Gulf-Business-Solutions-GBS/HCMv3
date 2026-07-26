using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.SystemSettings.Dto;

namespace HCMv3.SystemSettings;

public interface IManageAppraisalSettingsAppService : IApplicationService
{
    Task<PagedResultDto<AppraisalSettingsListDto>> GetAppraisalSettingsList(PagedAndSortedResultRequestDto input);

    Task<AppraisalSettingsEditDto> GetAppraisalSettingsForEdit(EntityDto input);

    Task CreateOrEditAppraisalSettings(AppraisalSettingsEditDto input);

    Task DeleteAppraisalSettings(EntityDto input);
}
