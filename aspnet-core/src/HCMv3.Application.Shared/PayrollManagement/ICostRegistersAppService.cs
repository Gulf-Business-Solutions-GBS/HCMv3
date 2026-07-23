using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface ICostRegistersAppService : IApplicationService
{
    Task<PagedResultDto<CostRegisterDto>> GetAll(GetAllCostRegistersInput input);

    Task<GetCostRegisterForEditOutput> GetCostRegisterForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditCostRegisterDto input);

    Task Delete(EntityDto input);
}
