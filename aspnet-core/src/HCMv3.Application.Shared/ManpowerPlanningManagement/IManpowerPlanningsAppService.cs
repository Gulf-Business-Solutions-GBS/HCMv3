using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.ManpowerPlanningManagement.Dto;

namespace HCMv3.ManpowerPlanningManagement;

public interface IManpowerPlanningsAppService : IApplicationService
{
    Task<PagedResultDto<ManpowerPlanningDto>> GetAll(GetAllManpowerPlanningsInput input);

    Task<GetManpowerPlanningForEditOutput> GetManpowerPlanningForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditManpowerPlanningDto input);

    Task Delete(EntityDto input);

    /// <summary>
    /// Legacy "Entry On Final Approval" parity: marks the request Approved and creates a
    /// <see cref="Position"/> for every detail line that doesn't have one yet.
    /// </summary>
    Task Approve(ApproveManpowerPlanningDto input);

    Task Reject(ApproveManpowerPlanningDto input);
}
