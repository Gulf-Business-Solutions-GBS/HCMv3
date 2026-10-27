using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.TimeReport.Dto;

namespace HCMv3.GridReports.TimeReport;

/// <summary>
/// Attendance Log analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptAttendanceLog.ascx</c>).
/// One read/export pair -- legacy has a single table (<c>GetTable()</c> case 1 only, line 420-426).
/// </summary>
public interface IAttendanceLogReportAppService : IApplicationService
{
    Task<PagedResultDto<AttendanceLogReportRowDto>> GetAttendanceLogReport(GetAttendanceLogReportInput input);

    Task<FileDto> GetAttendanceLogReportToExcel(GetAttendanceLogReportInput input);
}
