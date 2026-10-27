using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.BackgroundJobs;
using Abp.Timing;
using HCMv3.Authorization;
using HCMv3.PayrollManagement.Costing;
using HCMv3.PayrollManagement.Dto;
using HCMv3.Reports.Dto;
using MiniExcelLibs;

namespace HCMv3.PayrollManagement;

/// <inheritdoc cref="ICostingFileAppService"/>
/// <remarks>
/// <para><b>Background run, as legacy.</b> Legacy ran the cost engine on a pool thread with the page polling
/// <c>ProjectLog</c> for progress (<c>uscCostingFile.ascx.cs</c> lines 2204-2212, 2117-2184). Here
/// <see cref="GenerateCostingFile"/> queues <see cref="CostingFileBackgroundJob"/> and returns; the job writes
/// one <c>ProjectLog</c> row per employee as it goes, so <see cref="GetProcessStatus"/> and
/// <see cref="GetProcessLog"/> show live progress and the per-employee grid, and <see cref="RequestStop"/>
/// takes effect before the next employee.</para>
///
/// <para><b>Legacy behaviour intentionally dropped:</b> the audit-trail toggling around each employee's
/// object space (<c>ManageSecuritySettings.EnableAuditLogInCostEngine</c>,
/// <c>uscCostingFile.ascx.cs</c> lines 441-451) is XPO <c>AuditTrailService</c> plumbing with no
/// ABP counterpart; ABP's own entity-change auditing already covers the written entities.</para>
/// </remarks>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_CostingFile)]
public class CostingFileAppService : HCMv3AppServiceBase, ICostingFileAppService
{
    private readonly IJournalVoucherBuilder _journalVoucherBuilder;
    private readonly CostingScreenService _screen;
    private readonly IFinanceRunLog _runLog;
    private readonly IBackgroundJobManager _backgroundJobManager;

    public CostingFileAppService(
        IJournalVoucherBuilder journalVoucherBuilder,
        CostingScreenService screen,
        IFinanceRunLog runLog,
        IBackgroundJobManager backgroundJobManager)
    {
        _journalVoucherBuilder = journalVoucherBuilder;
        _screen = screen;
        _runLog = runLog;
        _backgroundJobManager = backgroundJobManager;
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_CostingFile_Run)]
    public async Task<FinanceRunStartedDto> GenerateCostingFile(CostingRunInputDto input)
    {
        _screen.ValidateInput(input);

        return await _screen.StartRunAsync(
            FinanceRunProcess.CostEngine,
            CostingMapper.ToDomain(input),
            await GetCurrentUserNameOrIdAsync(),
            null,
            args => _backgroundJobManager.EnqueueAsync<CostingFileBackgroundJob, FinanceRunJobArgs>(args));
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_CostingFile_Export)]
    public async Task<ReportPdfOutput> ExportCostingFile(CostingRunInputDto input)
    {
        _screen.ValidateInput(input);

        var result = await _journalVoucherBuilder.BuildAsync(CostingMapper.ToDomain(input));

        // Legacy lines 1650-1653 strip BillingType and IsMerged before binding the export grid;
        // JournalVoucherColumns.ExportOrder encodes exactly that column set and order.
        var rows = result.Lines.Select(l => new Dictionary<string, object>
        {
            [JournalVoucherColumns.Valid] = l.Valid,
            [JournalVoucherColumns.CompanyAccounts] = l.CompanyAccounts,
            [JournalVoucherColumns.JournalBatchNumber] = l.JournalBatchNumber,
            [JournalVoucherColumns.RecId] = l.RecId,
            [JournalVoucherColumns.LineNumber] = l.LineNumber,
            [JournalVoucherColumns.LineNumberDate] = l.LineNumberDate,
            [JournalVoucherColumns.LineNumberAccountType] = l.LineNumberAccountType,
            [JournalVoucherColumns.LedgerDimensionMainAccount] = l.LedgerDimensionMainAccount,
            [JournalVoucherColumns.LedgerDimensionMainAccountName] = l.LedgerDimensionMainAccountName,
            [JournalVoucherColumns.DefaultDimensionLocation] = l.DefaultDimensionLocation,
            [JournalVoucherColumns.DefaultDimensionEmployees] = l.DefaultDimensionEmployees,
            [JournalVoucherColumns.LedgerDimensionLocation] = l.LedgerDimensionLocation,
            [JournalVoucherColumns.LedgerDimensionBrand] = l.LedgerDimensionBrand,
            [JournalVoucherColumns.LedgerDimensionDepartment] = l.LedgerDimensionDepartment,
            [JournalVoucherColumns.LedgerDimensionEmployees] = l.LedgerDimensionEmployees,
            [JournalVoucherColumns.Currency] = l.Currency,
            [JournalVoucherColumns.ExchangeRate] = l.ExchangeRate,
            [JournalVoucherColumns.Debit] = l.Debit,
            [JournalVoucherColumns.Credit] = l.Credit,
            [JournalVoucherColumns.Description] = l.Description,
            [JournalVoucherColumns.Voucher] = l.Voucher,
            [JournalVoucherColumns.OffsetAccountType] = l.OffsetAccountType,
            [JournalVoucherColumns.OffsetLedgerDimensionMainAccount] = l.OffsetLedgerDimensionMainAccount,
            [JournalVoucherColumns.OffsetCompanyAccounts] = l.OffsetCompanyAccounts,
            [JournalVoucherColumns.TransDate] = l.TransDate,
            [JournalVoucherColumns.EmpCode] = l.EmpCode
        }).ToList();

        using var stream = new MemoryStream();
        MiniExcel.SaveAs(stream, rows, printHeader: true, excelType: ExcelType.XLSX);

        // Legacy file name pattern, lines 1657-1659.
        var fileName = $"CostFile_{input.PayrollAreaName}_" +
                       input.PayInDate.ToString("MMMM-yyyy", CultureInfo.InvariantCulture) + "_" +
                       Clock.Now.ToString("dd-MM-yyyy HHmm", CultureInfo.InvariantCulture) + ".xlsx";

        return new ReportPdfOutput(fileName, Convert.ToBase64String(stream.ToArray()));
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
        return _runLog.GetStatusAsync(FinanceRunProcess.CostEngine);
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_CostingFile_Run)]
    public Task ClearProcessStatus()
    {
        return _runLog.ClearAsync();
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_CostingFile_Run)]
    public Task RequestStop()
    {
        return _runLog.RequestStopAsync();
    }

    /// <summary>Legacy wrote the user NAME on the run record; falls back to the id when the user cannot be read.</summary>
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
