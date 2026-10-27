using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.Employees.Dto;

namespace HCMv3.GridReports.Employees;

/// <summary>
/// Employee Manager analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptEmployeeManager.ascx</c>).
///
/// Same two-operation shape as every report in this wave: one paged query and one "export everything
/// that matches the same filters", both taking the identical input DTO and both <c>[HttpPost]</c>.
/// </summary>
public interface IEmployeeManagerReportAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeManagerReportRowDto>> GetEmployeeManagerReport(GetEmployeeManagerReportInput input);

    Task<FileDto> GetEmployeeManagerReportToExcel(GetEmployeeManagerReportInput input);
}
