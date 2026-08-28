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

    Task<WorkflowActivityDto> AddComment(AddWorkflowCommentInput input);

    Task<WorkflowActivityDto> EditComment(EditWorkflowCommentInput input);

    Task DeleteComment(EntityDto input);
}
