using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.TimeManagement.Dto;

namespace HCMv3.TimeManagement;

public interface IRosterGroupsAppService : IApplicationService
{
    Task<PagedResultDto<RosterGroupDto>> GetAll(GetAllRosterGroupsInput input);

    Task<GetRosterGroupForEditOutput> GetRosterGroupForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditRosterGroupDto input);

    Task Delete(EntityDto input);
}
