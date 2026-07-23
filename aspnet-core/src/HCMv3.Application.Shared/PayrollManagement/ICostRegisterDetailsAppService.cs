using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface ICostRegisterDetailsAppService : IApplicationService
{
    Task<PagedResultDto<CostRegisterDetailDto>> GetAll(GetAllCostRegisterDetailsInput input);

    Task<GetCostRegisterDetailForEditOutput> GetCostRegisterDetailForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditCostRegisterDetailDto input);

    Task Delete(EntityDto input);
}
