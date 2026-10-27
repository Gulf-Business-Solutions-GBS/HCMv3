using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GatePassManagement.Dto;

namespace HCMv3.GatePassManagement;

public interface IGatePassMaterialsAppService : IApplicationService
{
    Task<PagedResultDto<GatePassMaterialDto>> GetAll(GetAllGatePassMaterialsInput input);

    Task<GetGatePassMaterialForEditOutput> GetGatePassMaterialForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditGatePassMaterialDto input);

    Task Delete(EntityDto input);
}
