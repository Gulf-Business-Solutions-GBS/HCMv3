using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GatePassManagement.Dto;

namespace HCMv3.GatePassManagement;

public interface IGatePassVehiclesAppService : IApplicationService
{
    Task<PagedResultDto<GatePassVehicleDto>> GetAll(GetAllGatePassVehiclesInput input);

    Task<GetGatePassVehicleForEditOutput> GetGatePassVehicleForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditGatePassVehicleDto input);

    Task Delete(EntityDto input);
}
