using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.TimeReport.Dto;

namespace HCMv3.GridReports.TimeReport;

/// <summary>
/// Employee Attendance Log analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptEmpAttendanceLog.ascx</c>).
/// One read/export pair -- legacy has a single table (<c>GetTable()</c> calls <c>Table1()</c>
/// unconditionally, lines 133-149).
/// </summary>
public interface IEmpAttendanceLogReportAppService : IApplicationService
{
    Task<PagedResultDto<EmpAttendanceLogReportRowDto>> GetEmpAttendanceLogReport(
        GetEmpAttendanceLogReportInput input);

    Task<FileDto> GetEmpAttendanceLogReportToExcel(GetEmpAttendanceLogReportInput input);
}
