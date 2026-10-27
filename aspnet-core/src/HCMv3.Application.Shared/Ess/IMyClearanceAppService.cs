using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Ess.Dto;

namespace HCMv3.Ess;

/// <summary>
/// Self-service exit clearance: the checklist IT, Finance, HR and whoever else sign off before somebody
/// leaves.
///
/// <para>
/// The interesting half of this is not the request -- that is the same create-and-submit every other ESS
/// type does -- but the two checklist methods, which are what an approver uses once the request reaches
/// them. Several departments hold the record at the same time because the type is configured with a
/// <c>Parallel</c> step; each of them sees only the lines their <c>WorkflowRole</c> owns. There is no
/// bespoke sign-off mechanism behind any of it: the engine routes, and
/// <c>ClearanceRequestFieldDetail.WorkflowRoleId</c> filters.
/// </para>
/// </summary>
public interface IMyClearanceAppService : IApplicationService
{
    Task<PagedResultDto<MyClearanceDto>> GetMyClearances(GetMyRequestsInput input);

    /// <summary>The clearance types an employee may actually raise -- those flagged
    /// <c>IncludeInWorkflow</c>.</summary>
    Task<List<ClearanceTypeLookupDto>> GetClearanceTypes();

    /// <summary>Creates the clearance and, when the type is workflow-enabled, submits it in one step.</summary>
    Task<MyRequestSubmissionDto> RequestClearance(RequestClearanceInput input);

    /// <summary>Withdraws a clearance while it is still theirs to withdraw.</summary>
    Task CancelMyClearance(EntityDto input);

    /// <summary>
    /// The signed-in approver's part of a clearance form: their department's lines, merged with any answers
    /// already recorded against them.
    /// </summary>
    Task<ClearanceChecklistDto> GetClearanceChecklist(EntityDto input);

    /// <summary>
    /// Records the signed-in approver's answers. Refused unless the clearance is open at a <c>Parallel</c>
    /// step, and refused for any line their roles do not own.
    /// </summary>
    Task SaveClearanceChecklist(SaveClearanceChecklistInput input);
}
