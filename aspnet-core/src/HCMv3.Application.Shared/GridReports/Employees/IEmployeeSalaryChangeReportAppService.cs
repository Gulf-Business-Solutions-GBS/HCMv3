using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.Employees.Dto;

namespace HCMv3.GridReports.Employees;

/// <summary>
/// Employee Salary Change analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptEmployeeSalaryChange.ascx</c>).
///
/// Same two operations as every report in the wave, but the rows carry a DYNAMIC wage-type pivot block --
/// see <see cref="Dto.EmployeeSalaryChangeReportRowDto"/> before consuming this.
/// </summary>
public interface IEmployeeSalaryChangeReportAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeSalaryChangeReportRowDto>> GetEmployeeSalaryChangeReport(
        GetEmployeeSalaryChangeReportInput input);

    Task<FileDto> GetEmployeeSalaryChangeReportToExcel(GetEmployeeSalaryChangeReportInput input);
}
