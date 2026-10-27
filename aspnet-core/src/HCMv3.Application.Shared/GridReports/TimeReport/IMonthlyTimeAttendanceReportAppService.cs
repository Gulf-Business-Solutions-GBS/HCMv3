using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.TimeReport.Dto;

namespace HCMv3.GridReports.TimeReport;

/// <summary>
/// Monthly Time Attendance grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptMonthlyTimeAttendance.ascx</c>).
/// A SUMMARY report: one row per department with the number of employees who incurred any late/early
/// deduction in the window.
/// </summary>
public interface IMonthlyTimeAttendanceReportAppService : IApplicationService
{
    Task<PagedResultDto<MonthlyTimeAttendanceReportRowDto>> GetMonthlyTimeAttendanceReport(
        GetMonthlyTimeAttendanceReportInput input);

    Task<FileDto> GetMonthlyTimeAttendanceReportToExcel(GetMonthlyTimeAttendanceReportInput input);
}
