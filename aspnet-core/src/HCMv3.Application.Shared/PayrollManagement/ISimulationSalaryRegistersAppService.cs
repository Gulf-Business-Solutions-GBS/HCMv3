using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface ISimulationSalaryRegistersAppService : IApplicationService
{
    Task<PagedResultDto<SimulationSalaryRegisterDto>> GetAll(GetAllSimulationSalaryRegistersInput input);

    Task<GetSimulationSalaryRegisterForEditOutput> GetSimulationSalaryRegisterForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditSimulationSalaryRegisterDto input);

    Task Delete(EntityDto input);
}
