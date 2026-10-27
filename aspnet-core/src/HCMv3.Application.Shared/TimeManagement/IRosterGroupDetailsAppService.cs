using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.TimeManagement.Dto;

namespace HCMv3.TimeManagement;

public interface IRosterGroupDetailsAppService : IApplicationService
{
    Task<PagedResultDto<RosterGroupDetailDto>> GetAll(GetAllRosterGroupDetailsInput input);

    Task<GetRosterGroupDetailForEditOutput> GetRosterGroupDetailForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditRosterGroupDetailDto input);

    Task Delete(EntityDto input);
}
