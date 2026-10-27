using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GovtRelationsManagement.Dto;

namespace HCMv3.GovtRelationsManagement;

public interface IOrganizationDocumentsAppService : IApplicationService
{
    Task<PagedResultDto<OrganizationDocumentDto>> GetAll(GetAllOrganizationDocumentsInput input);

    Task<GetOrganizationDocumentForEditOutput> GetOrganizationDocumentForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditOrganizationDocumentDto input);

    Task Delete(EntityDto input);
}
