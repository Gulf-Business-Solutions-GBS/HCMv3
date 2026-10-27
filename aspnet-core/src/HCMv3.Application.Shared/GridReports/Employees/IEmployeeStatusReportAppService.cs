using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.Employees.Dto;

namespace HCMv3.GridReports.Employees;

/// <summary>
/// Employee Status analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptEmployeeStatus.ascx</c>).
/// One paged read plus one full-set export, both taking the identical input DTO, both <c>[HttpPost]</c>.
/// </summary>
public interface IEmployeeStatusReportAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeStatusReportRowDto>> GetEmployeeStatusReport(GetEmployeeStatusReportInput input);

    Task<FileDto> GetEmployeeStatusReportToExcel(GetEmployeeStatusReportInput input);
}
