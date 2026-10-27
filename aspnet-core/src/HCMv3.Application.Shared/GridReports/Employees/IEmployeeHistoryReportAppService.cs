using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.Employees.Dto;

namespace HCMv3.GridReports.Employees;

/// <summary>
/// Employee History analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptEmployeeHistory.ascx</c>).
/// Both operations are <c>[HttpPost]</c> in the implementation because they take a custom input DTO.
/// </summary>
public interface IEmployeeHistoryReportAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeHistoryReportRowDto>> GetEmployeeHistoryReport(
        GetEmployeeHistoryReportInput input);

    Task<FileDto> GetEmployeeHistoryReportToExcel(GetEmployeeHistoryReportInput input);
}
