using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GatePassManagement.Dto;

namespace HCMv3.GatePassManagement;

public interface IGatePassVendorsAppService : IApplicationService
{
    Task<PagedResultDto<GatePassVendorDto>> GetAll(GetAllGatePassVendorsInput input);

    Task<GetGatePassVendorForEditOutput> GetGatePassVendorForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditGatePassVendorDto input);

    Task Delete(EntityDto input);
}
