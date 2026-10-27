using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dashboards.Workflow.Dto;
using HCMv3.Dto;

namespace HCMv3.Dashboards.Workflow;

/// <summary>
/// The Workflow Requests Dashboard: requests raised by the employees in scope over a period -- how many, of which type
/// and department, how they were decided and how long that took, and which open ones are overdue or waiting with whom.
/// Data scope comes from <c>IDashboardScopeService</c>.
/// </summary>
public interface IWorkflowDashboardAppService : IApplicationService
{
    Task<WorkflowDashboardDto> GetDashboard(WorkflowDashboardInput input);

    Task<PagedResultDto<WorkflowRequestRowDto>> GetRequests(GetWorkflowRequestsInput input);

    Task<FileDto> ExportToExcel(ExportWorkflowDashboardInput input);
}
