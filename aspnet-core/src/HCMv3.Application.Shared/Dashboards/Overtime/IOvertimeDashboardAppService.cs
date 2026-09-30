using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dashboards.Overtime.Dto;
using HCMv3.Dto;

namespace HCMv3.Dashboards.Overtime;

/// <summary>
/// Overtime Dashboard: approved overtime hours and what payroll paid for them, by month, type, department and
/// employee, plus requests still waiting for approval. Replaces the legacy ESS Overtime dashboard.
///
/// Whose employees it shows comes from <c>IDashboardScopeService</c>, never from the client.
/// </summary>
public interface IOvertimeDashboardAppService : IApplicationService
{
    Task<OvertimeDashboardDto> GetDashboard(OvertimeDashboardInput input);

    Task<PagedResultDto<OvertimeEntryRowDto>> GetEntries(GetOvertimeEntriesInput input);

    Task<FileDto> ExportToExcel(ExportOvertimeDashboardInput input);
}
