using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo.Dto;

namespace HCMv3.GeneralMasterInfo;

public interface IDocumentPrintFormsAppService : IApplicationService
{
    Task<PagedResultDto<DocumentPrintFormDto>> GetAll(GetAllDocumentPrintFormsInput input);

    Task<GetDocumentPrintFormForEditOutput> GetDocumentPrintFormForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditDocumentPrintFormDto input);

    Task Delete(EntityDto input);
}
