using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.SystemSettings.Dto;

namespace HCMv3.SystemSettings;

public interface IManageOrgSettingsAppService : IApplicationService
{
    Task<PagedResultDto<OrgSettingsListDto>> GetOrgSettingsList(PagedAndSortedResultRequestDto input);

    Task<OrgSettingsEditDto> GetOrgSettingsForEdit(EntityDto input);

    Task CreateOrEditOrgSettings(OrgSettingsEditDto input);

    Task DeleteOrgSettings(EntityDto input);
}
