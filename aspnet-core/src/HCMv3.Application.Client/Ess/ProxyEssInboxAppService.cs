using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using HCMv3.Ess.Dto;

namespace HCMv3.Ess;

public class ProxyEssInboxAppService : ProxyAppServiceBase, IEssInboxAppService
{
    public async Task<PagedResultDto<EssInboxItemDto>> GetMyInbox(GetMyInboxInput input)
    {
        return await ApiClient.PostAsync<PagedResultDto<EssInboxItemDto>>(GetEndpoint(nameof(GetMyInbox)), input);
    }

    public async Task<int> GetMyPendingCount()
    {
        return await ApiClient.GetAsync<int>(GetEndpoint(nameof(GetMyPendingCount)));
    }

    public async Task<EssInboxCountsDto> GetMyInboxCounts()
    {
        return await ApiClient.GetAsync<EssInboxCountsDto>(GetEndpoint(nameof(GetMyInboxCounts)));
    }

    public async Task Approve(ApproveRequestInput input)
    {
        await ApiClient.PostAsync(GetEndpoint(nameof(Approve)), input);
    }

    public async Task<BulkApproveResultDto> BulkApprove(BulkApproveInput input)
    {
        return await ApiClient.PostAsync<BulkApproveResultDto>(GetEndpoint(nameof(BulkApprove)), input);
    }

    public async Task Reject(RejectRequestInput input)
    {
        await ApiClient.PostAsync(GetEndpoint(nameof(Reject)), input);
    }

    public async Task SendBack(SendBackRequestInput input)
    {
        await ApiClient.PostAsync(GetEndpoint(nameof(SendBack)), input);
    }

    public async Task RequestFeedback(RequestFeedbackInput input)
    {
        await ApiClient.PostAsync(GetEndpoint(nameof(RequestFeedback)), input);
    }

    public async Task SubmitFeedback(SubmitFeedbackInput input)
    {
        await ApiClient.PostAsync(GetEndpoint(nameof(SubmitFeedback)), input);
    }

    public async Task<List<FeedbackCandidateDto>> GetFeedbackCandidates(GetFeedbackCandidatesInput input)
    {
        return await ApiClient.PostAsync<List<FeedbackCandidateDto>>(GetEndpoint(nameof(GetFeedbackCandidates)), input);
    }

    public async Task CancelFeedbackRequest(CancelFeedbackInput input)
    {
        await ApiClient.PostAsync(GetEndpoint(nameof(CancelFeedbackRequest)), input);
    }

    public async Task<List<FeedbackRequestDto>> GetFeedbackRequests(GetFeedbackCandidatesInput input)
    {
        return await ApiClient.PostAsync<List<FeedbackRequestDto>>(GetEndpoint(nameof(GetFeedbackRequests)), input);
    }
}
