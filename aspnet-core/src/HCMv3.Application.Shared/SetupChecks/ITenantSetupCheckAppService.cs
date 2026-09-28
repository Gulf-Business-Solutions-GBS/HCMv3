using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.Dto;
using HCMv3.SetupChecks.Dto;

namespace HCMv3.SetupChecks;

/// <summary>
/// Host-only: which setup each HCMv3 release needs, and whether a chosen tenant has it. Reads setup only, stores
/// nothing, and names employees by code only.
/// </summary>
public interface ITenantSetupCheckAppService : IApplicationService
{
    /// <summary>The releases (newest first) and tenants to choose from.</summary>
    Task<TenantSetupCheckOptionsDto> GetOptions();

    /// <summary>Runs the checks for one tenant and one release.</summary>
    Task<TenantSetupCheckReportDto> RunChecks(RunTenantSetupChecksInput input);

    /// <summary>Runs the checks and returns them as an Excel workbook.</summary>
    Task<FileDto> ExportToExcel(RunTenantSetupChecksInput input);
}
