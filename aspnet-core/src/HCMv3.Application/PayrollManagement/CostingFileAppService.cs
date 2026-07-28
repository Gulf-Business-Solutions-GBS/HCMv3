using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Runtime.Session;
using Abp.Timing;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.PayrollManagement.Costing;
using HCMv3.PayrollManagement.Dto;
using HCMv3.ProjectManagement;
using HCMv3.Reports.Dto;
using HCMv3.SystemSecurity;
using MiniExcelLibs;

namespace HCMv3.PayrollManagement;

/// <inheritdoc cref="ICostingFileAppService"/>
/// <remarks>
/// <para><b>DELIBERATE DEVIATION -- no background thread.</b> Legacy ran the cost engine on a
/// <c>System.Threading.Thread</c> kicked off from an <c>ASPxCallbackPanel</c>, with the page polling
/// <c>ProjectLog</c> row counts for progress (<c>uscCostingFile.ascx.cs</c> <c>ExecuteLongOperation</c>
/// line 239 and <c>CalculateProcessStatus</c> line 1574). That pattern relied on WebForms static state
/// (<c>LongOperationStatus</c>, line 37) which has no equivalent -- and no correct equivalent -- in a
/// stateless multi-tenant API host. <see cref="GenerateCostingFile"/> therefore runs INLINE and returns the
/// full per-employee log in one response. <see cref="GetProcessStatus"/> is retained so the screen can still
/// show the <c>ProjectLog</c>-derived counters, and <see cref="RequestStop"/> still raises the
/// <c>StopFlag</c> the engine loop polls, but for a very large payroll area this call will be long-running.
/// Promoting it to a Hangfire background job is the correct follow-up and is NOT attempted here.</para>
///
/// <para><b>Legacy behaviour intentionally dropped:</b> the audit-trail toggling around each employee's
/// object space (<c>ManageSecuritySettings.EnableAuditLogInCostEngine</c>,
/// <c>uscCostingFile.ascx.cs</c> lines 453-463, 706-721) is XPO <c>AuditTrailService</c> plumbing with no
/// ABP counterpart; ABP's own entity-change auditing already covers the written entities.</para>
/// </remarks>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_CostingFile)]
public class CostingFileAppService : HCMv3AppServiceBase, ICostingFileAppService
{
    private readonly ICostEngineService _costEngineService;
    private readonly IJournalVoucherBuilder _journalVoucherBuilder;
    private readonly IRepository<PayrollArea> _payrollAreaRepository;
    private readonly IRepository<ProjectLog> _projectLogRepository;
    private readonly IRepository<ExecutionLog> _executionLogRepository;

