using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.OrganizationManagement.Dto;

namespace HCMv3.OrganizationManagement;

public interface IOrganizationUnitTypesAppService : IApplicationService
{
    Task<PagedResultDto<OrganizationUnitTypeDto>> GetAll(GetAllOrganizationUnitTypesInput input);

    Task<GetOrganizationUnitTypeForEditOutput> GetOrganizationUnitTypeForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditOrganizationUnitTypeDto input);

    Task Delete(EntityDto input);
}
