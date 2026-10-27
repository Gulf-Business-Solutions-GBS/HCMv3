using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.ManpowerPlanningManagement.Dto;

namespace HCMv3.ManpowerPlanningManagement;

public interface IPositionRelationshipsAppService : IApplicationService
{
    Task<PagedResultDto<PositionRelationshipDto>> GetAll(GetAllPositionRelationshipsInput input);

    Task<GetPositionRelationshipForEditOutput> GetPositionRelationshipForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditPositionRelationshipDto input);

    Task Delete(EntityDto input);
}
