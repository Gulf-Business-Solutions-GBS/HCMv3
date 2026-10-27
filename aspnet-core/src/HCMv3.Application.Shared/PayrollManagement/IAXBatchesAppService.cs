using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface IAXBatchesAppService : IApplicationService
{
    Task<PagedResultDto<AXBatchDto>> GetAll(GetAllAXBatchesInput input);

    Task<GetAXBatchForEditOutput> GetAXBatchForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditAXBatchDto input);

    Task Delete(EntityDto input);
}
