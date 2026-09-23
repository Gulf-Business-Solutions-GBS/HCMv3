using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.SystemSettings.Dto;

namespace HCMv3.SystemSettings;

public interface IManageDocumentSettingsAppService : IApplicationService
{
    Task<PagedResultDto<DocumentSettingsListDto>> GetDocumentSettingsList(PagedAndSortedResultRequestDto input);

    Task<DocumentSettingsEditDto> GetDocumentSettingsForEdit(EntityDto input);

    /// <summary>
    /// The storage locations the server approves, for the picker on a settings row that does not exist yet.
    /// An existing row carries them on its edit DTO.
    /// </summary>
    Task<List<FileStorageRootDto>> GetFileStorageRoots();

    Task CreateOrEditDocumentSettings(DocumentSettingsEditDto input);

    Task DeleteDocumentSettings(EntityDto input);
}
