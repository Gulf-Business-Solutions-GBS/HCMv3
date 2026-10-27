using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.OrganizationManagement.Dto;

namespace HCMv3.OrganizationManagement;

public interface ICostElementsAppService : IApplicationService
{
    Task<PagedResultDto<CostElementDto>> GetAll(GetAllCostElementsInput input);

    Task<GetCostElementForEditOutput> GetCostElementForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditCostElementDto input);

    Task Delete(EntityDto input);
}
