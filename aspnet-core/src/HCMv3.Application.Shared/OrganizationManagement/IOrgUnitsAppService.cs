using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.OrganizationManagement.Dto;

namespace HCMv3.OrganizationManagement;

public interface IOrgUnitsAppService : IApplicationService
{
    Task<PagedResultDto<OrgUnitDto>> GetAll(GetAllOrgUnitsInput input);

    Task<GetOrgUnitForEditOutput> GetOrgUnitForEdit(EntityDto<long> input);

    Task CreateOrEdit(CreateOrEditOrgUnitDto input);

    Task Delete(EntityDto<long> input);
}
