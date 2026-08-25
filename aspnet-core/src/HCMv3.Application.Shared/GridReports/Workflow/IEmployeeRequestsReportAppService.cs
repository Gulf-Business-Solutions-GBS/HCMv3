using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.Workflow.Dto;

namespace HCMv3.GridReports.Workflow;

/// <summary>
/// Employee Requests analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Workflow/uscRptEmployeeRequests.ascx</c>).
/// </summary>
public interface IEmployeeRequestsReportAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeRequestsReportRowDto>> GetEmployeeRequestsReport(GetEmployeeRequestsReportInput input);

    Task<FileDto> GetEmployeeRequestsReportToExcel(GetEmployeeRequestsReportInput input);
}
