using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface ISimulationSalaryRegisterDaysDetailsAppService : IApplicationService
{
    Task<PagedResultDto<SimulationSalaryRegisterDaysDetailDto>> GetAll(GetAllSimulationSalaryRegisterDaysDetailsInput input);

    Task<GetSimulationSalaryRegisterDaysDetailForEditOutput> GetSimulationSalaryRegisterDaysDetailForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditSimulationSalaryRegisterDaysDetailDto input);

    Task Delete(EntityDto input);
}
