using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.PayrollManagement.Dto;
using HCMv3.Reports.Dto;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Cost allocation file screen. Ported from legacy
/// <c>HCM10.Web/UserControls/PayrollManagement/uscCostingFile.ascx(.cs)</c>.
///
/// <para>NOTE on the legacy clone: <c>uscCostingFile2.*</c> is a byte-for-byte copy of
/// <c>uscCostingFile.*</c> with only the class name changed (verified by normalised diff -- zero differing
/// lines). It is NOT ported separately.</para>
/// </summary>
public interface ICostingFileAppService : IApplicationService
{
    /// <summary>
    /// Legacy "Generate Costing File" button -> <c>ExecuteLongOperation</c> -> <c>Main</c> ->
    /// <c>CreateCostRegister</c> (<c>uscCostingFile.ascx.cs</c> lines 239-279, 385-691). Empties the log and
    /// QUEUES the run as a background job, as legacy queued it on a pool thread; poll
    /// <see cref="GetProcessStatus"/> for progress and <see cref="GetProcessLog"/> for the per-employee grid.
    /// Refused while another cost-engine or Post JV run is going.
    /// </summary>
    Task<FinanceRunStartedDto> GenerateCostingFile(CostingRunInputDto input);

    /// <summary>
    /// Builds the JV preview grid without posting anything. Legacy <c>ExportTable(...)</c> feeding the
    /// on-screen grid. Carries the fingerprint Post JV checks against.
    /// </summary>
    Task<JournalVoucherPreviewDto> GetJournalVoucherLines(CostingRunInputDto input);

    /// <summary>
    /// Legacy "Validate JV" button -> <c>btnValidateJV_Click</c> -> <c>ExportGrid(onlyValidate: true)</c>
    /// (<c>uscCostingFile.ascx.cs</c> lines 232-235, 1618-1634). Checks that rows exist and that
    /// debits equal credits after whole-unit rounding.
    /// </summary>
    Task<JournalVoucherValidationResultDto> ValidateJournalVoucher(CostingRunInputDto input);

    /// <summary>
    /// Legacy "Export" button -> <c>btnExportCostingFile_Click</c> -> <c>ExportGrid(onlyValidate: false)</c>
    /// -> <c>gridExport.WriteXlsToResponse()</c> (<c>uscCostingFile.ascx.cs</c> lines 228-231, 1648-1662).
    /// Returns the spreadsheet as base64 rather than writing to the response stream or to disk.
    /// </summary>
    Task<ReportPdfOutput> ExportCostingFile(CostingRunInputDto input);

    /// <summary>Legacy <c>BindPayrollArea</c> combo source.</summary>
    Task<List<PayrollAreaLookupDto>> GetPayrollAreasForLookup();

    /// <summary>Legacy "Current" period: <c>SetCurrentDates</c> from the area's payroll control record.</summary>
    Task<CostingPeriodContextDto> GetCostingPeriodContext(string payrollAreaName);

    /// <summary>Legacy "Other" period: <c>SetDates</c> for a typed month and year.</summary>
    Task<CostingPeriodDatesDto> GetCostingPeriodDates(string payrollAreaName, int payrollMonth, int payrollYear);

    /// <summary>Legacy employee popup (the "..." button beside Employee Code): one page.</summary>
    Task<PayrollRunCandidatesOutput> GetCostingFileCandidates(GetCostingFileCandidatesInput input);

    /// <summary>Every code the popup's filter shows, for Select All and OK.</summary>
    Task<List<string>> GetCostingFileCandidateCodes(GetCostingFileCandidatesInput input);

    /// <summary>Legacy <c>CalculateProcessStatus</c> status panel (lines 2484-2585) and status label.</summary>
    Task<CostingProcessStatusDto> GetProcessStatus();

    /// <summary>Legacy "Show details" grid (Cost Execution Log popup), one page.</summary>
    Task<FinanceRunLogOutput> GetProcessLog(GetFinanceRunLogInput input);

    /// <summary>Legacy Export Log button on that popup.</summary>
    Task<ReportPdfOutput> ExportProcessLog(GetFinanceRunLogInput input);

    /// <summary>Legacy "Clear Status" button -> <c>TruncateLog</c>.</summary>
    Task ClearProcessStatus();

    /// <summary>Legacy "Stop Costing File" button: stops the run before its next employee.</summary>
    Task RequestStop();
}
