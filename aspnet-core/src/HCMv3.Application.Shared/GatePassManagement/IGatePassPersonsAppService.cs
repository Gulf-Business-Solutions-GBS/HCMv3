using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GatePassManagement.Dto;

namespace HCMv3.GatePassManagement;

public interface IGatePassPersonsAppService : IApplicationService
{
    Task<PagedResultDto<GatePassPersonDto>> GetAll(GetAllGatePassPersonsInput input);

    Task<GetGatePassPersonForEditOutput> GetGatePassPersonForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditGatePassPersonDto input);

    Task Delete(EntityDto input);
}
