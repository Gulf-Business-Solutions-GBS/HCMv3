using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.TimeReport.Dto;

namespace HCMv3.GridReports.TimeReport;

/// <summary>
/// Daily Man Hours Summary analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptDailyManHoursSummary.ascx</c>).
///
/// ATYPICAL FOR THIS FAMILY: a two-level DRILL-DOWN whose level-2 COLUMN SET DEPENDS ON THE BUCKET --
/// legacy branches to <c>Table2()</c> for Regular and <c>Table3()</c> for Overtime (lines 390-401), and
/// the two have different columns. Hence THREE read/export pairs rather than two.
/// </summary>
public interface IDailyManHoursSummaryReportAppService : IApplicationService
{
    Task<PagedResultDto<DailyManHoursSummaryReportRowDto>> GetDailyManHoursSummaryReport(
        GetDailyManHoursSummaryReportInput input);

    Task<FileDto> GetDailyManHoursSummaryReportToExcel(GetDailyManHoursSummaryReportInput input);

    Task<PagedResultDto<DailyManHoursRegularDetailRowDto>> GetDailyManHoursRegularDetailReport(
        GetDailyManHoursSummaryDetailReportInput input);

    Task<FileDto> GetDailyManHoursRegularDetailReportToExcel(
        GetDailyManHoursSummaryDetailReportInput input);

    Task<PagedResultDto<DailyManHoursOvertimeDetailRowDto>> GetDailyManHoursOvertimeDetailReport(
        GetDailyManHoursSummaryDetailReportInput input);

    Task<FileDto> GetDailyManHoursOvertimeDetailReportToExcel(
        GetDailyManHoursSummaryDetailReportInput input);
}
