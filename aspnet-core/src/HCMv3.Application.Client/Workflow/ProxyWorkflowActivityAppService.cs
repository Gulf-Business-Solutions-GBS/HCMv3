using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.Workflow.Dto;

namespace HCMv3.Workflow;

/// <summary>
/// The mobile app's client for api/services/app/WorkflowActivity. Every read on the server carries
/// [HttpPost] (a Get* method with a custom input DTO), so they are posted here too; AddComment and EditComment
/// fall to ABP's POST default, and DeleteComment is an HTTP DELETE with the id on the query string.
/// </summary>
public class ProxyWorkflowActivityAppService : ProxyAppServiceBase, IWorkflowActivityAppService
{
    public async Task<WorkflowActivityFeedDto> GetActivityFeed(GetWorkflowActivityFeedInput input)
    {
        return await ApiClient.PostAsync<WorkflowActivityFeedDto>(GetEndpoint(nameof(GetActivityFeed)), input);
    }

    public async Task<WorkflowRequestDetailDto> GetRequestDetail(GetWorkflowRequestDetailInput input)
    {
        return await ApiClient.PostAsync<WorkflowRequestDetailDto>(GetEndpoint(nameof(GetRequestDetail)), input);
    }

    public async Task<WorkflowParticipantsDto> GetParticipants(GetWorkflowParticipantsInput input)
    {
        return await ApiClient.PostAsync<WorkflowParticipantsDto>(GetEndpoint(nameof(GetParticipants)), input);
    }

    public async Task<WorkflowParticipantProfileDto> GetParticipantProfile(GetWorkflowParticipantProfileInput input)
    {
        return await ApiClient.PostAsync<WorkflowParticipantProfileDto>(GetEndpoint(nameof(GetParticipantProfile)), input);
    }

    public async Task<List<WorkflowRequestLogEntryDto>> GetRequestLog(GetWorkflowRequestLogInput input)
    {
        return await ApiClient.PostAsync<List<WorkflowRequestLogEntryDto>>(GetEndpoint(nameof(GetRequestLog)), input);
    }

    public async Task<FileDto> ExportRequestLogToExcel(GetWorkflowRequestLogInput input)
    {
        return await ApiClient.PostAsync<FileDto>(GetEndpoint(nameof(ExportRequestLogToExcel)), input);
    }

    public async Task<WorkflowActivityDto> AddComment(AddWorkflowCommentInput input)
    {
        return await ApiClient.PostAsync<WorkflowActivityDto>(GetEndpoint(nameof(AddComment)), input);
    }

    public async Task<WorkflowActivityDto> EditComment(EditWorkflowCommentInput input)
    {
        return await ApiClient.PostAsync<WorkflowActivityDto>(GetEndpoint(nameof(EditComment)), input);
    }

    public async Task DeleteComment(EntityDto input)
    {
        await ApiClient.DeleteAsync(GetEndpoint(nameof(DeleteComment)), input);
    }
}
