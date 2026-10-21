using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.PayrollManagement.Dto;
using HCMv3.Reports.Dto;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Post JV to Dynamics 365. Ported from the Post JV half of legacy
/// <c>HCM10.Web/UserControls/PayrollManagement/uscCostingFile.ascx(.cs)</c> (<c>CreatePostJV</c> ->
/// <c>PostTablewiseJV_D360</c>), the screen that is live. The older AX screen <c>uscPostJV.ascx</c> is hidden
/// in legacy and is reference only.
///
/// <para>The JV LINE SET is not rebuilt here: it comes from the one shared
/// <c>HCMv3.PayrollManagement.Costing.IJournalVoucherBuilder</c> that also serves
/// <see cref="ICostingFileAppService"/>.</para>
/// </summary>
public interface IPostJvAppService : IApplicationService
{
    /// <summary>
    /// Legacy <c>CreatePostJV</c> (<c>uscCostingFile.ascx.cs</c> lines 765-1078). For a Regular run the
    /// whole payroll area is posted as one (or two, when
    /// <c>ManageFIPostingSettings.SeparateProvisionPosting</c> is on) journal(s); for an OffCycle run
    /// legacy posts ONE JOURNAL PER EMPLOYEE (lines 932-1075).
    ///
    /// <para>Refused, before anything is queued, when Dynamics 365 Finance is switched off in the Integration
    /// Center, when the area lacks its FI links, when there is nothing to post, when the JV does not balance
    /// (legacy's whole-unit test), or when it is not the JV whose preview fingerprint is sent in
    /// <see cref="CostingRunInputDto.PreviewFingerprint"/>. Then QUEUED as a background job; poll
    /// <see cref="GetProcessStatus"/> and <see cref="GetProcessLog"/>.</para>
    /// </summary>
    Task<FinanceRunStartedDto> PostJournalVoucher(CostingRunInputDto input);

    /// <summary>Same preview as the costing-file screen, served from the shared builder, with its fingerprint.</summary>
    Task<JournalVoucherPreviewDto> GetJournalVoucherLines(CostingRunInputDto input);

    /// <summary>Legacy "Validate JV".</summary>
    Task<JournalVoucherValidationResultDto> ValidateJournalVoucher(CostingRunInputDto input);

    /// <summary>Legacy <c>BindPayrollArea</c> combo source.</summary>
    Task<List<PayrollAreaLookupDto>> GetPayrollAreasForLookup();

    /// <summary>Legacy "Current" period: <c>SetCurrentDates</c> from the area's payroll control record.</summary>
    Task<CostingPeriodContextDto> GetCostingPeriodContext(string payrollAreaName);

    /// <summary>Legacy "Other" period: <c>SetDates</c> for a typed month and year.</summary>
    Task<CostingPeriodDatesDto> GetCostingPeriodDates(string payrollAreaName, int payrollMonth, int payrollYear);

    /// <summary>Legacy employee popup: one page.</summary>
    Task<PayrollRunCandidatesOutput> GetCostingFileCandidates(GetCostingFileCandidatesInput input);

    /// <summary>Every code the popup's filter shows, for Select All and OK.</summary>
    Task<List<string>> GetCostingFileCandidateCodes(GetCostingFileCandidatesInput input);

    /// <summary>Legacy <c>CalculateProcessStatus</c> for the Post JV run.</summary>
    Task<CostingProcessStatusDto> GetProcessStatus();

    /// <summary>Legacy "Show details" grid: every header, line batch and closing row, with D365's full error text.</summary>
    Task<FinanceRunLogOutput> GetProcessLog(GetFinanceRunLogInput input);

    /// <summary>Legacy Export Log button.</summary>
    Task<ReportPdfOutput> ExportProcessLog(GetFinanceRunLogInput input);

    /// <summary>Legacy "Clear Status" -> <c>TruncateLog</c>.</summary>
    Task ClearProcessStatus();

    /// <summary>Legacy "Stop Post JV" button.</summary>
    Task RequestStop();
}
