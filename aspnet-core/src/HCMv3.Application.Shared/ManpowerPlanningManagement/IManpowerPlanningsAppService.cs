using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.ManpowerPlanningManagement.Dto;

namespace HCMv3.ManpowerPlanningManagement;

/// <summary>
/// Manpower plan headers.
///
/// <para>
/// NO Approve/Reject. Both were deleted when this type was routed through the workflow engine: approving a
/// plan means approving its <c>WorkflowRequest</c> through <c>IWorkflowApprovalService</c> (the approver
/// inbox), which is also what triggers the creation of the planned positions. A second approval path here
/// would be a second writer of the same decision.
/// </para>
/// </summary>
public interface IManpowerPlanningsAppService : IApplicationService
{
    Task<PagedResultDto<ManpowerPlanningDto>> GetAll(GetAllManpowerPlanningsInput input);

    Task<GetManpowerPlanningForEditOutput> GetManpowerPlanningForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditManpowerPlanningDto input);

    Task Delete(EntityDto input);
}
