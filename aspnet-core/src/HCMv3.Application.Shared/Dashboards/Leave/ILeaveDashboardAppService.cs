using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dashboards.Leave.Dto;
using HCMv3.Dto;

namespace HCMv3.Dashboards.Leave;

/// <summary>
/// Leave Dashboard: who is out today, who is overdue back, and how leave is spread over time, departments
/// and leave types. Replaces the legacy HR "Leave Dashboard" (two lists) and the ESS Leave Planner charts.
///
/// One screen for everybody: whose leave it shows comes from <c>IDashboardScopeService</c> (row-level security,
/// the Admin role and the user's own team), never from the client.
/// </summary>
public interface ILeaveDashboardAppService : IApplicationService
{
    Task<LeaveDashboardOptionsDto> GetOptions();

    Task<LeaveDashboardDto> GetDashboard(LeaveDashboardInput input);

    Task<PagedResultDto<LeaveDashboardLeaveRowDto>> GetLeaves(GetLeaveDashboardLeavesInput input);

    /// <summary>Excel workbook of the whole dashboard or one tile, with the same filters and scope.</summary>
    Task<FileDto> ExportToExcel(ExportLeaveDashboardInput input);
}
