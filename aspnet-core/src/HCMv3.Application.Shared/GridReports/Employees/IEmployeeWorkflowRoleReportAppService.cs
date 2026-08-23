using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.Employees.Dto;

namespace HCMv3.GridReports.Employees;

/// <summary>
/// Employee Workflow Role analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptEmployeeWorkflowRole.ascx</c>).
/// Same two-operation shape as <see cref="IEmployeeContractReportAppService"/>.
/// </summary>
public interface IEmployeeWorkflowRoleReportAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeWorkflowRoleReportRowDto>> GetEmployeeWorkflowRoleReport(
        GetEmployeeWorkflowRoleReportInput input);

    Task<FileDto> GetEmployeeWorkflowRoleReportToExcel(GetEmployeeWorkflowRoleReportInput input);
}
