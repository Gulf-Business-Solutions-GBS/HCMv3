using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dashboards.Attendance.Dto;
using HCMv3.Dto;

namespace HCMv3.Dashboards.Attendance;

/// <summary>
/// Time &amp; Attendance Dashboard: attendance rate, absences, late arrivals, early departures and missing punches
/// over a period, by month, department, employee and weekday, plus punches asked for through self service.
/// Replaces the legacy ESS TimeAttendance / RawTimeAttendance dashboards.
///
/// Reads the Time Engine's results (<c>TimeRegister</c>), never raw punches, so it always agrees with payroll.
/// Whose employees it shows comes from <c>IDashboardScopeService</c>, never from the client.
/// </summary>
public interface IAttendanceDashboardAppService : IApplicationService
{
    Task<AttendanceDashboardDto> GetDashboard(AttendanceDashboardInput input);

    Task<PagedResultDto<AttendanceExceptionRowDto>> GetExceptions(GetAttendanceExceptionsInput input);

    Task<PagedResultDto<AttendancePunchRequestRowDto>> GetPunchRequests(GetAttendancePunchRequestsInput input);

    Task<FileDto> ExportToExcel(ExportAttendanceDashboardInput input);
}
