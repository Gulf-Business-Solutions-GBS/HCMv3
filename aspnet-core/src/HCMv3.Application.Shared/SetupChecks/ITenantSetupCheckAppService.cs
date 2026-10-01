using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.Dto;
using HCMv3.SetupChecks.Dto;

namespace HCMv3.SetupChecks;

/// <summary>
/// What's New &amp; Setup, for a tenant's own administrators: which release the tenant is on, which it came from, what
/// the update added, and whether the setup each new feature needs has been done. Setup is only read, never changed;
/// employees are named by code only.
/// </summary>
public interface ITenantSetupCheckAppService : IApplicationService
{
    /// <summary>Records the release the tenant is now on (see <c>ReleaseVersionTracker</c>) and returns what changed.</summary>
    Task<ReleaseUpdateStatusDto> GetUpdateStatus();

    /// <summary>Any one release, with this tenant's setup status for each of its features.</summary>
    Task<ReleaseChangesDto> GetRelease(string version);

    /// <summary>A release, or what the latest update brought, as an Excel workbook.</summary>
    Task<FileDto> ExportToExcel(ExportReleaseChangesInput input);
}
