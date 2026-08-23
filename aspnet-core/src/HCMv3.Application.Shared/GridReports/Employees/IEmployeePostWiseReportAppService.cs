using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.Employees.Dto;

namespace HCMv3.GridReports.Employees;

/// <summary>
/// Employee Post Wise analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptEmployeePostWise.ascx</c>).
/// One paged read plus one full-set export, both taking the identical input DTO, both <c>[HttpPost]</c>.
/// </summary>
public interface IEmployeePostWiseReportAppService : IApplicationService
{
    Task<PagedResultDto<EmployeePostWiseReportRowDto>> GetEmployeePostWiseReport(GetEmployeePostWiseReportInput input);

    Task<FileDto> GetEmployeePostWiseReportToExcel(GetEmployeePostWiseReportInput input);
}
