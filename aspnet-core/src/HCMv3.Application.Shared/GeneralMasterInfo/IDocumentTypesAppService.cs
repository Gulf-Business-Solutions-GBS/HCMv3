using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo.Dto;

namespace HCMv3.GeneralMasterInfo;

public interface IDocumentTypesAppService : IApplicationService
{
    Task<PagedResultDto<DocumentTypeDto>> GetAll(GetAllDocumentTypesInput input);

    Task<GetDocumentTypeForEditOutput> GetDocumentTypeForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditDocumentTypeDto input);

    Task Delete(EntityDto input);

    /// <summary>The reports a document type can be given to print as: active, and present in this build.</summary>
    Task<ListResultDto<SimpleLookupDto>> GetPrintReportsForLookup();
}
