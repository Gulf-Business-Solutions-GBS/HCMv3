using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.ManpowerPlanningManagement.Dto;

namespace HCMv3.ManpowerPlanningManagement;

public interface IPositionsAppService : IApplicationService
{
    Task<PagedResultDto<PositionDto>> GetAll(GetAllPositionsInput input);

    Task<GetPositionForEditOutput> GetPositionForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditPositionDto input);

    Task Delete(EntityDto input);
}