    public CostingFileAppService(
        ICostEngineService costEngineService,
        IJournalVoucherBuilder journalVoucherBuilder,
        IRepository<PayrollArea> payrollAreaRepository,
        IRepository<ProjectLog> projectLogRepository,
        IRepository<ExecutionLog> executionLogRepository)
    {
        _costEngineService = costEngineService;
        _journalVoucherBuilder = journalVoucherBuilder;
        _payrollAreaRepository = payrollAreaRepository;
        _projectLogRepository = projectLogRepository;
        _executionLogRepository = executionLogRepository;
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_CostingFile_Run)]
    public async Task<CostingRunResultDto> GenerateCostingFile(CostingRunInputDto input)
    {
        ValidateInput(input);

        // Legacy lines 403-435: an ExecutionLog row is opened INPROCESS before the loop and finalised
        // with the aggregate counts afterwards.
        var executionLogId = await OpenExecutionLogAsync(input);

        CostingRunResult result;
        try
        {
            result = await _costEngineService.RunAsync(CostingMapper.ToDomain(input));
        }
        catch
        {
            await CloseExecutionLogAsync(executionLogId, ExecutionLogStatus.FAILED, 0, 0, 0);
            throw;
        }

        // Legacy lines 685-688: ABORTED wins, then all-success -> SUCCESS, all-failed -> FAILED,
        // otherwise PARTIAL.
        var status = result.Aborted
            ? ExecutionLogStatus.ABORTED
            : result.TotalEmployees == result.SuccessCount
                ? ExecutionLogStatus.SUCCESS
                : result.TotalEmployees == result.FailedCount
                    ? ExecutionLogStatus.FAILED
                    : ExecutionLogStatus.PARTIAL;

        await CloseExecutionLogAsync(executionLogId, status, result.TotalEmployees,
                                     result.SuccessCount, result.FailedCount);

        await PersistProjectLogAsync(result, input.PayInDate);

        return CostingMapper.ToDto(result);
    }

    public async Task<JournalVoucherPreviewDto> GetJournalVoucherLines(CostingRunInputDto input)
    {
        ValidateInput(input);

        var result = await _journalVoucherBuilder.BuildAsync(CostingMapper.ToDomain(input));
        return CostingMapper.ToDto(result);
    }

    public async Task<JournalVoucherValidationResultDto> ValidateJournalVoucher(CostingRunInputDto input)
    {
        ValidateInput(input);

        var result = await _journalVoucherBuilder.BuildAsync(CostingMapper.ToDomain(input));
        return BuildValidationResult(result);
    }

    /// <summary>
    /// Legacy validation block of <c>ExportGrid(onlyValidate: true)</c>
    /// (<c>uscCostingFile.ascx.cs</c> lines 1710-1728). Shared with <c>PostJvAppService</c> so both
    /// screens report identically.
    /// </summary>
    internal JournalVoucherValidationResultDto BuildValidationResult(JournalVoucherResult result)
    {
        var messages = new List<string>();

        if (!result.Lines.Any())
        {
            messages.Add(L("NoDataFoundToPostJV"));
        }
        else if (!result.IsBalanced)
        {
            messages.Add(L("DebitCreditNotMatched"));
        }

        return new JournalVoucherValidationResultDto
        {
            IsValid = !messages.Any(),
            Message = messages.Any() ? string.Join(Environment.NewLine, messages) : L("JVValidatedSuccessfully"),
            TotalDebit = result.TotalDebit,
            TotalCredit = result.TotalCredit,
            LineCount = result.Lines.Count
        };
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_CostingFile_Export)]
    public async Task<ReportPdfOutput> ExportCostingFile(CostingRunInputDto input)
    {
        ValidateInput(input);

        var result = await _journalVoucherBuilder.BuildAsync(CostingMapper.ToDomain(input));

        // Legacy lines 1742-1745 strip BillingType and IsMerged before binding the export grid;
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

        // Legacy file name pattern, lines 1750-1752.
        var fileName = $"CostFile_{input.PayrollAreaName}_{input.PayInDate:MMMM-yyyy}_" +
                       $"{Clock.Now:dd-MM-yyyy HHmm}.xlsx";

        return new ReportPdfOutput(fileName, Convert.ToBase64String(stream.ToArray()));
    }

    public Task<List<PayrollAreaLookupDto>> GetPayrollAreasForLookup()
    {
        var payrollAreas = _payrollAreaRepository.GetAll()
            .OrderBy(a => a.Name)
            .Select(a => new PayrollAreaLookupDto
            {
                Id = a.Id,
                Code = a.Code,
                Name = a.Name,
                IsFiConfigured = a.FICompanyId != null && a.FIBankId != null
            })
            .ToList();

        return Task.FromResult(payrollAreas);
    }

    public Task<CostingProcessStatusDto> GetProcessStatus()
    {
        return Task.FromResult(BuildProcessStatus(_projectLogRepository));
    }

    /// <summary>Legacy <c>CalculateProcessStatus</c>, shared by both screens.</summary>
    internal static CostingProcessStatusDto BuildProcessStatus(IRepository<ProjectLog> projectLogRepository)
    {
        var logs = projectLogRepository.GetAll()
            .Select(l => new { l.Status, l.StopFlag })
            .ToList();

        var total = logs.Count;
        var success = logs.Count(l => l.Status == 0);

        return new CostingProcessStatusDto
        {
            TotalRecords = total,
            SuccessRecords = success,

            // Legacy line 681 clamps the failed count at zero rather than counting Status != 0.
            FailedRecords = total - success > 0 ? total - success : 0,
            StopRequested = logs.Any(l => l.StopFlag)
        };
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_CostingFile_Run)]
    public async Task ClearProcessStatus()
    {
        await ClearProjectLogAsync(_projectLogRepository);
    }

    /// <summary>Legacy <c>TruncateLog</c>, shared by both screens.</summary>
    internal static async Task ClearProjectLogAsync(IRepository<ProjectLog> projectLogRepository)
    {
        var ids = projectLogRepository.GetAll().Select(l => l.Id).ToList();
        foreach (var id in ids)
        {
            await projectLogRepository.DeleteAsync(id);
        }
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_CostingFile_Run)]
    public async Task RequestStop()
    {
        // Legacy stop button (uscCostingFile.ascx.cs lines 2287-2293) sets StopFlag on the FIRST EXISTING
        // ProjectLog row -- it does NOT insert a marker row. Reproduced exactly, and it is also the only
        // option here: this port's ProjectLog.EmployeeId is a non-nullable FK, so an employee-less marker
        // row could not be inserted at all (see the entity gap reported for this wave).
        // The engine loop polls for ANY row with StopFlag set (legacy line 469), so flagging one suffices.
        var first = _projectLogRepository.GetAll().OrderBy(l => l.Id).FirstOrDefault();
        if (first == null)
        {
            // Nothing has been logged yet, so no run is in progress to stop -- legacy no-ops here too
            // (its `if (t1 != null)` guard).
            return;
        }

        first.StopFlag = true;
        await _projectLogRepository.UpdateAsync(first);
    }

    /// <summary>
    /// Legacy input guards from <c>ExecuteLongOperation</c> lines 243-247 and <c>ExportGrid</c> lines
    /// 1648-1689.
    /// </summary>
    private void ValidateInput(CostingRunInputDto input)
    {
        if (string.IsNullOrWhiteSpace(input.PayrollAreaName) && string.IsNullOrWhiteSpace(input.EmployeeCode))
        {
            throw new UserFriendlyException(L("NoPayrollAreaOrEmployeeCodeEntered"));
        }

        if (input.PayInDate == default)
        {
            throw new UserFriendlyException(L("NoPayDateDefinedForJvPost"));
        }

        if (input.PostingDate == default)
        {
            throw new UserFriendlyException(L("PostingDateNotProperFormat"));
        }
    }

    /// <summary>Legacy execution-log parameter string, lines 404-423 (same separator and label order).</summary>
    private async Task<int> OpenExecutionLogAsync(CostingRunInputDto input)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(input.PayrollAreaName))
        {
            parts.Add("Payroll Area : " + input.PayrollAreaName);
        }

        parts.Add("Payroll RunType : " + input.PayrollRunType);
        parts.Add("Period : " + input.PayInDate.ToString("dd/MM/yyyy"));

        if (input.PostingDate != default)
        {
            parts.Add("Posting Date : " + input.PostingDate.ToString("dd/MM/yyyy"));
        }

        if (!string.IsNullOrWhiteSpace(input.EmployeeCode))
        {
            parts.Add("Employee Code : " + input.EmployeeCode);
        }

        return await _executionLogRepository.InsertAndGetIdAsync(new ExecutionLog
        {
            TenantId = AbpSession.GetTenantId(),
            UserName = AbpSession.UserId?.ToString(),
            ActionDateTime = Clock.Now,
            Action = ExecutionLogAction.RUN,
            Module = ExecutionLogModule.CostEngine,
            ScreenName = nameof(CostingFileAppService),
            ScreenCaption = L("CostingFile"),
            Parameters = string.Join(" | ", parts),
            ExecutionStatus = ExecutionLogStatus.INPRECESS,
            TotalRecords = 0,
            Successfull = 0,
            Failed = 0
        });
    }

    private async Task CloseExecutionLogAsync(int executionLogId, ExecutionLogStatus status,
                                              int total, int success, int failed)
    {
        var log = await _executionLogRepository.FirstOrDefaultAsync(executionLogId);
        if (log == null)
        {
            return;
        }

        log.ExecutionStatus = status;
        log.TotalRecords = total;
        log.Successfull = success;
        log.Failed = failed;

        await _executionLogRepository.UpdateAsync(log);
    }

    /// <summary>
    /// Legacy wrote one <c>ProjectLog</c> row per employee inside the loop (lines 614-628 / 632-653).
    /// This port writes them after the run so the engine stays free of persistence concerns it doesn't own.
    /// </summary>
    private async Task PersistProjectLogAsync(CostingRunResult result, DateTime payInDate)
    {
        var tenantId = AbpSession.GetTenantId();

        foreach (var entry in result.Entries)
        {
            // ProjectLog.EmployeeId is a NON-NULLABLE FK in this port, whereas legacy wrote employee-less
            // rows (e.g. uscCostingFile.ascx.cs line 1366 `pl1.Employee = null`). Every cost-engine entry
            // does carry an employee, so nothing is lost here -- but the guard is explicit rather than
            // coercing a missing id to 0, which would fail the FK at runtime.
            if (entry.EmployeeId == null)
            {
                continue;
            }

            await _projectLogRepository.InsertAsync(new ProjectLog
            {
                TenantId = tenantId,
                EmployeeId = entry.EmployeeId.Value,
                InPeriod = payInDate,
                Message = entry.Message,
                Status = entry.Status,
                RunTimeStamp = Clock.Now,
                NoOfEmployee = result.TotalEmployees,
                StopFlag = false
            });
        }
    }
}
