using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.Employees.Dto;

namespace HCMv3.GridReports.Employees;

/// <summary>
/// Employee Work Location analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptEmployeeWorkLocation.ascx</c>).
/// </summary>
public interface IEmployeeWorkLocationReportAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeWorkLocationReportRowDto>> GetEmployeeWorkLocationReport(
        GetEmployeeWorkLocationReportInput input);

    Task<FileDto> GetEmployeeWorkLocationReportToExcel(GetEmployeeWorkLocationReportInput input);
}
