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

    Task<WorkflowActivityDto> AddComment(AddWorkflowCommentInput input);

    Task<WorkflowActivityDto> EditComment(EditWorkflowCommentInput input);

    Task DeleteComment(EntityDto input);
}
