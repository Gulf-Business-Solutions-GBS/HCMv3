using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using HCMv3.Ess.Dto;

namespace HCMv3.Ess;

public class ProxyMyLeaveAppService : ProxyAppServiceBase, IMyLeaveAppService
{
    public async Task<List<MyLeaveBalanceDto>> GetMyBalances()
    {
        return await ApiClient.GetAsync<List<MyLeaveBalanceDto>>(GetEndpoint(nameof(GetMyBalances)));
    }

    public async Task<LeaveFormSettingsDto> GetLeaveFormSettings()
    {
        return await ApiClient.GetAsync<LeaveFormSettingsDto>(GetEndpoint(nameof(GetLeaveFormSettings)));
    }

    public async Task<PagedResultDto<MyLeaveDto>> GetMyLeaves(GetMyLeavesInput input)
    {
        return await ApiClient.PostAsync<PagedResultDto<MyLeaveDto>>(GetEndpoint(nameof(GetMyLeaves)), input);
    }

    public async Task<ApplyForLeaveResultDto> ApplyForLeave(ApplyForLeaveInput input)
    {
        return await ApiClient.PostAsync<ApplyForLeaveResultDto>(GetEndpoint(nameof(ApplyForLeave)), input);
    }

    public async Task<LeaveSplitPreviewDto> PreviewMyLeaveSplit(ApplyForLeaveInput input)
    {
        return await ApiClient.PostAsync<LeaveSplitPreviewDto>(GetEndpoint(nameof(PreviewMyLeaveSplit)), input);
    }

    public async Task<List<SickLeaveDeductionSegmentDto>> PreviewMySickLeaveDeduction(ApplyForLeaveInput input)
    {
        return await ApiClient.PostAsync<List<SickLeaveDeductionSegmentDto>>(GetEndpoint(nameof(PreviewMySickLeaveDeduction)), input);
    }

    public async Task CancelMyLeave(EntityDto input)
    {
        await ApiClient.PostAsync(GetEndpoint(nameof(CancelMyLeave)), input);
    }

    public async Task<int> AmendMyLeave(AmendMyLeaveInput input)
    {
        return await ApiClient.PostAsync<int>(GetEndpoint(nameof(AmendMyLeave)), input);
    }

    public async Task<ReturnFromLeaveResultDto> ReturnFromMyLeave(ReturnFromLeaveInput input)
    {
        return await ApiClient.PostAsync<ReturnFromLeaveResultDto>(GetEndpoint(nameof(ReturnFromMyLeave)), input);
    }

    public async Task<ReturnFromLeavePreviewDto> PreviewReturnFromMyLeave(ReturnFromLeaveInput input)
    {
        return await ApiClient.PostAsync<ReturnFromLeavePreviewDto>(GetEndpoint(nameof(PreviewReturnFromMyLeave)), input);
    }

    public async Task<int> RequestCancellationOfMyLeave(EntityDto input)
    {
        return await ApiClient.PostAsync<int>(GetEndpoint(nameof(RequestCancellationOfMyLeave)), input);
    }
}
