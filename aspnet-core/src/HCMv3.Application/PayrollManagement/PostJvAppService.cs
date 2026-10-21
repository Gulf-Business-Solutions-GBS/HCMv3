using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.BackgroundJobs;
using HCMv3.Authorization;
using HCMv3.PayrollManagement.Dto;
using HCMv3.Reports.Dto;

namespace HCMv3.PayrollManagement;

/// <inheritdoc cref="IPostJvAppService"/>
/// <remarks>
/// <para>Everything that decides whether a JV may go -- FI links, the Integration Center switch, empty JV,
/// balance, preview fingerprint -- is checked HERE, on the server, before the run is queued, and again by
/// <see cref="PostJvRunner"/> just before the first journal. The screen's disabled button is only a
/// convenience.</para>
///
/// <para>Finance posting otherwise stays as legacy, by the user's decision: no double-post block, no
/// posting history, no reversal or re-post.</para>
/// </remarks>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_PostJv)]
public class PostJvAppService : HCMv3AppServiceBase, IPostJvAppService
{
    private readonly PostJvPreconditions _preconditions;
    private readonly CostingScreenService _screen;
    private readonly IFinanceRunLog _runLog;
    private readonly IBackgroundJobManager _backgroundJobManager;

    public PostJvAppService(
        PostJvPreconditions preconditions,
        CostingScreenService screen,
        IFinanceRunLog runLog,
        IBackgroundJobManager backgroundJobManager)
    {
        _preconditions = preconditions;
        _screen = screen;
        _runLog = runLog;
        _backgroundJobManager = backgroundJobManager;
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_PostJv_Post)]
    public async Task<FinanceRunStartedDto> PostJournalVoucher(CostingRunInputDto input)
    {
        _screen.ValidateInput(input);

        // Legacy lines 784-788 first, then what legacy did not check at all.
        await _preconditions.RequirePostableAreaAsync(input.PayrollAreaName);
        await _preconditions.EnsurePostingSwitchedOnAsync();

        var domainInput = CostingMapper.ToDomain(input);
        await _preconditions.BuildPostableAsync(domainInput, input.PreviewFingerprint);

        return await _screen.StartRunAsync(
            FinanceRunProcess.PostJv,
            domainInput,
            await GetCurrentUserNameOrIdAsync(),
            input.PreviewFingerprint,
            args => _backgroundJobManager.EnqueueAsync<PostJvBackgroundJob, FinanceRunJobArgs>(args));
    }

    // [HttpPost] required: ABP maps Get* to HTTP GET, so NSwag would flatten CostingRunInputDto into
    // query-string params and never generate the DTO class, breaking the Angular client.
    [HttpPost]
    public Task<JournalVoucherPreviewDto> GetJournalVoucherLines(CostingRunInputDto input)
    {
        return _screen.PreviewAsync(input);
    }

    public Task<JournalVoucherValidationResultDto> ValidateJournalVoucher(CostingRunInputDto input)
    {
        return _screen.ValidateAsync(input);
    }

    public Task<List<PayrollAreaLookupDto>> GetPayrollAreasForLookup()
    {
        return Task.FromResult(_screen.GetPayrollAreasForLookup());
    }

    public Task<CostingPeriodContextDto> GetCostingPeriodContext(string payrollAreaName)
    {
        return Task.FromResult(_screen.GetPeriodContext(payrollAreaName));
    }

    public Task<CostingPeriodDatesDto> GetCostingPeriodDates(string payrollAreaName, int payrollMonth, int payrollYear)
    {
        return Task.FromResult(_screen.GetPeriodDates(payrollAreaName, payrollMonth, payrollYear));
    }

    [HttpPost]
    public Task<PayrollRunCandidatesOutput> GetCostingFileCandidates(GetCostingFileCandidatesInput input)
    {
        return _screen.GetCandidatesAsync(input);
    }

    [HttpPost]
    public Task<List<string>> GetCostingFileCandidateCodes(GetCostingFileCandidatesInput input)
    {
        return _screen.GetCandidateCodesAsync(input);
    }

    public Task<CostingProcessStatusDto> GetProcessStatus()
    {
        return _runLog.GetStatusAsync(FinanceRunProcess.PostJv);
    }

    [HttpPost]
    public Task<FinanceRunLogOutput> GetProcessLog(GetFinanceRunLogInput input)
    {
        return _runLog.GetLogAsync(input);
    }

    [HttpPost]
    public Task<ReportPdfOutput> ExportProcessLog(GetFinanceRunLogInput input)
    {
        return _screen.ExportLogAsync(input);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_PostJv_Post)]
    public Task ClearProcessStatus()
    {
        return _runLog.ClearAsync();
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_PostJv_Post)]
    public Task RequestStop()
    {
        return _runLog.RequestStopAsync();
    }

    private async Task<string> GetCurrentUserNameOrIdAsync()
    {
        try
        {
            var user = await GetCurrentUserAsync();
            return user.UserName;
        }
        catch (Exception)
        {
            return AbpSession.UserId?.ToString();
        }
    }
}
