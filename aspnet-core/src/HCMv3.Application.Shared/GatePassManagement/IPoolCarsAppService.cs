using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GatePassManagement.Dto;

namespace HCMv3.GatePassManagement;

public interface IPoolCarsAppService : IApplicationService
{
    Task<PagedResultDto<PoolCarDto>> GetAll(GetAllPoolCarsInput input);

    Task<GetPoolCarForEditOutput> GetPoolCarForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditPoolCarDto input);

    Task Delete(EntityDto input);
}
