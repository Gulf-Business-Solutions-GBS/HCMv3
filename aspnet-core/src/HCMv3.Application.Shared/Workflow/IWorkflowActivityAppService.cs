using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Workflow.Dto;

namespace HCMv3.Workflow;

/// <summary>
/// The activity feed for one workflow request: its action trail and its discussion, merged and ordered.
///
/// <para>
/// This is what replaces legacy's two separate surfaces -- a Syncfusion grid of <c>RequestLog</c> rows, and
/// a read-only <c>WorkflowComments</c> text blob that approvers' comments were string-concatenated onto.
/// The same feed serves the approval screen, the requester's own view of their request, and the
/// "Workflow History" button on the admin transaction screen.
/// </para>
/// </summary>
public interface IWorkflowActivityAppService : IApplicationService
{
    Task<WorkflowActivityFeedDto> GetActivityFeed(GetWorkflowActivityFeedInput input);

    /// <summary>
    /// One request's transaction fields, projected by the type's own handler, plus who raised it.
    ///
    /// <para>
    /// Lives on this service rather than on a new one because it answers the same authorization question the
    /// feed already answers -- "is this viewer part of this request?" -- and duplicating that check in a
    /// second place is how the two drift apart. The screen needs both together anyway.
    /// </para>
    /// </summary>
    Task<WorkflowRequestDetailDto> GetRequestDetail(GetWorkflowRequestDetailInput input);

    /// <summary>
    /// Who is entitled to act on one request, who has already acted, who stands in for whom, and who has
    /// opened it.
    ///
    /// <para>
    /// Sits beside <see cref="GetRequestDetail"/> rather than on a service of its own because it asks the
    /// identical authorization question -- "is this viewer part of this request?" -- and a second copy of
    /// that check is how the two drift apart. It is emphatically NOT
    /// <c>IWorkFlowApproversAppService</c>, which is the admin configuration screen and is gated on an
    /// admin permission no ordinary approver holds.
    /// </para>
    /// </summary>
    Task<WorkflowParticipantsDto> GetParticipants(GetWorkflowParticipantsInput input);

    Task<WorkflowActivityDto> AddComment(AddWorkflowCommentInput input);

    Task<WorkflowActivityDto> EditComment(EditWorkflowCommentInput input);

    Task DeleteComment(EntityDto input);
}
