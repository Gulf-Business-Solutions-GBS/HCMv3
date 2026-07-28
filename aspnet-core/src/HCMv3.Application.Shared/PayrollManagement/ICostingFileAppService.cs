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
    /// Runs the cost engine. Legacy "Generate Costing File" button ->
    /// <c>ExecuteLongOperation</c> -> <c>Main</c> -> <c>CreateCostRegister</c>
    /// (<c>uscCostingFile.ascx.cs</c> lines 239-279, 397-703).
    ///
    /// <para>Legacy ran this on a background thread with a polling status panel; this port runs it
    /// synchronously within the request. See the implementation's remarks.</para>
    /// </summary>
    Task<CostingRunResultDto> GenerateCostingFile(CostingRunInputDto input);

    /// <summary>
    /// Builds the JV preview grid without posting anything. Legacy <c>ExportTable(...)</c> feeding the
    /// on-screen grid.
    /// </summary>
    Task<JournalVoucherPreviewDto> GetJournalVoucherLines(CostingRunInputDto input);

    /// <summary>
    /// Legacy "Validate JV" button -> <c>btnValidateJV_Click</c> -> <c>ExportGrid(onlyValidate: true)</c>
    /// (<c>uscCostingFile.ascx.cs</c> lines 232-235, 1710-1728). Checks that rows exist and that
    /// debits equal credits after whole-unit rounding.
    /// </summary>
    Task<JournalVoucherValidationResultDto> ValidateJournalVoucher(CostingRunInputDto input);

    /// <summary>
    /// Legacy "Export" button -> <c>btnExportCostingFile_Click</c> -> <c>ExportGrid(onlyValidate: false)</c>
    /// -> <c>gridExport.WriteXlsToResponse()</c> (<c>uscCostingFile.ascx.cs</c> lines 228-231, 1741-1755).
    /// Returns the spreadsheet as base64 rather than writing to the response stream or to disk.
    /// </summary>
    Task<ReportPdfOutput> ExportCostingFile(CostingRunInputDto input);

    /// <summary>Legacy <c>BindPayrollArea</c> combo source.</summary>
    Task<List<PayrollAreaLookupDto>> GetPayrollAreasForLookup();

    /// <summary>Legacy <c>CalculateProcessStatus</c> status panel (lines 1574-1636).</summary>
    Task<CostingProcessStatusDto> GetProcessStatus();

    /// <summary>Legacy "Clear Status" button -> <c>TruncateLog</c> (line 1637).</summary>
    Task ClearProcessStatus();

    /// <summary>Legacy "Stop Costing File" button: raises the <c>ProjectLog.StopFlag</c> the run loop polls.</summary>
    Task RequestStop();
}
