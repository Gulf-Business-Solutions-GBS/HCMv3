using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.GoLiveSetup.Dto;

namespace HCMv3.GoLiveSetup;

/// <summary>
/// Turns the saved Go-Live Setup draft into real HCMv3 records.
///
/// <para><b>Preview first.</b> Both methods run the same code path; preview simply does not write. That
/// is deliberate -- the numbers a consultant approves cannot drift from what the button then does. The
/// UI is expected to require a clean preview before offering Apply.</para>
///
/// <para><b>Idempotent.</b> Rows are matched on their natural key (employee code), so a re-run updates
/// what exists rather than duplicating it. Re-running after fixing four bad rows is the normal case, not
/// an edge case.</para>
///
/// <para><b>Per-row isolation.</b> A row that cannot be provisioned is reported and the run continues.
/// Refusing the whole sheet because one civil ID is malformed would make a 2,000-employee import
/// impossible to land.</para>
/// </summary>
public interface IGoLiveApplyAppService : IApplicationService
{
    /// <summary>Reports what Apply would do. Writes nothing.</summary>
    Task<GoLiveApplyResultDto> Preview();

    /// <summary>Provisions the draft. Safe to re-run.</summary>
    Task<GoLiveApplyResultDto> Apply();
}
