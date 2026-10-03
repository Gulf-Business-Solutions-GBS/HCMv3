using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.Dto;
using HCMv3.SetupChecks.Dto;

namespace HCMv3.SetupChecks;

/// <summary>
/// Tenant Version Check, for a tenant's own administrators: which build the tenant is on, which it was updated from,
/// what changed in between, and what that asks the tenant to set up. Setup is only read, never changed.
/// </summary>
public interface ITenantVersionCheckAppService : IApplicationService
{
    /// <summary>Records the build the tenant is now on (see <c>ReleaseVersionTracker</c>) and returns the versions.</summary>
    Task<TenantVersionCheckStatusDto> GetStatus();

    /// <summary>Everything that changed after an older build, and this tenant's setup items for it.</summary>
    Task<BuildComparisonDto> Compare(CompareBuildsInput input);

    /// <summary>The same comparison as an Excel workbook.</summary>
    Task<FileDto> ExportToExcel(CompareBuildsInput input);
}
