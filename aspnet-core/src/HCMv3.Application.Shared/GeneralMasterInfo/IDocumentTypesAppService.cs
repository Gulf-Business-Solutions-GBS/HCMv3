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
}
