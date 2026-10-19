using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// HR's list of leave plans (legacy back-office <c>LeavePlanner_ListView</c>), also shown on the employee record.
///
/// <para>
/// Read and remove only. A plan is raised by the employee for themselves; legacy's back-office list existed to
/// look at plans and act on them through the workflow, which HCMv3 does from the approver's inbox.
/// </para>
/// </summary>
public interface ILeavePlansAppService : IApplicationService
{
    Task<PagedResultDto<LeavePlanDto>> GetAll(GetAllLeavePlansInput input);

    /// <summary>Removes the plan and closes any request still open on it, so no approver is left holding it.</summary>
    Task Delete(EntityDto input);
}
