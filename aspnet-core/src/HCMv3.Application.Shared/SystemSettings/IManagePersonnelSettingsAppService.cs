using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.SystemSettings.Dto;

namespace HCMv3.SystemSettings;

public interface IManagePersonnelSettingsAppService : IApplicationService
{
    Task<PagedResultDto<PersonnelSettingsListDto>> GetPersonnelSettingsList(PagedAndSortedResultRequestDto input);

    Task<PersonnelSettingsEditDto> GetPersonnelSettingsForEdit(EntityDto input);

    Task CreateOrEditPersonnelSettings(PersonnelSettingsEditDto input);

    Task DeletePersonnelSettings(EntityDto input);
}
