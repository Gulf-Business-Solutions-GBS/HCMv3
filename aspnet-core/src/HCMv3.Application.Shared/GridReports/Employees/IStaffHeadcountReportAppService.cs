using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.Employees.Dto;

namespace HCMv3.GridReports.Employees;

/// <summary>
/// Staff Headcount analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptStaffHeadcount.ascx</c>).
///
/// ATYPICAL FOR THIS FAMILY: this report is a multi-level DRILL-DOWN, so it has TWO read/export pairs
/// rather than the reference report's one -- level 1 is the department headcount summary and level 2 is
/// the employee list behind a chosen level-1 row.
/// </summary>
public interface IStaffHeadcountReportAppService : IApplicationService
{
    Task<PagedResultDto<StaffHeadcountReportRowDto>> GetStaffHeadcountReport(
        GetStaffHeadcountReportInput input);

    Task<FileDto> GetStaffHeadcountReportToExcel(GetStaffHeadcountReportInput input);

    Task<PagedResultDto<StaffHeadcountEmployeeReportRowDto>> GetStaffHeadcountEmployeeReport(
        GetStaffHeadcountEmployeeReportInput input);

    Task<FileDto> GetStaffHeadcountEmployeeReportToExcel(GetStaffHeadcountEmployeeReportInput input);
}
