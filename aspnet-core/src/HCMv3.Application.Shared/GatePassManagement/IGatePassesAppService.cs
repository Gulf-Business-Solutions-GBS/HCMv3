using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GatePassManagement.Dto;

namespace HCMv3.GatePassManagement;

public interface IGatePassesAppService : IApplicationService
{
    Task<PagedResultDto<GatePassDto>> GetAll(GetAllGatePassesInput input);

    Task<GetGatePassForEditOutput> GetGatePassForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditGatePassDto input);

    Task Delete(EntityDto input);
}
