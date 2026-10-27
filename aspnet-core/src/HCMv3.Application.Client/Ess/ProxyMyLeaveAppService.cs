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

    public async Task<int> RequestCancellationOfMyLeave(RequestLeaveCancellationInput input)
    {
        return await ApiClient.PostAsync<int>(GetEndpoint(nameof(RequestCancellationOfMyLeave)), input);
    }

    public async Task<List<MyLeaveTypeDto>> GetMyLeaveTypes()
    {
        return await ApiClient.GetAsync<List<MyLeaveTypeDto>>(GetEndpoint(nameof(GetMyLeaveTypes)));
    }

    public async Task<LeaveBalancePreviewDto> PreviewMyLeaveBalance(PreviewLeaveBalanceInput input)
    {
        return await ApiClient.PostAsync<LeaveBalancePreviewDto>(GetEndpoint(nameof(PreviewMyLeaveBalance)), input);
    }

    public async Task<ApplyForLeaveResultDto> ResubmitMyLeave(ResubmitMyLeaveInput input)
    {
        return await ApiClient.PostAsync<ApplyForLeaveResultDto>(GetEndpoint(nameof(ResubmitMyLeave)), input);
    }

    public async Task<int> ResubmitMyLeaveChange(ResubmitMyLeaveChangeInput input)
    {
        return await ApiClient.PostAsync<int>(GetEndpoint(nameof(ResubmitMyLeaveChange)), input);
    }

    public async Task WithdrawMyLeaveChange(WithdrawMyLeaveChangeInput input)
    {
        await ApiClient.PostAsync(GetEndpoint(nameof(WithdrawMyLeaveChange)), input);
    }

    public async Task<List<MyLeaveHistoryItemDto>> GetMyLeaveHistory(EntityDto input)
    {
        return await ApiClient.GetAsync<List<MyLeaveHistoryItemDto>>(GetEndpoint(nameof(GetMyLeaveHistory)), new { id = input.Id });
    }

    public async Task<HCMv3.Dto.FileDto> GetMyLeavesToExcel(ExportMyLeavesInput input)
    {
        return await ApiClient.PostAsync<HCMv3.Dto.FileDto>(GetEndpoint(nameof(GetMyLeavesToExcel)), input);
    }

    public async Task<ApplyForLeaveResultDto> SaveMyLeaveDraft(SaveMyLeaveDraftInput input)
    {
        return await ApiClient.PostAsync<ApplyForLeaveResultDto>(GetEndpoint(nameof(SaveMyLeaveDraft)), input);
    }

    public async Task<ApplyForLeaveResultDto> SubmitMyLeaveDraft(EntityDto input)
    {
        return await ApiClient.PostAsync<ApplyForLeaveResultDto>(GetEndpoint(nameof(SubmitMyLeaveDraft)), input);
    }

    public async Task<MyLeaveDto> GetMyLeaveForEdit(EntityDto input)
    {
        return await ApiClient.GetAsync<MyLeaveDto>(GetEndpoint(nameof(GetMyLeaveForEdit)), new { id = input.Id });
    }

    public async Task<int> SaveMyLeaveChangeDraft(SaveMyLeaveChangeDraftInput input)
    {
        return await ApiClient.PostAsync<int>(GetEndpoint(nameof(SaveMyLeaveChangeDraft)), input);
    }
}
