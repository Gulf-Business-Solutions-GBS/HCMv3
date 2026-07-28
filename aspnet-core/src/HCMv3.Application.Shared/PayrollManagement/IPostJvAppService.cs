using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Journal-voucher posting screen. Ported from legacy
/// <c>HCM10.Web/UserControls/PayrollManagement/uscPostJV.ascx(.cs)</c>.
///
/// <para>The JV LINE SET is not rebuilt here: it comes from the one shared
/// <c>HCMv3.PayrollManagement.Costing.IJournalVoucherBuilder</c> that also serves
/// <see cref="ICostingFileAppService"/>. Legacy duplicated that logic as a private
/// <c>ExportTable(...)</c> in each screen (<c>uscPostJV.ascx.cs</c> lines 1052-1343 vs
/// <c>uscCostingFile.ascx.cs</c> lines 1760-2174); this port keeps exactly one copy. See
/// <c>IJournalVoucherBuilder</c> for the four behaviours where the two legacy copies had drifted and
/// which one won.</para>
/// </summary>
public interface IPostJvAppService : IApplicationService
{
    /// <summary>
    /// Legacy <c>CreatePostJV</c> (<c>uscCostingFile.ascx.cs</c> lines 777-1090). For a Regular run the
    /// whole payroll area is posted as one (or two, when
    /// <c>ManageFIPostingSettings.SeparateProvisionPosting</c> is on) journal(s); for an OffCycle run
    /// legacy posts ONE JOURNAL PER EMPLOYEE (lines 963-1081).
    /// </summary>
    Task<PostJvResultDto> PostJournalVoucher(CostingRunInputDto input);

    /// <summary>Same preview as the costing-file screen, served from the shared builder.</summary>
    Task<JournalVoucherPreviewDto> GetJournalVoucherLines(CostingRunInputDto input);

    /// <summary>Legacy <c>btnValidateJV_Click</c> (<c>uscPostJV.ascx.cs</c> line 235).</summary>
    Task<JournalVoucherValidationResultDto> ValidateJournalVoucher(CostingRunInputDto input);

    /// <summary>Legacy <c>CalculateProcessStatus</c> (<c>uscPostJV.ascx.cs</c> lines 984-1046).</summary>
    Task<CostingProcessStatusDto> GetProcessStatus();

    /// <summary>Legacy <c>TruncateLog</c> (<c>uscPostJV.ascx.cs</c> line 1047).</summary>
    Task ClearProcessStatus();
}
