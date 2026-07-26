using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.SystemSettings.Dto;

namespace HCMv3.SystemSettings;

public interface IManageDocumentSettingsAppService : IApplicationService
{
    Task<PagedResultDto<DocumentSettingsListDto>> GetDocumentSettingsList(PagedAndSortedResultRequestDto input);

    Task<DocumentSettingsEditDto> GetDocumentSettingsForEdit(EntityDto input);

    Task CreateOrEditDocumentSettings(DocumentSettingsEditDto input);

    Task DeleteDocumentSettings(EntityDto input);
}
