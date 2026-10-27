using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.Employees.Dto;

namespace HCMv3.GridReports.Employees;

/// <summary>
/// Employee Incentive analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptEmployeeIncentive.ascx</c>).
///
/// ATYPICAL: this report has a TWO-LEVEL DRILL-DOWN (legacy <c>Table1()</c> -> <c>Table2()</c>), unlike the
/// single-level reference report -- hence four operations rather than two. All four are <c>[HttpPost]</c>
/// in the implementation because they take custom input DTOs.
/// </summary>
public interface IEmployeeIncentiveReportAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeIncentiveDepartmentRowDto>> GetEmployeeIncentiveReport(
        GetEmployeeIncentiveReportInput input);

    Task<FileDto> GetEmployeeIncentiveReportToExcel(GetEmployeeIncentiveReportInput input);

    Task<PagedResultDto<EmployeeIncentiveDetailRowDto>> GetEmployeeIncentiveDetailReport(
        GetEmployeeIncentiveDetailReportInput input);

    Task<FileDto> GetEmployeeIncentiveDetailReportToExcel(GetEmployeeIncentiveDetailReportInput input);
}
