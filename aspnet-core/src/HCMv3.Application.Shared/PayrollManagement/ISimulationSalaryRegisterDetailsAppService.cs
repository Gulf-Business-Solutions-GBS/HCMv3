using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface ISimulationSalaryRegisterDetailsAppService : IApplicationService
{
    Task<PagedResultDto<SimulationSalaryRegisterDetailDto>> GetAll(GetAllSimulationSalaryRegisterDetailsInput input);

    Task<GetSimulationSalaryRegisterDetailForEditOutput> GetSimulationSalaryRegisterDetailForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditSimulationSalaryRegisterDetailDto input);

    Task Delete(EntityDto input);
}
