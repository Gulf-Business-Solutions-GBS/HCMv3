using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PerformanceManagement.Dto;

namespace HCMv3.PerformanceManagement;

public interface IPACyclesAppService : IApplicationService
{
    Task<PagedResultDto<PACycleDto>> GetAll(GetAllPACyclesInput input);

    Task<GetPACycleForEditOutput> GetPACycleForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditPACycleDto input);

    Task Delete(EntityDto input);
}
