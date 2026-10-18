using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GovtRelationsManagement.Dto;

namespace HCMv3.GovtRelationsManagement;

/// <summary>
/// Legacy "Org Policy": the organisation documents whose document type is marked Show To All, read-only,
/// for everyone given the Org Policy permission rather than the documents' own maintenance permission.
/// </summary>
public interface IOrgPoliciesAppService : IApplicationService
{
    Task<PagedResultDto<OrganizationDocumentDto>> GetAll(GetAllOrganizationDocumentsInput input);
}
