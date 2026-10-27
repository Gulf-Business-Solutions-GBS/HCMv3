using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.Employees.Dto;

namespace HCMv3.GridReports.Employees;

/// <summary>
/// Employee Rejoin analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptEmployeeRejoin.ascx</c>).
/// One paged read plus one full-set export, both taking the identical input DTO, both <c>[HttpPost]</c>.
/// </summary>
public interface IEmployeeRejoinReportAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeRejoinReportRowDto>> GetEmployeeRejoinReport(GetEmployeeRejoinReportInput input);

    Task<FileDto> GetEmployeeRejoinReportToExcel(GetEmployeeRejoinReportInput input);
}
