using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.TimeManagement.Dto;

namespace HCMv3.TimeManagement;

public interface ISimulationTimeRegisterDetailsAppService : IApplicationService
{
    Task<PagedResultDto<SimulationTimeRegisterDetailDto>> GetAll(GetAllSimulationTimeRegisterDetailsInput input);

    Task<GetSimulationTimeRegisterDetailForEditOutput> GetSimulationTimeRegisterDetailForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditSimulationTimeRegisterDetailDto input);

    Task Delete(EntityDto input);
}
