using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Ess.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.Ess;

/// <summary>
/// Self-service leave plans (legacy ESS <c>LeavePlannerController</c>): the employee's own plans, planning a new
/// one for approval, and the team scheduler legacy offered beside the list.
///
/// <para>
/// A plan books nothing. It is the employee saying when they mean to be away so their manager can see it
/// coming; the leave itself is still applied for on the leave screen.
/// </para>
/// </summary>
public interface IMyLeavePlanAppService : IApplicationService
{
    Task<PagedResultDto<MyLeavePlanDto>> GetMyLeavePlans(GetMyRequestsInput input);

    /// <summary>The Leave Info figures for the dates on the form: planned days, projected balance, leave days.</summary>
    Task<LeavePlanPreviewDto> PreviewMyLeavePlan(PreviewLeavePlanInput input);

    /// <summary>Creates the plan and, when the type is workflow-enabled, submits it in the same call.</summary>
    Task<MyRequestSubmissionDto> RequestLeavePlan(RequestLeavePlanInput input);

    /// <summary>Files the plan as a draft, or updates a draft or a plan sent back. Nothing is sent.</summary>
    Task<MyRequestSubmissionDto> SaveMyLeavePlanDraft(SaveLeavePlanDraftInput input);

    /// <summary>Reopens one of the employee's own drafts, or a plan sent back to them, in the form.</summary>
    Task<MyLeavePlanDto> GetMyLeavePlanDraftForEdit(EntityDto input);

    /// <summary>Sends a draft into the approval chain, or a sent-back plan back into it under the same request.</summary>
    Task<MyRequestSubmissionDto> SubmitMyLeavePlanDraft(EntityDto input);

    /// <summary>Withdraws a plan still with an approver, or discards a draft.</summary>
    Task CancelMyLeavePlan(EntityDto input);

    /// <summary>
    /// Legacy ESS "Leave Scheduler" (<c>LeavePlannerController._LeavePlannerScheduler</c>): the employee's own
    /// plans and leave and those of the people who report directly to them, for one month.
    /// </summary>
    Task<LeavePlannerDto> GetMyTeamLeavePlanner(GetLeavePlannerInput input);
}
