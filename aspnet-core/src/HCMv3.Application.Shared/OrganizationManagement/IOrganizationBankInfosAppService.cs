using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.OrganizationManagement.Dto;

namespace HCMv3.OrganizationManagement;

public interface IOrganizationBankInfosAppService : IApplicationService
{
    Task<PagedResultDto<OrganizationBankInfoDto>> GetAll(GetAllOrganizationBankInfosInput input);

    Task<GetOrganizationBankInfoForEditOutput> GetOrganizationBankInfoForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditOrganizationBankInfoDto input);

    Task Delete(EntityDto input);
}
