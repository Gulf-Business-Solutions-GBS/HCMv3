using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.TimeReport.Dto;

namespace HCMv3.GridReports.TimeReport;

/// <summary>
/// Daily Attendance Summary analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptDailyAttendanceSummary.ascx</c>).
///
/// ATYPICAL FOR THIS FAMILY: a two-level DRILL-DOWN, so TWO read/export pairs -- level 1 is the
/// five-bucket day summary (<c>Table1()</c>) and level 2 the employees behind a chosen bucket
/// (<c>Table2(selectedType)</c>).
/// </summary>
public interface IDailyAttendanceSummaryReportAppService : IApplicationService
{
    Task<PagedResultDto<DailyAttendanceSummaryReportRowDto>> GetDailyAttendanceSummaryReport(
        GetDailyAttendanceSummaryReportInput input);

    Task<FileDto> GetDailyAttendanceSummaryReportToExcel(GetDailyAttendanceSummaryReportInput input);

    Task<PagedResultDto<DailyAttendanceSummaryDetailRowDto>> GetDailyAttendanceSummaryDetailReport(
        GetDailyAttendanceSummaryDetailReportInput input);

    Task<FileDto> GetDailyAttendanceSummaryDetailReportToExcel(
        GetDailyAttendanceSummaryDetailReportInput input);
}
