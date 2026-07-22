using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.OrganizationManagement.Dto;

namespace HCMv3.OrganizationManagement;

public interface ICostCentersAppService : IApplicationService
{
    Task<PagedResultDto<CostCenterDto>> GetAll(GetAllCostCentersInput input);

    Task<GetCostCenterForEditOutput> GetCostCenterForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditCostCenterDto input);

    Task Delete(EntityDto input);
}
