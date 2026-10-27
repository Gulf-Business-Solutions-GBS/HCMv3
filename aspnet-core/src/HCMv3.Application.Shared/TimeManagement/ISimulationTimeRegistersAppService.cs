using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.TimeManagement.Dto;

namespace HCMv3.TimeManagement;

public interface ISimulationTimeRegistersAppService : IApplicationService
{
    Task<PagedResultDto<SimulationTimeRegisterDto>> GetAll(GetAllSimulationTimeRegistersInput input);

    Task<GetSimulationTimeRegisterForEditOutput> GetSimulationTimeRegisterForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditSimulationTimeRegisterDto input);

    Task Delete(EntityDto input);
}
